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
using UnityEngine;
using Verse;
using Verse.AI;

namespace BetterRimworlds.UpliftedAnimals
{
    // Player order: a mastered animal that finished Attack Target
    // training strikes one pawn the master can currently see.
    public static class UpliftedAttackTarget
    {
        // Shared with 1.6+ Odyssey so a trained save still finds this skill.
        // Behavior, range, and buttons below are ours.
        public const string DefName = "AttackTarget";

        public const float MasterPointRange = 30f;

        public static TrainableDef Def
        {
            get { return DefDatabase<TrainableDef>.GetNamedSilentFail(DefName); }
        }

        public static bool KnowsTheSkill(Pawn animal)
        {
            TrainableDef skill = Def;
            return skill != null
                && animal != null
                && animal.training != null
                && animal.training.HasLearned(skill);
        }

        public static AcceptanceReport ReadyToBePointed(Pawn animal)
        {
            if (!KnowsTheSkill(animal) || animal.Faction != Faction.OfPlayer)
            {
                return false;
            }

            AcceptanceReport bodyReady = UpliftedTarget.CanIssueTarget(animal);
            if (!bodyReady.Accepted)
            {
                return bodyReady;
            }

            Pawn handler = animal.playerSettings == null ? null : animal.playerSettings.Master;
            if (handler == null)
            {
                return "BRW_AttackTargetNeedsMaster".Translate();
            }

            if (!handler.Spawned || handler.Map != animal.Map)
            {
                return "BRW_AttackTargetMasterAway".Translate();
            }

            return true;
        }

        public static bool MasterCanPointAt(Pawn handler, Pawn victim)
        {
            if (handler == null || victim == null || handler == victim)
            {
                return false;
            }

            if (!victim.Spawned || victim.Map != handler.Map)
            {
                return false;
            }

            if (victim.Dead || victim.Destroyed || victim.Downed)
            {
                return false;
            }

            float dist = (victim.Position - handler.Position).LengthHorizontal;
            if (dist > MasterPointRange)
            {
                return false;
            }

            return GenSight.LineOfSight(handler.Position, victim.Position, handler.Map, true);
        }

        public static IEnumerable<Gizmo> ButtonsForAnimal(Pawn animal)
        {
            if (!KnowsTheSkill(animal) || animal.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            Command_Action point = new Command_Action();
            point.defaultLabel = "BRW_AttackTarget".Translate();
            point.defaultDesc = "BRW_AttackTargetDesc".Translate();
            point.icon = TexCommand.Attack;
            point.action = PointFromSelectedAnimals;

            AcceptanceReport ready = ReadyToBePointed(animal);
            if (!ready.Accepted)
            {
                point.Disable(ready.Reason);
            }

            yield return point;

            if (IsOnPointedJob(animal))
            {
                Command_Action stop = new Command_Action();
                stop.defaultLabel = "BRW_AttackTargetCancel".Translate();
                stop.defaultDesc = "BRW_AttackTargetCancelDesc".Translate();
                stop.icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
                stop.action = StopSelectedPointedJobs;
                yield return stop;
            }
        }

        public static IEnumerable<Gizmo> ButtonsForHandler(Pawn handler)
        {
            if (handler == null || !handler.Drafted || handler.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            int waiting = CountReadyAnimals(handler);
            if (waiting <= 0)
            {
                yield break;
            }

            Command_Action point = new Command_Action();
            point.defaultLabel = "BRW_AttackTargetMaster".Translate() + " (" + waiting + ")";
            point.defaultDesc = "BRW_AttackTargetMasterDesc".Translate();
            point.icon = TexCommand.Attack;
            point.action = () => PointFromHandler(handler);
            yield return point;
        }

        private static int CountReadyAnimals(Pawn handler)
        {
            int n = 0;
            foreach (Pawn animal in AnimalsAssignedTo(handler))
            {
                if (ReadyToBePointed(animal).Accepted)
                {
                    n++;
                }
            }

            return n;
        }

        private static IEnumerable<Pawn> AnimalsAssignedTo(Pawn handler)
        {
            if (handler == null || handler.Map == null)
            {
                yield break;
            }

            List<Pawn> factionPawns = handler.Map.mapPawns.PawnsInFaction(handler.Faction);
            for (int i = 0; i < factionPawns.Count; i++)
            {
                Pawn other = factionPawns[i];
                if (other.playerSettings != null
                    && other.playerSettings.Master == handler
                    && KnowsTheSkill(other))
                {
                    yield return other;
                }
            }
        }

        private static void PointFromSelectedAnimals()
        {
            Pawn first = null;
            List<Pawn> selected = Find.Selector.SelectedPawns;
            for (int i = 0; i < selected.Count; i++)
            {
                if (ReadyToBePointed(selected[i]).Accepted)
                {
                    first = selected[i];
                    break;
                }
            }

            if (first == null)
            {
                return;
            }

            OpenPointer(first.playerSettings.Master, SendSelectedAnimals);
        }

        private static void PointFromHandler(Pawn handler)
        {
            OpenPointer(handler, click => SendHandlersAnimals(handler, click));
        }

        private static void OpenPointer(Pawn handler, System.Action<LocalTargetInfo> whenClicked)
        {
            TargetingParameters rules = UpliftedTarget.MakePawnTargetRules();
            rules.validator = info =>
            {
                Pawn victim = info.Thing as Pawn;
                return MasterCanPointAt(handler, victim);
            };

            UpliftedTarget.StartPawnTargeting(handler, whenClicked, rules);
        }

        private static void SendSelectedAnimals(LocalTargetInfo click)
        {
            Pawn victim = click.Thing as Pawn;
            if (victim == null)
            {
                return;
            }

            List<Pawn> selected = Find.Selector.SelectedPawns;
            Pawn handler = null;
            for (int i = 0; i < selected.Count; i++)
            {
                if (ReadyToBePointed(selected[i]).Accepted)
                {
                    handler = selected[i].playerSettings.Master;
                    break;
                }
            }

            if (handler == null)
            {
                return;
            }

            if (!MasterCanPointAt(handler, victim))
            {
                Messages.Message("BRW_AttackTargetNotVisible".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            for (int i = 0; i < selected.Count; i++)
            {
                TrySend(selected[i], victim);
            }
        }

        private static void SendHandlersAnimals(Pawn handler, LocalTargetInfo click)
        {
            Pawn victim = click.Thing as Pawn;
            if (victim == null)
            {
                return;
            }

            if (!MasterCanPointAt(handler, victim))
            {
                Messages.Message("BRW_AttackTargetNotVisible".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            foreach (Pawn animal in AnimalsAssignedTo(handler))
            {
                TrySend(animal, victim);
            }
        }

        private static void TrySend(Pawn animal, Pawn victim)
        {
            if (!ReadyToBePointed(animal).Accepted)
            {
                return;
            }

            if (!UpliftedTarget.IsValidPrey(animal, victim))
            {
                return;
            }

            UpliftedTarget.OrderTarget(animal, victim);
        }

        private static bool IsOnPointedJob(Pawn animal)
        {
            return animal.CurJobDef == BRW_JobDefOf.BRW_UpliftedTarget;
        }

        private static void StopSelectedPointedJobs()
        {
            List<Pawn> selected = Find.Selector.SelectedPawns;
            for (int i = 0; i < selected.Count; i++)
            {
                Pawn animal = selected[i];
                if (!IsOnPointedJob(animal) || animal.jobs == null)
                {
                    continue;
                }

                animal.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
    }
}
