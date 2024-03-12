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

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterRimworlds.UpliftedAnimals
{
    public class Hediff_ALZ112 : Hediff
    {
        public int ticksUntilNextChance;
        public int totalTicks = 0;

        private int deathMultiple;
        private float internalSeverity = 0.0f;
        private float severityIncrement;
        
        private int upliftAttempts = 0;

        public override bool ShouldRemove => Severity <= 0.001f;

        // private float healAmount => base.Part.def.GetMaxHealth(pawn) * Rand.Gaussian(meanHeal, healDeviation);


        public override void PostAdd(DamageInfo? dinfo)
        {
            // Severity = Part.def.GetMaxHealth(this.pawn) - 1f;
            Severity = 0.25f;
            this.internalSeverity = 0.25f;
        
            // var jobDriver = new JobDriver_Ingest();
            CurStage.restFallFactorOffset = 1f / 500f;
            // HediffComp_GetsPermanent permanentComp = (HediffComp_GetsPermanent)comps.Find(comp => comp is HediffComp_GetsPermanent);
            // permanentComp.IsPermanent = true;
        
            Log.Error("Pawn Def Name: " + pawn.def.defName);
            Log.Error("Is life threatening?" + CurStage.lifeThreatening);
            Log.Error("Min Severity: " + CurStage.minSeverity + " | Current Severity: " + Severity);
            if (this.pawn.def.defName == "Raccoon")
            {
                Severity = 0.0f;
                this.internalSeverity = 0.0f;
                Log.Error("Min Severity: " + CurStage.minSeverity + " | Current Severity: " + Severity);
                CurStage.hungerRateFactorOffset = 1f / 500f;
                //this.CurStage.minSeverity = 
            }
            
            var random = new System.Random();
            bool compatibleSpecies;
            compatibleSpecies = this.pawn.def.defName == "Raccoon";

            this.deathMultiple = compatibleSpecies ? 7 : random.Next(2, 5);

            double probability = this.deathMultiple == 1 ? 1 : 1.0f / Math.Pow((this.deathMultiple - 1), 2);
            double lethality = Math.Round(probability * 100f, 2);
            
            // 1 in 216 == Odds of hitting 6, 6, 6 with 3 dice.
            // Example:
            //     Rolling 1, 1 on 2 3-sided dice == 0.111111 probability.
            //     To match this to a 216 odds cycle:
            //         216 / 0.111111 = 1944.000
            //         1944.000 / 216 = 9.000 divisor
            //         0.111111 / 9.000 = 0.012345678 or 1 out of 81 odds.
            //     To check: 216 / 81 = 2.6667, roughly 0.1234% probability, which is close enough.
            this.severityIncrement = (float)(probability/(216.0f / probability / 216.0f));
            Log.Error("================= SEVERITY INCREMENT: " + this.severityIncrement);

            Log.Warning("Lethality: " + lethality + "% | Death Multiple: " + this.deathMultiple);
            this.CurStage.label = "Lethality: " + lethality + "% | Death Multiple: " + this.deathMultiple;
        }

        public override float BleedRate => 0f;

        public override Color LabelColor
        {
            get
            {
                return new Color(0.2f, 0.8f, 0.2f);
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
            var compatibleSpecies = true;
            Log.Error("1");
            ThingDef baseAnimalDef;
            string kindName = this.pawn.def.defName;
            baseAnimalDef = DefDatabase<ThingDef>.GetNamedSilentFail("Uplifted_" + this.pawn.def.defName);
            Log.Error("2");

            if (baseAnimalDef == null)
            {
                Log.Error("3");
                compatibleSpecies = false;
                baseAnimalDef = DefDatabase<ThingDef>.GetNamed(this.pawn.def.defName);
            }

            if (compatibleSpecies)
            {
                
            }

            Log.Error("4");
            this.pawn.def = baseAnimalDef;

            Log.Error("5");
            var b = new Hediff();

            
            
            // Cure the brain ailments + ALZ-112 Exposure..
            this.healBrainInjuries(this.pawn);
            
            // Add ALZ-112 Uplifted status.
            this.pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed("ALZ112Uplifted"));
            
            Log.Error("6.2");
            this.pawn.SetFactionDirect(Faction.OfPlayer);
            this.pawn.skills = new Pawn_SkillTracker(this.pawn);
            this.pawn.story = new Pawn_StoryTracker(this.pawn);
            // this.pawn.jobs = new Pawn_JobTracker(this.pawn);
            // this.pawn.workSettings = new Pawn_WorkSettings(this.pawn);

            var pawnName = this.pawn.Name;
            if (pawn.Name.Numerical == true)
            {
                //PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo(this.pawn, kindName, this.pawn.Faction.def);
                pawnName = PawnBioAndNameGenerator.GeneratePawnName(this.pawn, NameStyle.Full, kindName);
            }

            this.pawn.Name = new NameTriple(pawnName.ToString(), pawnName.ToString(), kindName);

            return true;
        }

        /** Derived from https://github.com/BetterRimworlds/Cryoregenesis/ **/
        private void healBrainInjuries(Pawn pawn)
        {
            #if RIMWORLD14
            var hediffsOfPawn = new List<Hediff>();
            pawn.health.hediffSet.GetHediffs<Hediff>(ref hediffsOfPawn);
            foreach (Hediff hediff in hediffsOfPawn.ToList())
            #else
            foreach (Hediff h in pawn.health.hediffSet.GetHediffs<Hediff>().ToList())
            #endif
            {

                if (h.def.defName == "Blindness"      ||
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
            var random = new System.Random();
            // int diceRoll = random.Next(1, 7) + random.Next(1, 7);
            int diceRoll;

            ++this.upliftAttempts;

            // Odds of Death:
            // Compatible species: 2.78% odds of death. (1 in 37)
            // Incompatible species: From 100% certain to 6.26%.
            var dices = new List<int>
            {
                random.Next(1, deathMultiple),
                random.Next(1, deathMultiple)
            };
            // diceRoll = random.Next(1, deathMultiple) + random.Next(1, deathMultiple)};
            diceRoll = dices[0] + dices[1];

            string upliftStatus = (diceRoll == 2 ? "Dying" : "Alive") + $" (Severity: {this.internalSeverity})";
            Log.Error($"[Uplift] Attempt {this.upliftAttempts}: Survived? {dices[0]}, {dices[1]} = {upliftStatus}");

            // If they roll snake eyes, kill them instantly. 1 in 37 chance.
            if (diceRoll == 2)
            {
                // this.Severity += this.severityIncrement;
                this.internalSeverity = Math.Min(this.severityIncrement + this.internalSeverity, 1.0f);
                Log.Warning($"[Uplift] The severity of the ALZ-112 Exposure in {this.pawn.Name} has reached {this.internalSeverity}.");

                if (this.internalSeverity >= 1.0f)
                {
                    Messages.Message(this.pawn.Name.ToStringFull + " died from exposure to drug ALZ-112.",
                        MessageTypeDefOf.NegativeEvent);
                    this.pawn.Kill(null, this);
                }

                return false;
            }
            
            // If they are incompatible species, reroll the previous dice for proper odds.
            if (this.deathMultiple < 7)
            {
                dices[0] = random.Next(1, 7);
                dices[1] = random.Next(1, 7);
            }

            // Roll the uplifting dice.
            dices.Add(random.Next(1, 7));
            diceRoll = dices.Sum();

            upliftStatus = diceRoll == 18 ? "Uplifted" : "Unchanged";

            Log.Error($"[Uplift] Uplift Attempt {this.upliftAttempts}: : {dices[0]}, {dices[1]}, {dices[2]} = {upliftStatus}");
            
            // If 3 sixes are rolled, uplift them. 1 in 216 chance.
            // 216 attempts x 1.5 hours / 24 hours = 13.5 days, on average.
            // if (diceRoll == 18)
            if (diceRoll >= 10)
            {
                float days = this.totalTicks / 2500f / 24f;
                Log.Error($"SUCCESSFULLY UPLIFTED AFTER {this.totalTicks} ({days} days)!!!");

                return this.DoUplifting();
            }

            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksUntilNextChance, "ticksUntilNextHeal", 0, false);
        }

        public void SetNextTick()
        {
            // One chance every 1.5 in-game hours / 90 minutes... 3750 Ticks.
            //int timeDiff = (int)(2500f * 1.5f);
            int timeDiff = (int)(2500f * 0.10);
            this.ticksUntilNextChance = Current.Game.tickManager.TicksGame + timeDiff;
            this.totalTicks += timeDiff;
        }
    }
}
