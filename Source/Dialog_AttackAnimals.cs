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
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BetterRimworlds.UpliftedAnimals
{
    public class Dialog_AttackAnimals : Window
    {
        private const float RowHeight = 48f;
        private const float IconSize = 40f;
        private const float HeaderHeight = 32f;

        private static readonly List<Pawn> Cached = new List<Pawn>();

        private Vector2 scroll;

        private const float LeftMargin = 80f;

        public override Vector2 InitialSize
        {
            get { return new Vector2(380f, 460f); }
        }

        public Dialog_AttackAnimals()
        {
            this.doCloseX = true;
            this.draggable = true;
            this.resizeable = true;
            this.absorbInputAroundWindow = false;
            this.preventCameraMotion = false;
            this.closeOnClickedOutside = false;
            this.forcePause = false;
#if !RIMWORLD12 && !RIMWORLD13
            this.drawInScreenshotMode = false;
#endif
            this.optionalTitle = "BRW_AttackAnimals".Translate();
        }

        protected override void SetInitialSizeAndPosition()
        {
            Vector2 size = this.InitialSize;
            float y = ((float)UI.screenHeight - size.y) / 2f;
            if (y < 0f)
            {
                y = 0f;
            }

            this.windowRect = new Rect(LeftMargin, y, size.x, size.y).Rounded();
        }

        public static void CloseIfOpen()
        {
            if (Find.WindowStack == null || !Find.WindowStack.IsOpen(typeof(Dialog_AttackAnimals)))
            {
                return;
            }

            Find.WindowStack.TryRemove(typeof(Dialog_AttackAnimals));
        }

        public override void ExtraOnGUI()
        {
            if (Event.current.type != EventType.MouseDown)
            {
                return;
            }

            Vector2 mouse = UI.MousePositionOnUIInverted;
            if (this.windowRect.Contains(mouse) || AttackAnimalsHud.HitTest(mouse))
            {
                return;
            }

            this.Close();
        }

        public override void DoWindowContents(Rect inRect)
        {
            AttackAnimals.CollectOnMap(Find.CurrentMap, Cached);

            Rect header = new Rect(inRect.x, inRect.y, inRect.width, HeaderHeight);
            if (Cached.Count > 0
                && Widgets.ButtonText(new Rect(header.x, header.y, 120f, HeaderHeight - 4f),
                    "BRW_AttackAnimalsSelectAll".Translate()))
            {
                SelectAllReady();
            }

            Rect listRect = new Rect(inRect.x, inRect.y + HeaderHeight + 4f,
                inRect.width, inRect.height - HeaderHeight - 4f);

            if (Cached.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(listRect, "BRW_AttackAnimalsNone".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            float viewHeight = Cached.Count * RowHeight;
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(listRect, ref this.scroll, viewRect);

            for (int i = 0; i < Cached.Count; i++)
            {
                DrawRow(new Rect(0f, i * RowHeight, viewRect.width, RowHeight), Cached[i]);
            }

            Widgets.EndScrollView();
        }

        private static void DrawRow(Rect rect, Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (Find.Selector.IsSelected(pawn))
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else
            {
                Widgets.DrawHighlightIfMouseover(rect);
            }

            Rect icon = new Rect(rect.x + 4f, rect.y + (rect.height - IconSize) / 2f, IconSize, IconSize);
            Widgets.ThingIcon(icon, pawn);

            bool ready = AttackAnimals.CanTakeAttackOrder(pawn);
            Color old = GUI.color;
            if (!ready)
            {
                GUI.color = Color.gray;
            }

            Rect text = new Rect(icon.xMax + 8f, rect.y + 4f, rect.width - icon.width - 16f, rect.height - 8f);
            StringBuilder line = new StringBuilder();
            line.Append(pawn.LabelShort.CapitalizeFirst());
            line.Append("  ");
            line.Append(pawn.def.label);
            line.AppendLine();
            line.Append("BRW_AttackAnimalsDps".Translate(AttackAnimals.MeleeDps(pawn).ToString("0.0")));
            if (pawn.Downed)
            {
                line.Append("  ");
                line.Append("BRW_AttackAnimalsDowned".Translate());
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(text, line.ToString().TrimEnd());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = old;

            if (Widgets.ButtonInvisible(rect))
            {
                SelectPawn(pawn);
            }
        }

        private static void SelectAllReady()
        {
            Find.Selector.ClearSelection();
            for (int i = 0; i < Cached.Count; i++)
            {
                Pawn pawn = Cached[i];
                if (AttackAnimals.CanTakeAttackOrder(pawn))
                {
                    Find.Selector.Select(pawn);
                }
            }
        }

        private static void SelectPawn(Pawn pawn)
        {
            if (Event.current.shift)
            {
                if (Find.Selector.IsSelected(pawn))
                {
                    Find.Selector.Deselect(pawn);
                }
                else
                {
                    Find.Selector.Select(pawn);
                }

                return;
            }

            Find.Selector.ClearSelection();
            Find.Selector.Select(pawn);
            SoundDefOf.Click.PlayOneShotOnCamera();
        }
    }
}
