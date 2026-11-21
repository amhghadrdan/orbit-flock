using UnityEngine;

namespace OrbitFlock
{
    public enum Formation { Grid, V, Cluster }

    [CreateAssetMenu(menuName = "Orbit Flock/Wave")]
    public sealed class WaveDataSO : ScriptableObject
    {
        public CombatActor Prefab;
        [Min(1)] public int Count = 8;
        [Min(0.02f)] public float SpawnInterval = 0.25f;
        [Min(0.1f)] public float Speed = 4f;
        [Min(1)] public int Health = 1;
        [Min(0)] public int Score = 100;

        [Header("Chicken waves")]
        public Formation Formation;
        [Min(1)] public int Columns = 5;
        public Vector2 EntryStart = new Vector2(-7f, 5.2f);
        public Vector2 EntryControlA = new Vector2(-5.5f, 4.8f);
        public Vector2 EntryControlB = new Vector2(4f, 4f);
        public bool MirrorAlternate;
        public Vector2 EggInterval = new Vector2(4f, 7f);
        [Min(0)] public int DiveEvery = 4;
        [Min(0.1f)] public float DiveSpeed = 7f;
        public Vector2 DiveDelay = new Vector2(2f, 5f);
        [Min(0)] public int GiftCarrier = 1;

        [Header("Asteroid waves")]
        [Min(0.1f)] public float AsteroidSpread = 7.1f;
        [Min(0)] public float AsteroidDrift = 0.6f;
    }
}
