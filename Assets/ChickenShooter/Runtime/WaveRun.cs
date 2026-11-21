using UnityEngine;

namespace OrbitFlock
{
    internal sealed class WaveRun
    {
        private readonly RoundData round;
        private int spawned;
        private int enemiesAlive;
        private float nextSpawn;

        internal WaveRun(RoundData round)
        {
            this.round = round;
        }

        internal bool IsComplete => spawned >= round.Count && enemiesAlive == 0;
        internal bool IsAsteroidWave => round.Wave.Prefab.Kind == ActorKind.Asteroid;

        internal void Launch(float now) => nextSpawn = now;

        internal void Advance(float now, CombatWorld world)
        {
            if (spawned >= round.Count || now < nextSpawn)
                return;

            WaveDataSO wave = round.Wave;
            int index = spawned++;
            enemiesAlive++;
            nextSpawn = now + wave.SpawnInterval / round.Difficulty;
            int health = Mathf.Max(1, Mathf.RoundToInt(wave.Health * round.Difficulty));

            if (IsAsteroidWave)
            {
                const float laneStep = 0.618034f;
                float lane = Mathf.Repeat(0.33f + index * laneStep, 1f);
                float x = Mathf.Lerp(-wave.AsteroidSpread, wave.AsteroidSpread, lane);
                var position = new Vector2(x, PlayfieldCamera.HalfHeight + 0.85f);
                var velocity = new Vector2(-Mathf.Sign(x) * wave.AsteroidDrift, -wave.Speed * round.Difficulty);
                world.Spawn(wave.Prefab, position, health).ConfigureAsteroid(wave, velocity);
                return;
            }

            Vector2 start = wave.EntryStart;
            if (wave.MirrorAlternate && index % 2 == 1)
                start.x = -start.x;
            world.Spawn(wave.Prefab, start, health)
                .ConfigureChicken(wave, FormationSlot(wave, index, round.Count), index, round.Difficulty);
        }

        internal void OnActorReleased(CombatActor actor)
        {
            if (actor.Kind == ActorKind.Chicken || actor.Kind == ActorKind.Asteroid)
                enemiesAlive--;
        }

        private static Vector2 FormationSlot(WaveDataSO wave, int index, int count)
        {
            int row = index / wave.Columns;
            int columnsInRow = Mathf.Min(wave.Columns, count - row * wave.Columns);
            float column = index % wave.Columns - (columnsInRow - 1) * 0.5f;
            switch (wave.Formation)
            {
                case Formation.V:
                    return new Vector2(column * 1.42f, 1.65f + Mathf.Abs(column) * 0.47f - row * 0.91f);
                case Formation.Cluster:
                    return new Vector2(column * 0.98f + (row % 2 == 0 ? -0.22f : 0.22f), 3.0f - row * 0.68f);
                default:
                    return new Vector2(column * 1.50f, 2.95f - row * 0.87f);
            }
        }
    }
}
