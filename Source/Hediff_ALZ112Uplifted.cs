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
using RimWorld;
using Verse;

namespace BetterRimworlds.UpliftedAnimals
{
    public class Hediff_ALZ112Uplifted : Hediff
    {
        public Hediff_ALZ112Uplifted()
        {
        }

        private bool anchored;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            this.AnchorToColony();
        }

        public override void Tick()
        {
            base.Tick();
            if (!this.anchored)
            {
                this.AnchorToColony();
            }
        }

#if RIMWORLD16
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!this.anchored)
            {
                this.AnchorToColony();
            }
        }
#endif

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.anchored, "anchored", false);
        }

        // Stops pen-roam / exit-map behaviour immediately so a missed save/reload
        // cannot lose the animal. Safe to call more than once.
        internal void AnchorToColony()
        {
            Pawn pawn = this.pawn;
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            if (pawn.MentalStateDef != null && pawn.MentalStateDef.defName == "Roaming")
            {
                pawn.mindState.mentalStateHandler.CurState.RecoverFromState();
            }

            // 1.6-steam marks ClearMind obsolete in favor of ClearMind_NewTemp,
            // but that 4-arg method is missing on other 1.6 builds and a
            // MissingMethodException here strips this hediff from the pawn.
#pragma warning disable CS0612
            pawn.ClearMind(true);
#pragma warning restore CS0612
            if (pawn.pather != null)
            {
                pawn.pather.StopDead();
            }

            if (pawn.Faction != Faction.OfPlayer)
            {
                pawn.SetFaction(Faction.OfPlayer);
            }

            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn);

            if (pawn.playerSettings == null)
            {
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            }

            if (pawn.Spawned && pawn.Map != null)
            {
#if RIMWORLD15 || RIMWORLD16
                if (pawn.playerSettings.AreaRestrictionInPawnCurrentMap == null)
                {
                    pawn.playerSettings.AreaRestrictionInPawnCurrentMap = pawn.Map.areaManager.Home;
                }
#else
                if (pawn.playerSettings.AreaRestriction == null)
                {
                    pawn.playerSettings.AreaRestriction = pawn.Map.areaManager.Home;
                }
#endif
            }

            this.anchored = true;
        }
    }
}
