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

using Verse;

namespace BetterRimworlds.UpliftedAnimals
{
    // ThingDef flag for the player-ordered Attack gizmo.
    // RaceProperties has no directedAttack field, so this lives as a
    // mod extension: <modExtensions><li Class="...DirectedAttack">
    // <directedAttack>true</directedAttack></li></modExtensions>
    public class DirectedAttack : DefModExtension
    {
        public bool directedAttack = true;

        public static bool Has(ThingDef def)
        {
            if (def == null || !UpliftedNamer.IsUplifted(def))
            {
                return false;
            }

            DirectedAttack ext = def.GetModExtension<DirectedAttack>();
            return ext != null && ext.directedAttack;
        }

        public static bool Has(Pawn pawn)
        {
            return pawn != null && Has(pawn.def);
        }
    }
}
