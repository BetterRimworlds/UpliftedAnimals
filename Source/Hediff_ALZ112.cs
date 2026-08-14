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

        public override bool ShouldRemove => Severity <= 0.001f;

        // private float healAmount => base.Part.def.GetMaxHealth(pawn) * Rand.Gaussian(meanHeal, healDeviation);

        // HediffComp_TendDuration but tendable = false

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            // Severity = Part.def.GetMaxHealth(this.pawn) - 1f;
            Severity = 0.25f;
            this.internalSeverity = 0.25f;

            CurStage.restFallFactorOffset = 5f;

            var compatibleSpecies = true;
            var baseAnimalDef = DefDatabase<ThingDef>.GetNamedSilentFail("Uplifted_" + this.pawn.def.defName);

            if (baseAnimalDef == null)
            {
                compatibleSpecies = false;
                baseAnimalDef = DefDatabase<ThingDef>.GetNamed(this.pawn.def.defName);
            }

            if (compatibleSpecies == true)
            {
                Severity = 0.0f;
                this.internalSeverity = 0.0f;
            }

            CurStage.hungerRateFactorOffset = 5f;
            var random = new System.Random();

            this.deathMultiple = compatibleSpecies ? 7 : random.Next(2, 5);

            double probability = this.deathMultiple == 1 ? 1 : 1.0f / Math.Pow((this.deathMultiple - 1), 2);
            this.lethality = Math.Round(probability * 100f, 2);

            // 1 in 216 == Odds of hitting 6, 6, 6 with 3 dice.
            // Example:
            //     Rolling 1, 1 on 2 3-sided dice == 0.111111 probability.
            //     To match this to a 216 odds cycle:
            //         216 / 0.111111 = 1944.000
            //         1944.000 / 216 = 9.000 divisor
            //         0.111111 / 9.000 = 0.012345678 or 1 out of 81 odds.
            //     To check: 216 / 81 = 2.6667, roughly 0.1234% probability, which is close enough.
            this.severityIncrement = (float)(probability/(216.0f / probability / 216.0f));
        }

        public override float BleedRate => 0f;

        public override string Label
        {
            get
            {
                string severityString = $"{this.internalSeverity * 100f:F2}%";
                int upliftBonus = this.upliftAttempts / 10;
                int targetRoll = Math.Max(3, 18 - upliftBonus);

                int[] successCounts =
                {
                    216, 216, 216, 216, 215, 212, 206, 196,
                    181, 160, 135, 108, 81, 56, 35, 20, 10, 4, 1
                };

                float upliftChance = successCounts[targetRoll] / 216f * 100f;

                return $"ALZ-112 Exposure\n" +
                    $"  • Lethality: {this.lethality}%\n" +
                    $"  • Uplift Mod #{this.upliftAttempts}\n" +
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

            bool flag = Current.Game.tickManager.TicksGame >= this.ticksUntilNextChance;
            if (flag)
            {
                if (this.TryUplift() == false)
                {
                    this.SetNextTick();
                }
            }
        }

        private bool DoUplifting()
        {
            ThingDef baseAnimalDef;
            string kindName = this.pawn.def.defName;

            var compatibleSpecies = true;
            baseAnimalDef = DefDatabase<ThingDef>.GetNamedSilentFail("Uplifted_" + this.pawn.def.defName);

            if (baseAnimalDef == null)
            {
                compatibleSpecies = false;
                baseAnimalDef = DefDatabase<ThingDef>.GetNamed(this.pawn.def.defName);
            }

            this.pawn.def = baseAnimalDef;
            // Birth clones mother.kindDef. Keep it aligned with the new race once,
            // at the moment of uplift, so future offspring inherit the uplift.
            PawnKindDef upliftedKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(
                baseAnimalDef.defName);
            if (upliftedKind != null && this.pawn.kindDef != upliftedKind)
            {
                this.pawn.ChangeKind(upliftedKind);
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

            if (compatibleSpecies)
            {
                // this.pawn.skills = new Pawn_SkillTracker(this.pawn);
                // this.pawn.story = new Pawn_StoryTracker(this.pawn);
                // this.pawn.jobs = new Pawn_JobTracker(this.pawn);
                // this.pawn.workSettings = new Pawn_WorkSettings(this.pawn);
            }

            UpliftedNamer.GiveUpliftName(this.pawn, kindName);
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

            pawn.filth = new Pawn_FilthTracker(pawn);
            // pawn.royalty = new Pawn_RoyaltyTracker(pawn);

            //this.pawn.InitializeComps();

            float days = this.totalTicks / 2500f / 24f;
            Log.Warning($"SUCCESSFULLY UPLIFTED AFTER {this.totalTicks} ({days} days)!!!");
            Messages.Message($"Successfully uplifted {this.pawn.Name} after {days} days!!", MessageTypeDefOf.PositiveEvent);
            Find.LetterStack.ReceiveLetter(
                "Uplifted Animal",
                $"{pawnName} has been successfully Uplifted to full sentience after {days} days!",
                LetterDefOf.PositiveEvent,
                this.pawn);

            //this.successfulUplift = true;

            var alert = Dialog_MessageBox.CreateConfirmation(
                this.pawn.Name + $" has been Uplifted after {days} days!.\n\n" + "You must immediately save and reopen the game.",
                new Action(delegate
                {

                }),
                destructive: true,
                title: "Uplifted Animal"
            );
            Find.WindowStack.Add(alert);



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

            // Odds of Death:
            // Compatible species: 2.78% odds of death. (1 in 37)
            // Incompatible species: From 100% certain to 6.26%.
            var dices = new List<int>
            {
                Rand.RangeInclusive(1, deathMultiple - 1),
                Rand.RangeInclusive(1, deathMultiple - 1)
            };

            diceRoll = dices[0] + dices[1];

            string upliftStatus =
                (diceRoll == 2 ? "Dying" : "Alive") +
                $" (Severity: {this.internalSeverity})";

            if (upliftStatus.Contains("Dying"))
            {
                Log.Warning(
                    $"[Uplift] Attempt {this.upliftAttempts}: " +
                    $"Survived? {dices[0]}, {dices[1]} = {upliftStatus}"
                );
            }

            // If they roll snake eyes, increase severity and possibly kill them.
            if (diceRoll == 2)
            {
                this.internalSeverity = Math.Min(
                    this.severityIncrement + this.internalSeverity,
                    1.0f
                );

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

            // If they are incompatible species, reroll the previous dice for proper odds.
            if (this.deathMultiple < 7)
            {
                dices[0] = Rand.RangeInclusive(1, 6);
                dices[1] = Rand.RangeInclusive(1, 6);
            }

            // Roll the uplifting dice.
            dices.Add(Rand.RangeInclusive(1, 6));

            int rawRoll = dices.Sum();
            int upliftBonus = this.upliftAttempts / 10;
            int adjustedRoll = rawRoll + upliftBonus;

            upliftStatus = adjustedRoll >= 18 ? "Uplifted" : "Unchanged";

            Log.Warning(
                $"[Uplift] Uplift Attempt {this.upliftAttempts}: " +
                $"{dices[0]}, {dices[1]}, {dices[2]} + {upliftBonus} = " +
                $"{adjustedRoll} (raw {rawRoll}) = {upliftStatus}"
            );

            // Success chance improves with number of attempts, with no cap.
            if (adjustedRoll >= 18)
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
