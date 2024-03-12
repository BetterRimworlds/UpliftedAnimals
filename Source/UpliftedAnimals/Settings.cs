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

using UnityEngine;
using Verse;

namespace BetterRimworlds.UpliftedAnimals
{
    class Settings : ModSettings
    {
        private int minutesBetweenUpliftAttempts = 90;
        private float hungerRateThreshold = 150f;

        public int MinutesBetweenUpliftAttempts => minutesBetweenUpliftAttempts;

        public float HungerRateThreshold => hungerRateThreshold;

        public static Settings Get()
        {
            return LoadedModManager.GetMod<BetterRimworlds.UpliftedAnimals.Mod>().GetSettings<Settings>();
        }

        public void DoWindowContents(Rect canvas)
        {
            var options = new Listing_Standard();
            
            options.Begin(canvas);

            options.Gap();

            string minutesBetweenUpliftAttemptsBuffer = minutesBetweenUpliftAttempts.ToString();
            options.TextFieldNumericLabeled(
                "Minutes between Uplift attempts (Default: 90): ",
                ref minutesBetweenUpliftAttempts,
                ref minutesBetweenUpliftAttemptsBuffer,
                10, 24 * 60
            );
            
            options.Gap();

            string hungerRateThresholdBuffer = hungerRateThreshold.ToString();

            // options.TextFieldNumericLabeled("Increased Hunger Rate: ", ref hungerRateThreshold, ref hungerRateThresholdBuffer, 0, 100);
            //
            // options.Gap();

            options.End();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref minutesBetweenUpliftAttempts, "MinutesBetweenUpliftAttempts", 90);
            Scribe_Values.Look(ref hungerRateThreshold, "HungerRateThreshold", 150f);
        }
    }
}
