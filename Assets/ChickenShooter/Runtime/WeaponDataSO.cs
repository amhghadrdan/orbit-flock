using System;
using UnityEngine;

namespace OrbitFlock
{
    [CreateAssetMenu(menuName = "Orbit Flock/Weapon")]
    public sealed class WeaponDataSO : ScriptableObject
    {
        public string DisplayName;
        public Sprite Sprite;
        public CombatActor Projectile;
        [Min(1)] public int Damage = 1;
        [Min(0.1f)] public float Speed = 15f;
        [Min(0.1f)] public float ShotsPerSecond = 5f;
        public ShotPattern[] Shots;
    }

    [Serializable]
    public struct ShotPattern
    {
        public float HorizontalOffset;
        public float AngleDegrees;

        public ShotPattern(float horizontalOffset, float angleDegrees)
        {
            HorizontalOffset = horizontalOffset;
            AngleDegrees = angleDegrees;
        }
    }
}
