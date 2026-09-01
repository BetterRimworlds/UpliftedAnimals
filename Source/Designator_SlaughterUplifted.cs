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
    // Selected-pawn Slaughter gizmo only. The Animals tab checkbox is
    // a different worker and is left alone.
    public class Designator_SlaughterUplifted : Designator_Slaughter
    {
        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            Pawn pawn = t as Pawn;
            if (pawn != null && !UpliftedNamer.AllowsLivestockAction(pawn.def))
            {
                return false;
            }

            return base.CanDesignateThing(t);
        }

        public static void EnsureReverseDesignator()
        {
            // InitDesignators and our ctor load textures. FinalizeInit
            // runs on a LongEvent worker thread; Unity forbids that.
            if (!UnityData.IsInMainThread || Find.MapUI == null)
            {
                return;
            }

            List<Designator> list = Find.ReverseDesignatorDatabase.AllDesignators;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].GetType() == typeof(Designator_Slaughter))
                {
                    list[i] = new Designator_SlaughterUplifted();
                }
            }
        }
    }
}
