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
    // Player-ordered Attack on uplifted animals with directedAttack.
    // Target any pawn (including colonists) and melee them until they go down.
    public static class UpliftedTarget
    {
        public static IEnumerable<Gizmo> GetGizmos(Pawn hunter)
        {
            if (!DirectedAttack.Has(hunter) || hunter.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            Command_Action attack = new Command_Action();
            attack.defaultLabel = "Attack";
            attack.defaultDesc = "Order this animal to attack any pawn until they are downed. "
                + "Colonists, animals, visitors, and enemies are all valid. "
                + "The attack stops once the target is downed and will not finish them off.";
            attack.icon = TexCommand.Attack;
            attack.action = BeginTargetingSelected;

            AcceptanceReport canAttack = CanIssueTarget(hunter);
            if (!canAttack.Accepted)
            {
                attack.Disable(canAttack.Reason);
            }

            yield return attack;
        }

        public static AcceptanceReport CanIssueTarget(Pawn hunter)
        {
            if (hunter == null || !hunter.Spawned || hunter.Map == null)
            {
                return "Not spawned.";
            }

            if (hunter.Dead || hunter.Destroyed)
            {
                return "Dead.";
            }

            if (hunter.Downed)
            {
                return "Downed.";
            }

            if (hunter.InMentalState)
            {
                return "In mental state.";
            }

            if (hunter.jobs == null)
            {
                return "Cannot take jobs.";
            }

            if (hunter.meleeVerbs == null || hunter.meleeVerbs.TryGetMeleeVerb(null) == null)
            {
                return "Cannot melee.";
            }

            return true;
        }

        public static bool IsValidPrey(Pawn hunter, Pawn prey)
        {
            if (prey == null || hunter == null || prey == hunter)
            {
                return false;
            }

            if (prey.Dead || prey.Destroyed || prey.Downed)
            {
                return false;
            }

            return prey.Spawned && prey.Map == hunter.Map;
        }

        public static void OrderTarget(Pawn hunter, Pawn prey)
        {
            if (!CanIssueTarget(hunter).Accepted || !IsValidPrey(hunter, prey))
            {
                return;
            }

            JobDef jobDef = BRW_JobDefOf.BRW_UpliftedTarget;
            if (jobDef == null)
            {
                jobDef = JobDefOf.AttackMelee;
            }

            if (jobDef == null || hunter.jobs == null)
            {
                return;
            }

            Job job = JobMaker.MakeJob(jobDef, prey);
            job.playerForced = true;
            job.killIncappedTarget = false;
            job.ignoreForbidden = true;
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            job.expiryInterval = 999999;

            hunter.jobs.StopAll(false);
            hunter.jobs.StartJob(job, JobCondition.InterruptForced);
            Dialog_AttackAnimals.CloseIfOpen();
        }

        private static void BeginTargetingSelected()
        {
            Pawn caster = FirstSelectedHunter();
            if (caster == null)
            {
                return;
            }

            TargetingParameters parms = MakePawnTargetRules();
            parms.validator = IsTargetValidForSelection;
            StartPawnTargeting(caster, OrderSelectedHunters, parms);
        }

        public static TargetingParameters MakePawnTargetRules()
        {
            TargetingParameters rules = new TargetingParameters();
            rules.canTargetPawns = true;
            rules.canTargetAnimals = true;
            rules.canTargetHumans = true;
            rules.canTargetMechs = true;
            rules.canTargetSelf = false;
            rules.canTargetBuildings = false;
            rules.canTargetItems = false;
            rules.neverTargetIncapacitated = true;
            rules.mustBeSelectable = false;
            rules.mapObjectTargetsMustBeAutoAttackable = false;
            return rules;
        }

        public static void StartPawnTargeting(
            Pawn lookFrom,
            System.Action<LocalTargetInfo> whenClicked,
            TargetingParameters rules)
        {
#if RIMWORLD15 || RIMWORLD16
            Find.Targeter.BeginTargeting(rules, whenClicked, lookFrom, null, TexCommand.Attack, true);
#else
            Find.Targeter.BeginTargeting(rules, whenClicked, lookFrom, null, TexCommand.Attack);
#endif
        }

        private static bool IsTargetValidForSelection(TargetInfo info)
        {
            Pawn prey = info.Thing as Pawn;
            if (prey == null || prey.Dead || prey.Destroyed || prey.Downed)
            {
                return false;
            }

            List<Pawn> selected = Find.Selector.SelectedPawns;
            for (int i = 0; i < selected.Count; i++)
            {
                Pawn hunter = selected[i];
                if (CanIssueTarget(hunter).Accepted && IsValidPrey(hunter, prey))
                {
                    return true;
                }
            }

            return false;
        }

        private static void OrderSelectedHunters(LocalTargetInfo target)
        {
            Pawn prey = target.Thing as Pawn;
            if (prey == null)
            {
                return;
            }

            List<Pawn> selected = Find.Selector.SelectedPawns;
            for (int i = 0; i < selected.Count; i++)
            {
                Pawn hunter = selected[i];
                if (!DirectedAttack.Has(hunter))
                {
                    continue;
                }

                OrderTarget(hunter, prey);
            }
        }

        private static Pawn FirstSelectedHunter()
        {
            List<Pawn> selected = Find.Selector.SelectedPawns;
            for (int i = 0; i < selected.Count; i++)
            {
                Pawn hunter = selected[i];
                if (DirectedAttack.Has(hunter) && CanIssueTarget(hunter).Accepted)
                {
                    return hunter;
                }
            }

            return null;
        }
    }
}
