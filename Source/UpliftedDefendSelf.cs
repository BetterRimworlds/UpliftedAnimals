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
    // Colony Uplifted_* animals. Vanilla Animal AI on a player home map
    // neither flees nor fights (ShouldAnimalFleeDanger is false; Release
    // only works while a drafted master has the toggle on). They sit on
    // Wander while raiders shoot them. This closes that gap: melee any
    // hostile they can see and reach.
    public class UpliftedDefendSelf : ThinkNode_JobGiver
    {
        public float targetAcquireRadius = 25f;

        private const int RecentlyHarmedTicks = 250;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            UpliftedDefendSelf copy = (UpliftedDefendSelf)base.DeepCopy(resolve);
            copy.targetAcquireRadius = this.targetAcquireRadius;
            return copy;
        }

        public override float GetPriority(Pawn pawn)
        {
            return this.CanFight(pawn) ? 9.6f : 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!this.CanFight(pawn))
            {
                return null;
            }

            Thing threat = this.FindThreat(pawn);
            if (threat == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, threat);
            job.killIncappedTarget = false;
            job.ignoreForbidden = true;
            job.expiryInterval = 450;
            job.checkOverrideOnExpire = true;
            job.expireRequiresEnemiesNearby = true;
            job.collideWithPawns = true;
            return job;
        }

        private bool CanFight(Pawn pawn)
        {
            if (pawn == null || !UpliftedNamer.IsUplifted(pawn.def))
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

            if (pawn.Faction != Faction.OfPlayer)
            {
                return false;
            }

            if (pawn.Map == null || pawn.jobs == null)
            {
                return false;
            }

            if (pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                return false;
            }

            if (pawn.meleeVerbs == null || pawn.meleeVerbs.TryGetMeleeVerb(null) == null)
            {
                return false;
            }

            return true;
        }

        private Thing FindThreat(Pawn pawn)
        {
            List<IAttackTarget> candidates = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            bool recentlyHarmed = pawn.mindState != null
                && Find.TickManager.TicksGame - pawn.mindState.lastHarmTick < RecentlyHarmedTicks;
            float maxDistSq = this.targetAcquireRadius * this.targetAcquireRadius;
            Thing best = null;
            float bestDistSq = maxDistSq;

            for (int i = 0; i < candidates.Count; i++)
            {
                IAttackTarget candidate = candidates[i];
                Thing thing = candidate == null ? null : candidate.Thing;
                if (thing == null || thing.Destroyed || !thing.Spawned)
                {
                    continue;
                }

                if (candidate.ThreatDisabled(pawn))
                {
                    continue;
                }

                if (!thing.HostileTo(pawn))
                {
                    continue;
                }

                Pawn other = thing as Pawn;
                if (other != null && (other.Dead || other.Downed))
                {
                    continue;
                }

                float distSq = pawn.Position.DistanceToSquared(thing.Position);
                if (distSq > maxDistSq)
                {
                    continue;
                }

                if (!recentlyHarmed
                    && !GenSight.LineOfSight(pawn.Position, thing.Position, pawn.Map, true))
                {
                    continue;
                }

                if (!pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                if (best == null || distSq < bestDistSq)
                {
                    best = thing;
                    bestDistSq = distSq;
                }
            }

            return best;
        }
    }
}
