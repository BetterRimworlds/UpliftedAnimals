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
using Verse.AI;

namespace BetterRimworlds.UpliftedAnimals
{
    // Uplifted wargs only. Vanilla wargs keep the stock animal tree.
    // When food drops below the race hunger line, hunt any reachable map
    // pawn except colony (player-faction) animals and colonists.
    public class HuntWhenStarving : ThinkNode_JobGiver
    {
        public const string UpliftedWargDefName = "Uplifted_Warg";

        // The two whole-map scans below cost a pathfinding reachability check
        // per candidate, so cache their results and re-run them at most once
        // per second instead of on every think tick on crowded maps.
        private const int SearchIntervalTicks = 60;

        // Cached result of the last prey scan, reused while it stays valid.
        private Pawn cachedPrey;
        private int nextPreySearchTick;

        // Cached result of the last edible-food scan, reused while it stays valid.
        private Thing cachedFood;
        private int nextFoodSearchTick;

        // < 0 means use the race's FoodLevelPercentageWantEat.
        public float maxFoodLevelPercentage = -1f;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            HuntWhenStarving copy = (HuntWhenStarving)base.DeepCopy(resolve);
            copy.maxFoodLevelPercentage = this.maxFoodLevelPercentage;
            // base.DeepCopy is a MemberwiseClone; never share scan results or
            // cooldowns with the copy (each pawn gets its own fresh caches).
            copy.cachedPrey = null;
            copy.nextPreySearchTick = 0;
            copy.cachedFood = null;
            copy.nextFoodSearchTick = 0;
            return copy;
        }

        public override float GetPriority(Pawn pawn)
        {
            return this.ShouldHunt(pawn) ? 9.5f : 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!this.ShouldHunt(pawn))
            {
                return null;
            }

            if (this.HasEdibleFood(pawn))
            {
                return null;
            }

            Pawn prey = this.FindPrey(pawn);
            if (prey == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOf.PredatorHunt, prey);
            job.killIncappedTarget = true;
            job.ignoreForbidden = true;
            return job;
        }

        public static bool IsUpliftedWarg(Pawn pawn)
        {
            return pawn?.def != null && pawn.def.defName == UpliftedWargDefName;
        }

        private bool ShouldHunt(Pawn pawn)
        {
            if (!IsUpliftedWarg(pawn))
            {
                return false;
            }

            if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.InMentalState)
            {
                return false;
            }

            if (ALZ112Medical.WantsHospitalBed(pawn))
            {
                return false;
            }

            if (pawn.Map == null || pawn.needs?.food == null)
            {
                return false;
            }

            if (pawn.meleeVerbs.TryGetMeleeVerb(null) == null)
            {
                return false;
            }

            return pawn.needs.food.CurLevelPercentage < this.HungerThreshold(pawn);
        }

        private float HungerThreshold(Pawn pawn)
        {
            if (this.maxFoodLevelPercentage >= 0f)
            {
                return this.maxFoodLevelPercentage;
            }

            return pawn.RaceProps.FoodLevelPercentageWantEat;
        }

        private bool HasEdibleFood(Pawn pawn)
        {
            if (Find.TickManager.TicksGame < this.nextFoodSearchTick)
            {
                // Within the cooldown: reuse the cached food source while it is
                // still edible; otherwise drop the cooldown and rescan.
                if (this.cachedFood != null && IsEdibleFood(pawn, this.cachedFood))
                {
                    return true;
                }

                this.cachedFood = null;
                this.nextFoodSearchTick = 0;
            }

            List<Thing> foods = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);
            for (int i = 0; i < foods.Count; i++)
            {
                Thing food = foods[i];
                if (!IsEdibleFood(pawn, food))
                {
                    continue;
                }

                this.cachedFood = food;
                this.nextFoodSearchTick = Find.TickManager.TicksGame + SearchIntervalTicks;
                return true;
            }

            this.cachedFood = null;
            this.nextFoodSearchTick = Find.TickManager.TicksGame + SearchIntervalTicks;
            return false;
        }

        private static bool IsEdibleFood(Pawn pawn, Thing food)
        {
            if (food == null || !food.Spawned || food is Pawn || !food.IngestibleNow)
            {
                return false;
            }

            if (!pawn.RaceProps.CanEverEat(food))
            {
                return false;
            }

            if (food.IsForbidden(pawn))
            {
                return false;
            }

            return pawn.CanReach(food, PathEndMode.ClosestTouch, Danger.Deadly);
        }

        private Pawn FindPrey(Pawn hunter)
        {
            if (Find.TickManager.TicksGame < this.nextPreySearchTick)
            {
                // Within the cooldown: reuse the previous result while the prey
                // is still a valid, reachable target instead of re-scanning the
                // whole map.
                if (this.cachedPrey != null && this.cachedPrey.Spawned
                    && this.cachedPrey.Map == hunter.Map && IsValidPrey(hunter, this.cachedPrey))
                {
                    return this.cachedPrey;
                }

                // The cached target is gone; drop the cooldown so the next
                // think tick rescans instead of refusing to hunt.
                this.nextPreySearchTick = 0;
                return null;
            }

            Pawn best = null;
            float bestScore = float.MinValue;

            foreach (Pawn prey in hunter.Map.mapPawns.AllPawnsSpawned)
            {
                if (!IsValidPrey(hunter, prey))
                {
                    continue;
                }

                float score = FoodUtility.GetPreyScoreFor(hunter, prey);
                if (best == null || score > bestScore)
                {
                    best = prey;
                    bestScore = score;
                }
            }

            this.cachedPrey = best;
            this.nextPreySearchTick = Find.TickManager.TicksGame + SearchIntervalTicks;
            return best;
        }

        // Whole map. Anything flesh they can physically take down is legal
        // except player-faction animals (and the rest of the colony).
        private static bool IsValidPrey(Pawn hunter, Pawn prey)
        {
            if (prey == null || prey == hunter || prey.Dead || prey.Destroyed)
            {
                return false;
            }

            if (prey.RaceProps == null || !prey.RaceProps.canBePredatorPrey || !prey.RaceProps.IsFlesh)
            {
                return false;
            }

            // Colony animals, colonists, prisoners, slaves, guests.
            if (prey.Faction == Faction.OfPlayer || prey.HostFaction == Faction.OfPlayer)
            {
                return false;
            }

            if (prey.BodySize > hunter.RaceProps.maxPreyBodySize)
            {
                return false;
            }

            return hunter.CanReach(prey, PathEndMode.ClosestTouch, Danger.Deadly);
        }
    }
}
