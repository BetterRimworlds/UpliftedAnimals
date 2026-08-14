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

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterRimworlds.UpliftedAnimals
{
    // ALZ-112 uplifts are unique. These races must never appear as ordinary
    // wildlife, farm wander-ins, manhunter packs, quests, or trader stock.
    [StaticConstructorOnStartup]
    public static class UpliftedSpawnBlocker
    {
        static UpliftedSpawnBlocker()
        {
            foreach (PawnKindDef kind in DefDatabase<PawnKindDef>.AllDefsListForReading)
            {
                if (!UpliftedNamer.IsUplifted(kind))
                {
                    continue;
                }

                kind.canArriveManhunter = false;
            }

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.race == null || !UpliftedNamer.IsUplifted(def))
                {
                    continue;
                }

                def.race.herdMigrationAllowed = false;
#if !RIMWORLD12
                if (!UpliftedNamer.AllowsLivestockAction(def))
                {
                    def.race.canReleaseToWild = false;
                }
#endif
#if RIMWORLD16
                def.race.neverIncludeInQuests = true;
#endif
#if RIMWORLD12
                // 1.2 farm wander-in selects any animal with wildness < 0.35.
                if (def.race.wildness < 0.36f)
                {
                    def.race.wildness = 0.36f;
                }
#endif

                if (def.tradeTags == null)
                {
                    def.tradeTags = new List<string>();
                }

                def.tradeTags.RemoveAll(tag => tag == "AnimalFarm");
                if (!def.tradeTags.Contains("AnimalExotic"))
                {
                    def.tradeTags.Add("AnimalExotic");
                }

                // Player can sell their own; traders will not generate them as stock.
                if (def.tradeability == Tradeability.All || def.tradeability == Tradeability.Buyable)
                {
                    def.tradeability = Tradeability.Sellable;
                }

                if (def.comps == null)
                {
                    def.comps = new List<CompProperties>();
                }

                if (!def.comps.Exists(c => c is CompProperties_UpliftedNamer))
                {
                    def.comps.Add(new CompProperties_UpliftedNamer());
                }
            }
        }
    }
}
