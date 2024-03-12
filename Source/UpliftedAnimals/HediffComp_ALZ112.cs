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

using Verse;

namespace BetterRimworlds.UpliftedAnimals
{
    public class HediffComp_ALZ112 : HediffComp_SeverityPerDay
    {
        public HediffCompProperties_ALZ112 Props
        {
            get
            {
                return this.props as HediffCompProperties_ALZ112;
            }
        }
        
        public override void CompPostMake()
        {
            base.CompPostMake();

            // Log.Error("HediffComp Pawn Label: " + this.Pawn.LabelShort);
            // if (this.Pawn.LabelShort == "Raccoon")
            // {
            //     this.Props.severityPerDay = 0.2f;
            // }
            
        }
    }
}
