using System;

namespace OrbitFlock
{
    internal sealed class FlightProgress
    {
        internal const int StartingLives = 3;
        internal const int StartingMissiles = 2;
        internal const int MaxPower = 3;
        internal const int FoodPerMissile = 5;

        internal int Lives { get; private set; } = StartingLives;
        internal int Score { get; private set; }
        internal int Missiles { get; private set; } = StartingMissiles;
        internal int Power { get; private set; } = 1;
        internal int Food { get; private set; }

        internal event Action<int> LivesChanged;
        internal event Action<int> ScoreChanged;
        internal event Action<int> MissilesChanged;
        internal event Action<int> PowerChanged;
        internal event Action<int> FoodChanged;

        internal void Reset() => Restore(new FlightCheckpoint
        {
            Lives = StartingLives,
            Bombs = StartingMissiles,
            Power = 1
        });

        // Loads happen before a scene change. The new HUD reads the full snapshot on enable.
        internal void Restore(FlightCheckpoint saved)
        {
            Lives = saved.Lives;
            Score = saved.Score;
            Missiles = saved.Bombs;
            Power = saved.Power;
            Food = saved.Food;
        }

        internal FlightCheckpoint Capture(int level, int round, int lives) => new FlightCheckpoint
        {
            Level = level,
            Round = round,
            Lives = lives,
            Score = Score,
            Bombs = Missiles,
            Power = Power,
            Food = Food
        };

        internal void LoseLife()
        {
            if (Lives == 0)
                return;

            Lives--;
            ChangePower(Math.Max(1, Power - 1));
            LivesChanged?.Invoke(Lives);
        }

        internal void UpgradeWeapon() => ChangePower(Math.Min(MaxPower, Power + 1));

        internal bool TrySpendMissile()
        {
            if (Missiles == 0)
                return false;

            MissilesChanged?.Invoke(--Missiles);
            return true;
        }

        internal void AddScore(int amount)
        {
            if (amount <= 0)
                return;

            Score = (int)Math.Min((long)Score + amount, int.MaxValue);
            ScoreChanged?.Invoke(Score);
        }

        internal void CollectFood()
        {
            AddScore(50);
            if (++Food == FoodPerMissile)
            {
                Food = 0;
                if (Missiles < int.MaxValue)
                    MissilesChanged?.Invoke(++Missiles);
            }
            FoodChanged?.Invoke(Food);
        }

        private void ChangePower(int next)
        {
            if (next == Power)
                return;

            Power = next;
            PowerChanged?.Invoke(Power);
        }
    }
}
