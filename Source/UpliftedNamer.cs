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
    public static class UpliftedNamer
    {
        public const string Prefix = "Uplifted_";

        public static bool IsUplifted(Def def)
        {
            return def?.defName != null && def.defName.StartsWith(Prefix);
        }

        public static bool IsBird(ThingDef def)
        {
            return def?.race?.body != null && def.race.body.defName == "Bird";
        }

        // Selected-pawn Slaughter / Release to wild stay on birds only.
        public static bool AllowsLivestockAction(ThingDef def)
        {
            return !IsUplifted(def) || IsBird(def);
        }

        public static bool NeedsName(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !IsUplifted(pawn.def))
            {
                return false;
            }

            if (pawn.Faction != Faction.OfPlayer)
            {
                return false;
            }

            return pawn.Name == null || pawn.Name.Numerical;
        }

        public static void GiveNameIfNeeded(Pawn pawn)
        {
            if (!NeedsName(pawn))
            {
                return;
            }

            string firstName = RandomFirstName(pawn);
            pawn.Name = new NameTriple(firstName, firstName, LastNameFor(pawn));
        }

        // Used at ALZ-112 success. Keeps a tame/player first name and uses
        // the pre-uplift species def as the last name (Raccoon, not Uplifted_Raccoon).
        public static void GiveUpliftName(Pawn pawn, string lastName)
        {
            if (pawn == null)
            {
                return;
            }

            string firstName;
            if (pawn.Name != null && pawn.Name.Numerical == false)
            {
                firstName = pawn.Name.ToStringShort;
            }
            else
            {
                firstName = RandomFirstName(pawn);
            }

            pawn.Name = new NameTriple(firstName, firstName, lastName);
        }

        public static void NameAllUnnamed()
        {
            if (Current.Game == null)
            {
                return;
            }

            foreach (Pawn pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive)
            {
                GiveNameIfNeeded(pawn);
                ALZ112Medical.GiveUpliftedHediffIfNeeded(pawn);
            }
        }

        private static string RandomFirstName(Pawn pawn)
        {
            NameTriple solidName = PawnBioAndNameGenerator.TryGetRandomUnusedSolidName(pawn.gender);
            if (solidName != null && !string.IsNullOrEmpty(solidName.First))
            {
                return solidName.First;
            }

            Name generated = PawnBioAndNameGenerator.GeneratePawnName(pawn, NameStyle.Full);
            if (generated is NameTriple generatedTriple && !string.IsNullOrEmpty(generatedTriple.First))
            {
                return generatedTriple.First;
            }

            if (generated is NameSingle generatedSingle
                && generatedSingle.Numerical == false
                && !string.IsNullOrEmpty(generatedSingle.Name))
            {
                return generatedSingle.Name;
            }

            return pawn.KindLabel;
        }

        private static string LastNameFor(Pawn pawn)
        {
            string family = TryFamilyLastName(pawn);
            if (!string.IsNullOrEmpty(family))
            {
                return family;
            }

            string defName = pawn.def.defName;
            if (defName.StartsWith(Prefix) && defName.Length > Prefix.Length)
            {
                return defName.Substring(Prefix.Length);
            }

            return defName;
        }

        private static string TryFamilyLastName(Pawn pawn)
        {
            if (pawn.relations == null)
            {
                return null;
            }

            foreach (DirectPawnRelation relation in pawn.relations.DirectRelations)
            {
                if (relation.def != PawnRelationDefOf.Parent)
                {
                    continue;
                }

                if (relation.otherPawn?.Name is NameTriple parentName
                    && !string.IsNullOrEmpty(parentName.Last))
                {
                    return parentName.Last;
                }
            }

            return null;
        }
    }
}
