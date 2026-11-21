using System;
using UnityEngine;

namespace OrbitFlock
{
    [CreateAssetMenu(menuName = "Orbit Flock/Level")]
    public sealed class LevelDataSO : ScriptableObject
    {
        [Min(1)] public int Number = 1;
        public string SceneName;
        public string Theme;
        public Sprite Backdrop;
        public AudioClip Music;
        public RoundData[] Rounds;
        public BossData Boss;
    }

    [Serializable]
    public sealed class RoundData
    {
        public WaveDataSO Wave;
        [Min(0)] public int CountOverride;
        [Min(0.1f)] public float Difficulty = 1f;

        public int Count => CountOverride > 0 ? CountOverride : Wave.Count;
    }

    [Serializable]
    public sealed class BossData
    {
        public string Name;
        [Min(1)] public int Health = 60;
        [Range(3, 9)] public int VolleyShots = 5;
        [Min(0.15f)] public float VolleyInterval = 1.2f;
        [Min(1f)] public float ShotSpeed = 4f;
        [Min(1f)] public float DiveSpeed = 8f;
    }
}
