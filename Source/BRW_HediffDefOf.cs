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

using RimWorld;
using Verse;

namespace BetterRimworlds.UpliftedAnimals
{
    [DefOf]
    public class BRW_HediffDefOf
    {
#pragma warning disable CS0649
        // public static HediffDef RegrowingBodypart;
        public static HediffDef ALZ112Exposure;
#pragma warning restore CS0649

        static BRW_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BRW_HediffDefOf));
        }
    }
}
