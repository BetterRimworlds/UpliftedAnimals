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
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterRimworlds.UpliftedAnimals
{
    public class CompProperties_AttackTarget : CompProperties
    {
        public CompProperties_AttackTarget()
        {
            this.compClass = typeof(CompAttackTarget);
        }
    }

    // Animal-side Attack Target button. On uplifted races only.
    // Also owns the hunt allowed-area override so restore never runs
    // inside Toil.Cleanup (the setter can EndCurrentJob and NRE).
    public class CompAttackTarget : ThingComp
    {
        private Area previousArea;
        private bool areaCleared;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref this.previousArea, "brwHuntPrevArea");
            Scribe_Values.Look(ref this.areaCleared, "brwHuntAreaCleared", false);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Pawn pawn = this.parent as Pawn;
            if (pawn == null)
            {
                yield break;
            }

            foreach (Gizmo gizmo in UpliftedAttackTarget.ButtonsForAnimal(pawn))
            {
                yield return gizmo;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.TryRestoreAllowedArea();
        }

        public override void CompTick()
        {
            base.CompTick();
            this.TryRestoreAllowedArea();
        }

#if RIMWORLD16
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            this.TryRestoreAllowedArea();
        }
#endif

        public void ClearAllowedAreaForHunt()
        {
            if (this.areaCleared)
            {
                return;
            }

            Pawn pawn = this.parent as Pawn;
            if (pawn?.playerSettings == null)
            {
                return;
            }

            this.previousArea = this.GetAllowedArea(pawn);
            this.SetAllowedArea(pawn, null);
            this.areaCleared = true;
        }

        private void TryRestoreAllowedArea()
        {
            if (!this.areaCleared)
            {
                return;
            }

            Pawn pawn = this.parent as Pawn;
            if (pawn == null || pawn.CurJobDef == BRW_JobDefOf.BRW_UpliftedTarget)
            {
                return;
            }

            this.RestoreAllowedArea();
        }

        private void RestoreAllowedArea()
        {
            if (!this.areaCleared)
            {
                return;
            }

            Pawn pawn = this.parent as Pawn;
            if (pawn?.playerSettings != null && this.GetAllowedArea(pawn) == null)
            {
                this.SetAllowedArea(pawn, this.previousArea);
            }

            this.areaCleared = false;
            this.previousArea = null;
        }

        private Area GetAllowedArea(Pawn pawn)
        {
#if RIMWORLD15 || RIMWORLD16
            return pawn.playerSettings.AreaRestrictionInPawnCurrentMap;
#else
            return pawn.playerSettings.AreaRestriction;
#endif
        }

        private void SetAllowedArea(Pawn pawn, Area area)
        {
#if RIMWORLD15 || RIMWORLD16
            pawn.playerSettings.AreaRestrictionInPawnCurrentMap = area;
#else
            pawn.playerSettings.AreaRestriction = area;
#endif
        }
    }

    public class CompProperties_MasterAttackTarget : CompProperties
    {
        public CompProperties_MasterAttackTarget()
        {
            this.compClass = typeof(CompMasterAttackTarget);
        }
    }

    // Drafted-master Attack Target button.
    public class CompMasterAttackTarget : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Pawn pawn = this.parent as Pawn;
            if (pawn == null)
            {
                yield break;
            }

            foreach (Gizmo gizmo in UpliftedAttackTarget.ButtonsForHandler(pawn))
            {
                yield return gizmo;
            }
        }
    }
}
