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
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace BetterRimworlds.UpliftedAnimals
{
    // Game-layer ImmediateWindow parked beside the colony bar.
    // Requested from GameComponentOnGUI; drawn later on the window stack.
    public static class AttackAnimalsHud
    {
        private const int WindowId = 847362915;
        private const float Gap = 8f;
        private const float ExtraSpacer = 16f;
        private const float FallbackTop = 21f;
        private const float MinSize = 32f;

        private static float buttonSize = MinSize;

        public static bool HitTest(Vector2 screenPos)
        {
            return ButtonRect().Contains(screenPos);
        }

        public static void OnGUI()
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            if (Find.WindowStack == null || Find.CurrentMap == null)
            {
                return;
            }

            if (LookingAtPlanet())
            {
                return;
            }

            if (Find.ScreenshotModeHandler != null && Find.ScreenshotModeHandler.Active)
            {
                return;
            }

            Rect rect = ButtonRect();
            Find.WindowStack.ImmediateWindow(
                WindowId,
                rect,
                WindowLayer.GameUI,
                DrawButton,
                false,
                false,
                0f);
        }

        private static void DrawButton()
        {
            Rect inner = new Rect(0f, 0f, buttonSize, buttonSize);
            int count = AttackAnimals.CountOnMap(Find.CurrentMap);
            bool open = Find.WindowStack.IsOpen(typeof(Dialog_AttackAnimals));

            GUI.DrawTexture(inner, ColonistBar.BGTex);
            if (open)
            {
                Widgets.DrawBox(inner, 2);
            }

            Rect icon = inner.ContractedBy(4f);
            if (Widgets.ButtonImage(icon, TexCommand.Attack))
            {
                ToggleDialog();
            }

            TooltipHandler.TipRegion(
                inner,
                "BRW_AttackAnimals".Translate() + " (" + count + ")\n"
                + "BRW_AttackAnimalsDesc".Translate());

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerRight;
            Widgets.Label(inner.ContractedBy(2f), count.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static void ToggleDialog()
        {
            if (Find.WindowStack.IsOpen(typeof(Dialog_AttackAnimals)))
            {
                Find.WindowStack.TryRemove(typeof(Dialog_AttackAnimals));
                return;
            }

            Find.WindowStack.Add(new Dialog_AttackAnimals());
        }

        private static Rect ButtonRect()
        {
            ColonistBar bar = Find.ColonistBar;
            float size = bar != null ? bar.Size.y : MinSize;
            if (size < MinSize)
            {
                size = MinSize;
            }

            buttonSize = size;

            Vector2 pos;
            if (Find.PlaySettings != null
                && Find.PlaySettings.showColonistBar
                && bar != null
                && bar.DrawLocs != null
                && bar.DrawLocs.Count > 0)
            {
                List<Vector2> locs = bar.DrawLocs;
                Vector2 barSize = bar.Size;
                float minX = float.MaxValue;
                float maxX = float.MinValue;
                float minY = float.MaxValue;
                for (int i = 0; i < locs.Count; i++)
                {
                    minX = Mathf.Min(minX, locs[i].x);
                    maxX = Mathf.Max(maxX, locs[i].x + barSize.x);
                    minY = Mathf.Min(minY, locs[i].y);
                }

                float spacer = bar.SpaceBetweenColonistsHorizontal + ExtraSpacer * bar.Scale;
                if (spacer < Gap + ExtraSpacer)
                {
                    spacer = Gap + ExtraSpacer;
                }

                float leftX = minX - spacer - size;
                if (leftX < Gap)
                {
                    pos = new Vector2(maxX + spacer, minY);
                }
                else
                {
                    pos = new Vector2(leftX, minY);
                }
            }
            else
            {
                pos = new Vector2((UI.screenWidth - size) * 0.5f + 80f, FallbackTop);
            }

            pos.x = Mathf.Clamp(pos.x, Gap, UI.screenWidth - size - Gap);
            pos.y = Mathf.Clamp(pos.y, Gap, UI.screenHeight - size - Gap);
            return new Rect(pos.x, pos.y, size, size);
        }

        private static bool LookingAtPlanet()
        {
#if RIMWORLD16
            return WorldRendererUtility.WorldSelected;
#else
            return WorldRendererUtility.WorldRenderedNow;
#endif
        }
    }
}
