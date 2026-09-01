/*
 * This file is part of Uplifted Animals, a Better Rimworlds Project.
 *
 * Copyright © 2024 Theodore R. Smith
 * Author: Theodore R. Smith <hopeseekr@gmail.com>
 *   GPG Fingerprint: D8EA 6E4D 5952 159D 7759  2BB4 EEB6 CE72 F441 EC41
 *   https://github.com/BetterRimworlds/UpliftedAnimals
 *
 * This file is licensed under the Creative Commons No-Derivations v4.0 License.
 * Most rights are reserved.
 */

#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BetterRimworlds.UpliftedAnimals
{
    public class Hediff_ALZ112 : HediffWithComps
    {
        // Unity Mono's Activator.CreateInstance requires an explicit public
        // parameterless ctor. A compiler-generated one is not always found.
        public Hediff_ALZ112()
        {
        }

        public int ticksUntilNextChance;
        public int totalTicks = 0;

        private int deathMultiple;
        private float internalSeverity = 0.0f;
        private float severityIncrement;

        private double lethality;
        private int upliftAttempts = 0;
        private float manhunterMtbDays;

        // 2500 ticks = 1 in-game hour.
        private const float TicksPerHour = 2500f;
        private const float AnimalBerserkMtbDays = 2.5f / 24f;
        private const float HumanBerserkMtbDays = 9f / 24f;
        // Vanilla Moving minForCapable is 0.15. ALZ-112 caps Moving at 0.35,
        // so CapableOf/Downed is the wrong gate — rage when they can still walk.
        private const float MinMovingForRage = 0.15f;

        // No Harmony: watch the tend job + wound tend timers. ALZ-112 itself
        // stays tendable=false so it never gets a tend cooldown.
        private readonly Dictionary<int, int> lastWoundTendTicksLeft = new Dictionary<int, int>();
        private ThingDef pendingTendMedicineDef;
        private int pendingTendMedicineUntilTick;
        private int lastMedicineWatchTick = -1;
        private int lastTimesTendedTo = -1;

        // Incompatible / human: death-save fails 50% more often than the raw
        // snake-eyes rate, and the Nd6 bonus grows half as fast.
        // Compatible: 1d6 babies, 2d6 juveniles, 3d6 otherwise. Incompatible: 4d6.
        // Success is raw + bonus >= diceCount * 6 (natural max).
        private const float IncompatibleLethalityFactor = 1.5f;
        private const int CompatibleUpliftBonusPerAttempts = 10;
        private const int IncompatibleUpliftBonusPerAttempts = 20;
        private const int UpliftDieSides = 6;

        // Exposure is removed explicitly on success. Compatible species start
        // at 0% progress; a Severity<=0 check would delete the hediff on the
        // next health tick and abort the uplift. Already-Uplifted pawns drop
        // a leftover second-dose Exposure immediately (live saves included).
        public override bool ShouldRemove => ALZ112Medical.IsImmune(this.pawn);

        // Vanilla CauseDeathNow treats lethalSeverity as an instant kill once
        // IsLethal is true. Death is owned by the dice + 100% severity bar.
        public override bool CauseDeathNow() => false;

        // private float healAmount => base.Part.def.GetMaxHealth(pawn) * Rand.Gaussian(meanHeal, healDeviation);

        private void NotifyTreatedWithMedicine(ThingDef medicineDef)
        {
            if (medicineDef == null || this.pawn == null || this.pawn.Dead)
            {
                return;
            }

            float reduction;
            if (medicineDef == ThingDefOf.MedicineUltratech)
            {
                reduction = Rand.RangeInclusive(100, 250) / 1000f;
            }
            else if (medicineDef == ThingDefOf.MedicineIndustrial)
            {
                reduction = Rand.RangeInclusive(50, 100) / 1000f;
            }
            else
            {
                return;
            }

            float before = this.internalSeverity;
            this.SetProgress(this.internalSeverity - reduction);
            float applied = before - this.internalSeverity;
            if (applied <= 0f)
            {
                return;
            }
        }

        private void MaybeApplyMedicineFromWoundTend()
        {
            if (this.pawn == null || this.pawn.Dead || this.pawn.health?.hediffSet == null)
            {
                return;
            }

            int now = Current.Game.tickManager.TicksGame;
            if (this.lastMedicineWatchTick == now)
            {
                return;
            }

            this.lastMedicineWatchTick = now;
            this.RememberActiveTendMedicine(now);
            // Jobs tick before health. FinalizeTend has already consumed the
            // medicine by the time we see the tend-complete signal, so keep
            // the last seen industrial/glitterworld def — do not replace it
            // with null from the now-empty job target.
            if (this.TendJustCompleted() && this.pendingTendMedicineDef != null)
            {
                this.NotifyTreatedWithMedicine(this.pendingTendMedicineDef);
                this.pendingTendMedicineDef = null;
            }
        }

        private void RememberActiveTendMedicine(int now)
        {
            bool tendingNow;
            ThingDef medicineDef = this.FindActiveTendMedicine(out tendingNow);
            if (tendingNow)
            {
                if (medicineDef != null)
                {
                    this.pendingTendMedicineDef = medicineDef;
                    this.pendingTendMedicineUntilTick = now + 180;
                }

                return;
            }

            if (now > this.pendingTendMedicineUntilTick)
            {
                this.pendingTendMedicineDef = null;
            }
        }

        private ThingDef FindActiveTendMedicine(out bool tendingNow)
        {
            ThingDef medicineDef;
            if (this.TryReadTendJob(this.pawn, this.pawn, out medicineDef))
            {
                tendingNow = true;
                return medicineDef;
            }

            var spawned = this.pawn.Map?.mapPawns?.AllPawnsSpawned;
            if (spawned == null)
            {
                tendingNow = false;
                return null;
            }

            foreach (Pawn doctor in spawned)
            {
                if (doctor == null || doctor == this.pawn)
                {
                    continue;
                }

                if (this.TryReadTendJob(doctor, this.pawn, out medicineDef))
                {
                    tendingNow = true;
                    return medicineDef;
                }
            }

            tendingNow = false;
            return null;
        }

        private bool TryReadTendJob(Pawn doctor, Pawn patient, out ThingDef medicineDef)
        {
            medicineDef = null;
            Job job = doctor?.CurJob;
            if (job == null || job.def != JobDefOf.TendPatient)
            {
                return false;
            }

            if (job.targetA.Thing != patient && job.targetA.Pawn != patient)
            {
                return false;
            }

            Thing medicine = job.targetB.Thing;
            if (!IsMedicineItem(medicine))
            {
                medicine = job.targetC.Thing;
            }

            if (IsMedicineItem(medicine))
            {
                medicineDef = medicine.def;
            }

            return true;
        }

        private static bool IsMedicineItem(Thing thing)
        {
            return thing?.def != null &&
                   (thing.def == ThingDefOf.MedicineIndustrial ||
                    thing.def == ThingDefOf.MedicineUltratech ||
                    thing.def == ThingDefOf.MedicineHerbal);
        }

        private bool WoundTendTicksJustJumped()
        {
            bool jumped = false;
            List<Hediff> hediffs = this.pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                HediffWithComps withComps = hediffs[i] as HediffWithComps;
                if (withComps == null)
                {
                    continue;
                }

                HediffComp_TendDuration tend = withComps.TryGetComp<HediffComp_TendDuration>();
                if (tend == null)
                {
                    continue;
                }

                int id = withComps.loadID;
                int left = tend.tendTicksLeft;
                int prev;
                if (!this.lastWoundTendTicksLeft.TryGetValue(id, out prev))
                {
                    prev = left;
                }
                else if (left > prev + 30)
                {
                    jumped = true;
                }

                this.lastWoundTendTicksLeft[id] = left;
            }

            return jumped;
        }

        private void SnapshotWoundTendTicks()
        {
            this.lastWoundTendTicksLeft.Clear();
            if (this.pawn?.health?.hediffSet?.hediffs == null)
            {
                return;
            }

            this.WoundTendTicksJustJumped();
            this.SnapshotTimesTended();
        }

        private void SnapshotTimesTended()
        {
            if (this.pawn?.records == null)
            {
                this.lastTimesTendedTo = -1;
                return;
            }

            this.lastTimesTendedTo = this.pawn.records.GetAsInt(RecordDefOf.TimesTendedTo);
        }

        private bool TendJustCompleted()
        {
            bool woundJumped = this.WoundTendTicksJustJumped();
            bool tendedRecordJumped = false;
            if (this.pawn.records != null)
            {
                int current = this.pawn.records.GetAsInt(RecordDefOf.TimesTendedTo);
                if (this.lastTimesTendedTo < 0)
                {
                    this.lastTimesTendedTo = current;
                }
                else if (current > this.lastTimesTendedTo)
                {
                    tendedRecordJumped = true;
                    this.lastTimesTendedTo = current;
                }
            }

            return woundJumped || tendedRecordJumped;
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (ALZ112Medical.IsImmune(this.pawn))
            {
                return;
            }

            bool compatibleSpecies = this.IsCompatibleSpecies();
            this.SetProgress(compatibleSpecies ? 0.001f : 0.25f);

            this.deathMultiple = compatibleSpecies ? 7 : Rand.RangeInclusive(3, 5);
            this.manhunterMtbDays = Rand.Range(8f, 10f) / 24f;
            this.ConfigureLethality();
            this.SnapshotWoundTendTicks();
        }

        // A second dose must not merge and add 0.25 to vanilla Severity.
        // That desynced the kill bar and could look like "died at 25%".
        public override bool TryMergeWith(Hediff other)
        {
            return other != null && other.def == this.def;
        }

        public override float BleedRate => 0f;

        public override string Label
        {
            get
            {
                string severityString = $"{this.internalSeverity * 100f:F2}%";
                int diceCount = this.GetUpliftDiceCount();
                int threshold = diceCount * UpliftDieSides;
                int upliftBonus = this.upliftAttempts / this.UpliftBonusDivisor;
                int targetRoll = Math.Max(diceCount, threshold - upliftBonus);
                float upliftChance = UpliftSuccessChancePercent(diceCount, targetRoll);

                return $"ALZ-112 Exposure\n" +
                    $"  • Uplift Attempt #{this.upliftAttempts}\n" +
                    $"  • Uplift Dice: {diceCount}d{UpliftDieSides} need {threshold}+\n" +
                    $"  • Uplift Bonus: +{upliftBonus}\n" +
                    $"  • Uplift Chance: {upliftChance:F2}%\n" +
                    $"  • Severity: {severityString}";
            }
        }

        public override Color LabelColor
        {
            get
            {
                return new Color(1f, 1f, 0f);
            }
        }

        public override void PostMake()
        {
            base.PostMake();
            this.SetNextTick();
        }

        public override void Tick()
        {
            base.Tick();
            if (ALZ112Medical.IsImmune(this.pawn))
            {
                return;
            }

            this.MaybeApplyMedicineFromWoundTend();
#if !RIMWORLD16
            this.MaybeStartBerserk();
#endif
            this.CheckUpliftChance();
        }

#if RIMWORLD16
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (ALZ112Medical.IsImmune(this.pawn))
            {
                return;
            }

            this.MaybeApplyMedicineFromWoundTend();
            this.MaybeStartBerserk(delta);
            this.CheckUpliftChance();
        }
#endif

        private void CheckUpliftChance()
        {
            if (this.pawn == null || this.pawn.Dead || this.pawn.health == null ||
                ALZ112Medical.IsImmune(this.pawn))
            {
                return;
            }

            // 1.6 calls both Tick and TickInterval. After a successful uplift
            // healBrainInjuries removes this hediff; the interval pass must
            // not roll death on the dangling instance.
            if (!this.pawn.health.hediffSet.hediffs.Contains(this))
            {
                return;
            }

            if (this.deathMultiple < 3 || this.severityIncrement <= 0f || this.severityIncrement > 0.08f)
            {
                this.ConfigureLethality();
            }

            if (Current.Game.tickManager.TicksGame < this.ticksUntilNextChance)
            {
                return;
            }

            if (this.TryUplift() == false)
            {
                this.SetNextTick();
            }
        }

        private bool IsCompatibleSpecies()
        {
            if (this.pawn?.def == null)
            {
                return false;
            }

            // Already-uplifted races have no Uplifted_Uplifted_* def. They
            // must stay on the safe dice, not the human/incompatible table.
            if (UpliftedNamer.IsUplifted(this.pawn.def))
            {
                return true;
            }

            return DefDatabase<ThingDef>.GetNamedSilentFail("Uplifted_" + this.pawn.def.defName) != null;
        }

        private bool IsHumanlikePawn => this.pawn != null && this.pawn.RaceProps != null && this.pawn.RaceProps.Humanlike;

        private int UpliftBonusDivisor =>
            this.IsCompatibleSpecies()
                ? CompatibleUpliftBonusPerAttempts
                : IncompatibleUpliftBonusPerAttempts;

        private int GetUpliftDiceCount()
        {
            if (!this.IsCompatibleSpecies())
            {
                return 4;
            }

            return CompatibleLifeStageDiceCount(this.pawn);
        }

        private static int CompatibleLifeStageDiceCount(Pawn pawn)
        {
            LifeStageDef lifeStage = pawn?.ageTracker?.CurLifeStage;
            if (lifeStage == null)
            {
                return 3;
            }

            if (LifeStageMatches(lifeStage, "Baby", "Newborn", "Larva", "Hatchling", "Chick"))
            {
                return 1;
            }

            if (LifeStageMatches(lifeStage, "Juvenile", "Child", "Toddler", "Immature"))
            {
                return 2;
            }

#if !(RIMWORLD12 || RIMWORLD13)
            DevelopmentalStage stage = lifeStage.developmentalStage;
            if ((stage & (DevelopmentalStage.Newborn | DevelopmentalStage.Baby)) != 0)
            {
                return 1;
            }

            if ((stage & DevelopmentalStage.Child) != 0)
            {
                return 2;
            }
#endif

            var ages = pawn.RaceProps?.lifeStageAges;
            if (ages != null && ages.Count >= 3 && pawn.ageTracker != null)
            {
                int index = pawn.ageTracker.CurLifeStageIndex;
                if (index <= 0)
                {
                    return 1;
                }

                if (index == 1)
                {
                    return 2;
                }
            }

            return 3;
        }

        private static bool LifeStageMatches(LifeStageDef lifeStage, params string[] tokens)
        {
            string defName = lifeStage.defName ?? string.Empty;
            string label = lifeStage.label ?? string.Empty;
            for (int i = 0; i < tokens.Length; i++)
            {
                if (defName.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (label.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        // Ways to roll >= need on Nd6. Index is the needed sum.
        private static readonly int[] UpliftWaysGe1d6 =
        {
            6, 6, 5, 4, 3, 2, 1
        };

        private static readonly int[] UpliftWaysGe2d6 =
        {
            36, 36, 36, 35, 33, 30, 26, 21, 15, 10, 6, 3, 1
        };

        private static readonly int[] UpliftWaysGe3d6 =
        {
            216, 216, 216, 216, 215, 212, 206, 196,
            181, 160, 135, 108, 81, 56, 35, 20, 10, 4, 1
        };

        private static readonly int[] UpliftWaysGe4d6 =
        {
            1296, 1296, 1296, 1296, 1296, 1295, 1291, 1281, 1261, 1226,
            1170, 1090, 986, 861, 721, 575, 435, 310, 206, 126, 70, 35, 15, 5, 1
        };

        private static float UpliftSuccessChancePercent(int diceCount, int need)
        {
            int[] waysGe;
            int total;
            switch (diceCount)
            {
                case 1:
                    waysGe = UpliftWaysGe1d6;
                    total = 6;
                    break;
                case 2:
                    waysGe = UpliftWaysGe2d6;
                    total = 36;
                    break;
                case 4:
                    waysGe = UpliftWaysGe4d6;
                    total = 1296;
                    break;
                default:
                    waysGe = UpliftWaysGe3d6;
                    total = 216;
                    break;
            }

            if (need <= diceCount)
            {
                return 100f;
            }

            if (need >= waysGe.Length)
            {
                return 0f;
            }

            return waysGe[need] * 100f / total;
        }

        // Sleeping / bedded patients can still snap (forceWake). Below
        // 15% Moving they cannot path — a forced Berserk/Manhunter job
        // throws. Vanilla Downed is the wrong gate: ALZ-112 caps Moving
        // at 35% and often marks them Downed in bed. Animals: Manhunter
        // every 8–10 hours; otherwise short Berserk. After the episode
        // they go back to a hospital bed.
#if RIMWORLD16
        private void MaybeStartBerserk(int delta)
#else
        private void MaybeStartBerserk()
#endif
        {
            if (this.pawn == null || this.pawn.Dead || this.pawn.mindState == null)
            {
                return;
            }

            if (!this.pawn.Spawned)
            {
                return;
            }

            if (!this.CanRageOnMoving())
            {
                this.RecoverRageIfAny();
                return;
            }

            if (this.pawn.InMentalState)
            {
                return;
            }

#if RIMWORLD16
            if (!this.pawn.IsHashIntervalTick(60, delta))
#else
            if (!this.pawn.IsHashIntervalTick(60))
#endif
            {
                return;
            }

            if (this.manhunterMtbDays <= 0f)
            {
                this.manhunterMtbDays = Rand.Range(8f, 10f) / 24f;
            }

            if (this.IsHumanlikePawn)
            {
                if (Rand.MTBEventOccurs(HumanBerserkMtbDays, 60000f, 60f))
                {
                    this.StartRage("Berserk", silent: false, minHours: 1f, maxHours: 2f);
                }

                return;
            }

            if (Rand.MTBEventOccurs(this.manhunterMtbDays, 60000f, 60f))
            {
                this.StartRage("Manhunter", silent: false, minHours: 1.5f, maxHours: 2.5f);
                return;
            }

            if (Rand.MTBEventOccurs(AnimalBerserkMtbDays, 60000f, 60f))
            {
                this.StartRage("Berserk", silent: true, minHours: 1f, maxHours: 1.8f);
            }
        }

        private bool CanRageOnMoving()
        {
            if (this.pawn.health?.capacities == null)
            {
                return false;
            }

            if (!this.pawn.health.capacities.CanBeAwake)
            {
                return false;
            }

            return this.pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= MinMovingForRage;
        }

        private void StartRage(string defName, bool silent, float minHours, float maxHours)
        {
            MentalStateDef rage = DefDatabase<MentalStateDef>.GetNamedSilentFail(defName);
            if (rage == null)
            {
                return;
            }

            string reason = "MentalStateReason_Hediff".Translate(this.def.LabelCap);
            bool started;
#if RIMWORLD15 || RIMWORLD16
            started = this.pawn.mindState.mentalStateHandler.TryStartMentalState(
                rage,
                reason,
                forced: true,
                forceWake: true,
                transitionSilently: silent);
#else
            started = this.pawn.mindState.mentalStateHandler.TryStartMentalState(
                rage, reason, forceWake: true, causedByMood: false, otherPawn: null);
#endif
            if (!started || this.pawn.MentalState == null)
            {
                return;
            }

            int minTicks = (int)(minHours * TicksPerHour);
            int maxTicks = (int)(maxHours * TicksPerHour);
            this.pawn.MentalState.forceRecoverAfterTicks = Rand.RangeInclusive(minTicks, maxTicks);
        }

        private void RecoverRageIfAny()
        {
            MentalStateDef cur = this.pawn.MentalStateDef;
            if (cur == null)
            {
                return;
            }

            if (cur.defName != "Berserk" && cur.defName != "Manhunter")
            {
                return;
            }

            this.pawn.mindState.mentalStateHandler.CurState?.RecoverFromState();
        }

        private void ConfigureLethality()
        {
            if (this.IsCompatibleSpecies())
            {
                this.deathMultiple = 7;
            }
            else if (this.deathMultiple < 3)
            {
                // Old 1-sided / 100% saves. Do not re-roll 3–5 or a reload
                // changes their odds. 3 = 2-sided dice = 25% snake-eyes.
                this.deathMultiple = 3;
            }

            int deathDieSides = Math.Max(2, this.deathMultiple - 1);
            double snakeEyes = 1.0 / (deathDieSides * deathDieSides);
            double failChance = this.IsCompatibleSpecies()
                ? snakeEyes
                : snakeEyes * IncompatibleLethalityFactor;
            this.lethality = Math.Round(failChance * 100.0, 2);

            // increment = snake-eyes p², matching the original 216-cycle
            // comment (p=1/9 → 1/81). Never a full-bar step. The 1.5x
            // incompatible factor is extra fails, not bigger steps.
            float rawIncrement = (float)(snakeEyes * snakeEyes);
            this.severityIncrement = Math.Min(rawIncrement, 0.08f);
            if (this.severityIncrement < 0.001f)
            {
                this.severityIncrement = rawIncrement > 0f ? rawIncrement : 0.001f;
            }
        }

        private void SetProgress(float value)
        {
            this.internalSeverity = Math.Max(0f, Math.Min(value, 1f));
            this.Severity = Math.Max(this.internalSeverity, 0.001f);
        }

        private bool DoUplifting()
        {
            bool humanlike = this.IsHumanlikePawn;
            string kindName = this.pawn.def.defName;

            if (!humanlike && !UpliftedNamer.IsUplifted(this.pawn.def))
            {
                ThingDef baseAnimalDef = DefDatabase<ThingDef>.GetNamedSilentFail("Uplifted_" + this.pawn.def.defName);
                if (baseAnimalDef == null)
                {
                    baseAnimalDef = DefDatabase<ThingDef>.GetNamed(this.pawn.def.defName);
                }

                this.pawn.def = baseAnimalDef;
                PawnKindDef upliftedKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(
                    baseAnimalDef.defName);
                if (upliftedKind != null && this.pawn.kindDef != upliftedKind)
                {
                    this.pawn.ChangeKind(upliftedKind);
                }
            }

            // Cure the brain ailments + ALZ-112 Exposure..
            this.healBrainInjuries(this.pawn);

            // Add ALZ-112 Uplifted status.
            this.pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed("ALZ112Uplifted"));

            if (this.pawn.health.hediffSet.GetFirstHediffOfDef(
                    DefDatabase<HediffDef>.GetNamed("ALZ112Uplifted")) is Hediff_ALZ112Uplifted uplifted)
            {
                uplifted.AnchorToColony();
            }

            if (!humanlike)
            {
                UpliftedNamer.GiveUpliftName(this.pawn, kindName);
            }

            Name pawnName = this.pawn.Name;
            Log.Warning("====== New Name: " + pawnName + " =======");

            // {FULLY UPLIFTED ANIMAL}}
            // this.pawn.caller = new Pawn_CallTracker(this.pawn);
            // this.pawn.equipment = new Pawn_EquipmentTracker(this.pawn);
            // this.pawn.verbTracker = new VerbTracker(this.pawn);
            // this.pawn.drafter = new Pawn_DraftController(this.pawn);
            // this.pawn.jobs = new Pawn_JobTracker(this.pawn);

            // pawn.abilities = new Pawn_AbilityTracker(pawn);
            // pawn.apparel = new Pawn_ApparelTracker(pawn);
            // pawn.caller = new Pawn_CallTracker(pawn);
            // pawn.drafter = new Pawn_DraftController(pawn);
            // pawn.drugs = new Pawn_DrugPolicyTracker(pawn);
            // pawn.equipment = new Pawn_EquipmentTracker(pawn);
            // pawn.filth = new Pawn_FilthTracker(pawn);
            // pawn.guest = new Pawn_GuestTracker(pawn);
            // pawn.guilt = new Pawn_GuiltTracker();
            // pawn.interactions = new Pawn_InteractionsTracker(pawn);
            // pawn.inventory = new Pawn_InventoryTracker(pawn);
            // pawn.jobs = new Pawn_JobTracker(pawn);
            // pawn.mindState = new Pawn_MindState(pawn);
            // pawn.natives = new Pawn_NativeVerbs(pawn);
            // pawn.needs = new Pawn_NeedsTracker(pawn);
            // pawn.outfits = new Pawn_OutfitTracker(pawn);
            // pawn.ownership = new Pawn_Ownership(pawn);
            // pawn.pather = new Pawn_PathFollower(pawn);
            // pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            // pawn.psychicEntropy = new Pawn_PsychicEntropyTracker(pawn);
            // pawn.records = new Pawn_RecordsTracker(pawn);
            // pawn.relations = new Pawn_RelationsTracker(pawn);
            // pawn.rotationTracker = new Pawn_RotationTracker(pawn);
            // pawn.stances = new Pawn_StanceTracker(pawn);
            // pawn.story = new Pawn_StoryTracker(pawn);
            // pawn.thinker = new Pawn_Thinker(pawn);
            // pawn.workSettings = new Pawn_WorkSettings(pawn);

            // pawn.skills = new Pawn_SkillTracker(pawn);
            // pawn.timetable = new Pawn_TimetableTracker(pawn);
            // pawn.trader = new Pawn_TraderTracker(pawn);
            // pawn.training = new Pawn_TrainingTracker(pawn);

            // pawn.verbTracker = new VerbTracker(pawn);
            // pawn.carryTracker = new Pawn_CarryTracker(pawn);
            // pawn.meleeVerbs = new Pawn_MeleeVerbs(pawn);
            // pawn.verbTracker.VerbsNeedReinitOnLoad();

            if (!humanlike)
            {
                pawn.filth = new Pawn_FilthTracker(pawn);
            }

            float days = this.totalTicks / 2500f / 24f;
            Log.Warning($"SUCCESSFULLY UPLIFTED AFTER {this.totalTicks} ({days} days)!!!");
            Messages.Message($"Successfully uplifted {this.pawn.Name} after {days} days!!", MessageTypeDefOf.PositiveEvent);

            if (humanlike)
            {
                Find.LetterStack.ReceiveLetter(
                    "Uplifted Human",
                    $"{pawnName} has survived ALZ-112 and been Uplifted after {days} days. They remain human.",
                    LetterDefOf.PositiveEvent,
                    this.pawn);
            }
            else
            {
                Find.LetterStack.ReceiveLetter(
                    "Uplifted Animal",
                    $"{pawnName} has been successfully Uplifted to full sentience after {days} days!",
                    LetterDefOf.PositiveEvent,
                    this.pawn);

                var alert = Dialog_MessageBox.CreateConfirmation(
                    this.pawn.Name + $" has been Uplifted after {days} days!.\n\n" + "You must immediately save and reopen the game.",
                    new Action(delegate
                    {
                    }),
                    destructive: true,
                    title: "Uplifted Animal"
                );
                Find.WindowStack.Add(alert);
            }

            return true;
        }

        /** Derived from https://github.com/BetterRimworlds/Cryoregenesis/ **/
        private void healBrainInjuries(Pawn pawn)
        {
            #if RIMWORLD12 || RIMWORLD13
            foreach (Hediff h in pawn.health.hediffSet.GetHediffs<Hediff>().ToList())
            #else
            var hediffsOfPawn = new List<Hediff>();
            pawn.health.hediffSet.GetHediffs<Hediff>(ref hediffsOfPawn);
            foreach (Hediff h in hediffsOfPawn.ToList())
            #endif
            {

                if (h.def.defName == "Cataract"       ||
                    h.def.defName == "Dementia"       ||
                    h.def.defName == "ALZ112Exposure" ||
                    h.def.defName.Contains("Alzheimer"))
                {
                    pawn.health.RemoveHediff(h);
                }
            }
        }

        public bool TryUplift()
        {
            int diceRoll;

            ++this.upliftAttempts;

            // Odds of a severity tick (snake eyes on two death dice):
            // Compatible: 6-sided → 1/36.
            // Human / incompatible: 2–4 sided → 1/4, 1/9, or 1/16.
            int deathDieSides = Math.Max(2, this.deathMultiple - 1);
            var dices = new List<int>
            {
                Rand.RangeInclusive(1, deathDieSides),
                Rand.RangeInclusive(1, deathDieSides)
            };

            diceRoll = dices[0] + dices[1];

            bool deathHit = diceRoll == 2;
            if (!this.IsCompatibleSpecies() && !deathHit)
            {
                // Raise fail rate by 50% without a 1-sided die.
                // extra = (1.5p - p) / (1 - p)
                double snakeEyes = 1.0 / (deathDieSides * (double)deathDieSides);
                double extraFail = 0.5 * snakeEyes / (1.0 - snakeEyes);
                if (Rand.Value < extraFail)
                {
                    deathHit = true;
                }
            }

            string upliftStatus =
                (deathHit ? "Dying" : "Alive") +
                $" (Severity: {this.internalSeverity})";

            if (deathHit)
            {
                Log.Warning(
                    $"[Uplift] Attempt {this.upliftAttempts}: " +
                    $"Survived? {dices[0]}, {dices[1]} = {upliftStatus}"
                );
            }

            if (deathHit)
            {
                float step = this.severityIncrement;
                if (step <= 0f || step > 0.08f)
                {
                    step = Math.Min(0.08f, Math.Max(step, 0.001f));
                }

                this.SetProgress(this.internalSeverity + step);

                Log.Warning(
                    $"[Uplift] The severity of the ALZ-112 Exposure in " +
                    $"{this.pawn.Name} has reached {this.internalSeverity}."
                );

                if (this.internalSeverity >= 1.0f)
                {
                    Messages.Message(
                        this.pawn.LabelShortCap +
                            " died from exposure to drug ALZ-112.",
                        MessageTypeDefOf.NegativeEvent
                    );
                    this.pawn.Kill(null, this);
                }

                return false;
            }

            // Death dice are not the rewrite dice. Roll a fair Nd6 after surviving.
            int diceCount = this.GetUpliftDiceCount();
            int threshold = diceCount * UpliftDieSides;
            dices.Clear();
            for (int i = 0; i < diceCount; i++)
            {
                dices.Add(Rand.RangeInclusive(1, UpliftDieSides));
            }

            int rawRoll = dices.Sum();
            int upliftBonus = this.upliftAttempts / this.UpliftBonusDivisor;
            int adjustedRoll = rawRoll + upliftBonus;

            upliftStatus = adjustedRoll >= threshold ? "Uplifted" : "Unchanged";

            Log.Warning(
                $"[Uplift] Uplift Attempt {this.upliftAttempts}: " +
                $"{string.Join(", ", dices)} + {upliftBonus} = " +
                $"{adjustedRoll} (raw {rawRoll}) = {upliftStatus}"
            );

            // Success chance improves with number of attempts, with no cap.
            if (adjustedRoll >= threshold)
            {
                return this.DoUplifting();
            }

            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksUntilNextChance, "ticksUntilNextChance", 0, false);
            Scribe_Values.Look<int>(ref this.totalTicks,           "totalTicks", 0, false);
            Scribe_Values.Look<int>(ref this.upliftAttempts,       "upliftAttempts", 0, false);
            Scribe_Values.Look<int>(ref this.deathMultiple,        "deathMultiple", 0, false);
            Scribe_Values.Look<float>(ref this.internalSeverity,   "internalSeverity", 0, false);
            Scribe_Values.Look<float>(ref this.severityIncrement,  "severityIncrement", 0, false);
            Scribe_Values.Look<double>(ref this.lethality,         "lethality", 0, false);
            Scribe_Values.Look<float>(ref this.manhunterMtbDays,   "manhunterMtbDays", 0, false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.ConfigureLethality();
                this.SetProgress(this.internalSeverity);
                this.SnapshotWoundTendTicks();
                if (this.manhunterMtbDays <= 0f)
                {
                    this.manhunterMtbDays = Rand.Range(8f, 10f) / 24f;
                }
            }
        }

        public void SetNextTick()
        {
            // One chance every 1.5 in-game hours / 90 minutes... 3750 Ticks, by default.
            const double ticksPerMinute = 2500f / 60f;
            int timeDiff = (int)Math.Ceiling(Settings.Get().MinutesBetweenUpliftAttempts * ticksPerMinute);
            // timeDiff = 10;

            this.ticksUntilNextChance = Current.Game.tickManager.TicksGame + timeDiff;
            this.totalTicks += timeDiff;
        }
    }
}
