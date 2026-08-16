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
using RimWorld;
using Verse;

namespace BetterRimworlds.UpliftedAnimals
{
    public static class ALZ112Medical
    {
        public static bool HasExposure(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || BRW_HediffDefOf.ALZ112Exposure == null)
            {
                return false;
            }

            return pawn.health.hediffSet.HasHediff(BRW_HediffDefOf.ALZ112Exposure);
        }

        // Already rewritten. A second dose must consume the pill and do
        // nothing else — no Exposure, dice, rage, or re-uplift.
        public static bool IsImmune(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (UpliftedNamer.IsUplifted(pawn.def))
            {
                return true;
            }

            if (pawn.health?.hediffSet == null || BRW_HediffDefOf.ALZ112Uplifted == null)
            {
                return false;
            }

            return pawn.health.hediffSet.HasHediff(BRW_HediffDefOf.ALZ112Uplifted);
        }

        public static bool HasAdministerBill(Pawn pawn)
        {
            BillStack stack = pawn?.health?.surgeryBills;
            if (stack == null)
            {
                return false;
            }

            for (int i = 0; i < stack.Count; i++)
            {
                Bill bill = stack[i];
                if (bill == null || bill.suspended || bill.recipe == null)
                {
                    continue;
                }

                string defName = bill.recipe.defName;
                if (defName != null && defName.IndexOf("ALZ-112", StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool WantsHospitalBed(Pawn pawn)
        {
            return HasExposure(pawn) || HasAdministerBill(pawn);
        }
    }
}
