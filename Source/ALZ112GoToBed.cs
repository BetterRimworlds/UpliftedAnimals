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
using Verse.AI;

namespace BetterRimworlds.UpliftedAnimals
{
    // 1.6 only treats Downed / tendable / surgery+available-doctor as
    // medical rest. ALZ-112 patients must walk to a bed and stay there
    // so Administer bills can start (CurrentlyUsableForBills requires InBed).
    public class ALZ112GoToBed : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            return ALZ112Medical.WantsHospitalBed(pawn) ? 10f : 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!ALZ112Medical.WantsHospitalBed(pawn))
            {
                return null;
            }

            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed)
            {
                return null;
            }

            if (pawn.InMentalState)
            {
                return null;
            }

            if (RestUtility.DisturbancePreventsLyingDown(pawn))
            {
                return null;
            }

            Building_Bed current = pawn.CurrentBed();
            if (current != null && this.BedIsGoodEnough(pawn, current))
            {
                if (pawn.CurJob != null && pawn.CurJob.restUntilHealed)
                {
                    return null;
                }

                Job stay = JobMaker.MakeJob(JobDefOf.LayDown, current);
                stay.restUntilHealed = true;
                stay.checkOverrideOnExpire = true;
                return stay;
            }

            Building_Bed bed = this.FindBed(pawn);
            if (bed == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOf.LayDown, bed);
            job.restUntilHealed = true;
            job.checkOverrideOnExpire = true;
            return job;
        }

        private bool BedIsGoodEnough(Pawn pawn, Building_Bed bed)
        {
            if (bed == null || !bed.Spawned)
            {
                return false;
            }

            if (pawn.RaceProps != null && pawn.RaceProps.Humanlike)
            {
                return bed.Medical;
            }

            return true;
        }

        private Building_Bed FindBed(Pawn pawn)
        {
            Building_Bed bed = RestUtility.FindPatientBedFor(pawn);
            if (bed != null)
            {
                return bed;
            }

            return RestUtility.FindBedFor(pawn);
        }
    }
}
