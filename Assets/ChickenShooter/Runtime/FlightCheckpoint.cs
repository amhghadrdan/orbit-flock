using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OrbitFlock.PlayModeTests")]

namespace OrbitFlock
{
    [Serializable]
    internal sealed class FlightCheckpoint
    {
        // Field names form the save format used by previously released builds.
        public int Level;
        public int Round;
        public int Score;
        public int Lives;
        public int Bombs;
        public int Power;
        public int Food;

        internal bool Fits(LevelDataSO level)
        {
            return level != null && Round >= 1 && Round <= level.Rounds.Length
                && Lives >= 1 && Lives <= FlightProgress.StartingLives && Score >= 0 && Bombs >= 0
                && Power >= 1 && Power <= FlightProgress.MaxPower && Food >= 0 && Food < FlightProgress.FoodPerMissile;
        }
    }
}
