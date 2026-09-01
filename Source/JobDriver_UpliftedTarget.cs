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
    // Chase and melee a pawn until they are downed. Never finishes
    // a downed target. Pack-friendly: no exclusive reservation.
    public class JobDriver_UpliftedTarget : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.pawn.TryGetComp<CompAttackTarget>()?.ClearAllowedAreaForHunt();

            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() =>
            {
                Pawn prey = this.TargetA.Thing as Pawn;
                return prey == null || prey.Dead || prey.Downed;
            });
            this.FailOn(() => this.pawn.Downed || this.pawn.Dead || this.pawn.InMentalState);

            Toil hunt = Toils_Combat.FollowAndMeleeAttack(TargetIndex.A, delegate
            {
                Thing thing = this.job.GetTarget(TargetIndex.A).Thing;
                Pawn prey = thing as Pawn;
                if (prey == null || prey.Dead || prey.Downed)
                {
                    this.EndJobWith(JobCondition.Succeeded);
                    return;
                }

                this.pawn.meleeVerbs.TryMeleeAttack(thing);
            });
            yield return hunt;
        }
    }
}
