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
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterRimworlds.UpliftedAnimals
{
    // Player-faction animals that have this mod's Attack order.
    public static class AttackAnimals
    {
        public static bool IsAttackAnimal(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                return false;
            }

            if (pawn.Faction != Faction.OfPlayer)
            {
                return false;
            }

            if (!pawn.Spawned || pawn.Map == null)
            {
                return false;
            }

            return DirectedAttack.Has(pawn);
        }

        public static int CountOnMap(Map map)
        {
            if (map?.mapPawns == null)
            {
                return 0;
            }

            int n = 0;
            List<Pawn> pawns = map.mapPawns.PawnsInFaction(Faction.OfPlayer);
            for (int i = 0; i < pawns.Count; i++)
            {
                if (IsAttackAnimal(pawns[i]))
                {
                    n++;
                }
            }

            return n;
        }

        public static void CollectOnMap(Map map, List<Pawn> into)
        {
            into.Clear();
            if (map?.mapPawns == null)
            {
                return;
            }

            List<Pawn> pawns = map.mapPawns.PawnsInFaction(Faction.OfPlayer);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (IsAttackAnimal(pawn))
                {
                    into.Add(pawn);
                }
            }

            into.Sort(CompareLethality);
        }

        public static float MeleeDps(Pawn pawn)
        {
            if (pawn == null || StatDefOf.MeleeDPS == null)
            {
                return 0f;
            }

            return pawn.GetStatValue(StatDefOf.MeleeDPS);
        }

        public static bool CanTakeAttackOrder(Pawn pawn)
        {
            return UpliftedTarget.CanIssueTarget(pawn).Accepted;
        }

        private static int CompareLethality(Pawn a, Pawn b)
        {
            int downed = (a.Downed ? 1 : 0) - (b.Downed ? 1 : 0);
            if (downed != 0)
            {
                return downed;
            }

            int dps = b.GetStatValue(StatDefOf.MeleeDPS).CompareTo(a.GetStatValue(StatDefOf.MeleeDPS));
            if (dps != 0)
            {
                return dps;
            }

            float aPower = a.kindDef == null ? 0f : a.kindDef.combatPower;
            float bPower = b.kindDef == null ? 0f : b.kindDef.combatPower;
            int power = bPower.CompareTo(aPower);
            if (power != 0)
            {
                return power;
            }

            return string.Compare(a.LabelShort, b.LabelShort, StringComparison.OrdinalIgnoreCase);
        }
    }
}
