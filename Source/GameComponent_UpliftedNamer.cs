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
using Verse;

namespace BetterRimworlds.UpliftedAnimals
{
    // FillComponents auto-instantiates every GameComponent subclass.
    // Catches caravan births and already-spawned numeric names the
    // race comp cannot see until the pawn is on a map.
    public class GameComponent_UpliftedNamer : GameComponent
    {
        private const int CheckIntervalTicks = 60;

        public GameComponent_UpliftedNamer(Game game)
        {
        }

        public override void FinalizeInit()
        {
            UpliftedNamer.NameAllUnnamed();
            LongEventHandler.ExecuteWhenFinished(Designator_SlaughterUplifted.EnsureReverseDesignator);
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % CheckIntervalTicks != 0)
            {
                return;
            }

            UpliftedNamer.NameAllUnnamed();
            Designator_SlaughterUplifted.EnsureReverseDesignator();
        }
    }
}
