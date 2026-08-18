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
using Verse;

namespace BetterRimworlds.UpliftedAnimals
{
    public class CompProperties_UpliftedNamer : CompProperties
    {
        public CompProperties_UpliftedNamer()
        {
            this.compClass = typeof(CompUpliftedNamer);
        }
    }

    // Replaces the numeric "Uplifted raccoon 1" stamp with the same
    // NameTriple style used at ALZ-112 success, and grants the
    // ALZ-112 Uplifted hediff that newborns do not inherit.
    // Injected onto every Uplifted_* race in UpliftedSpawnBlocker.
    public class CompUpliftedNamer : ThingComp
    {
        private bool tried;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.TryName();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!this.tried)
            {
                this.TryName();
            }
        }

#if RIMWORLD16
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (!this.tried)
            {
                this.TryName();
            }
        }
#endif

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            Pawn pawn = this.parent as Pawn;
            if (pawn == null)
            {
                yield break;
            }

            foreach (Gizmo gizmo in UpliftedTarget.GetGizmos(pawn))
            {
                yield return gizmo;
            }
        }

        private void TryName()
        {
            Pawn pawn = this.parent as Pawn;
            if (pawn == null || !UpliftedNamer.IsUplifted(pawn.def))
            {
                this.tried = true;
                return;
            }

            ALZ112Medical.GiveUpliftedHediffIfNeeded(pawn);
            UpliftedNamer.GiveNameIfNeeded(pawn);
            if (!UpliftedNamer.NeedsName(pawn))
            {
                this.tried = true;
            }
        }
    }
}
