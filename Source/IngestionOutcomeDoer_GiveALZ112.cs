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
    // Same as vanilla GiveHediff, except Uplifted pawns consume the dose
    // with no Exposure. Administer and eat both go through this doer.
    public class IngestionOutcomeDoer_GiveALZ112 : IngestionOutcomeDoer_GiveHediff
    {
#if RIMWORLD15 || RIMWORLD16
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            if (ALZ112Medical.IsImmune(pawn))
            {
                return;
            }

            base.DoIngestionOutcomeSpecial(pawn, ingested, ingestedCount);
        }
#else
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested)
        {
            if (ALZ112Medical.IsImmune(pawn))
            {
                return;
            }

            base.DoIngestionOutcomeSpecial(pawn, ingested);
        }
#endif
    }
}
