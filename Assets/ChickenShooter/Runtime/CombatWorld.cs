using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OrbitFlock
{
    internal sealed class CombatWorld
    {
        private readonly Transform actorRoot;
        private readonly PlayerShip player;
        private readonly FlightProgress progress;
        private readonly FlightAudio audio;
        private readonly CombatActor eggPrefab, giftPrefab, foodPrefab;
        private readonly Sprite feather;
        private readonly Dictionary<CombatActor, ActorPool> pools = new Dictionary<CombatActor, ActorPool>();
        private readonly HashSet<CombatActor> active = new HashSet<CombatActor>();
        private readonly List<CombatActor> selection = new List<CombatActor>();
        private readonly HashSet<CombatActor> minions = new HashSet<CombatActor>();
        private RoundData bossRound;
        private int summonIndex;

        internal CombatWorld(Transform actorRoot, PlayerShip player, GameManager game,
            CombatActor eggPrefab, CombatActor giftPrefab, CombatActor foodPrefab, Sprite feather)
        {
            this.actorRoot = actorRoot;
            this.player = player;
            progress = game.Progress;
            audio = game.Audio;
            this.eggPrefab = eggPrefab;
            this.giftPrefab = giftPrefab;
            this.foodPrefab = foodPrefab;
            this.feather = feather;
        }

        internal Vector2 PlayerPosition => player.Position;

        internal event Action<CombatActor> ActorReleased;
        internal event Action<float> BossHealthChanged;
        internal event Action<ActorKind, Vector2> EnemyDestroyed;
        internal event Action<Vector2> MissileLaunched;

        internal CombatActor Spawn(CombatActor prefab, Vector2 position, int health = 1)
        {
            if (!pools.TryGetValue(prefab, out ActorPool pool))
            {
                pool = new ActorPool(prefab, actorRoot);
                pools.Add(prefab, pool);
            }

            CombatActor actor = pool.Rent(this, position, health);
            active.Add(actor);
            return actor;
        }

        internal CombatActor SpawnBoss(RoundData round, BossData data)
        {
            bossRound = round;
            summonIndex = 0;
            var origin = new Vector2(0f, PlayfieldCamera.HalfHeight + 0.9f);
            CombatActor actor = Spawn(round.Wave.Prefab, origin, data.Health);
            actor.ConfigureBoss(round.Wave, data);
            return actor;
        }

        internal void Fire(WeaponDataSO weapon, Vector2 origin)
        {
            audio.Play(FlightSound.Laser);
            foreach (ShotPattern shot in weapon.Shots)
            {
                Vector2 position = origin + Vector2.right * shot.HorizontalOffset;
                Vector2 direction = Quaternion.Euler(0f, 0f, shot.AngleDegrees) * Vector2.up;
                Spawn(weapon.Projectile, position).ConfigureMotion(direction * weapon.Speed, weapon.Damage, weapon.Sprite);
            }
        }

        internal void FireMissile()
        {
            if (!progress.TrySpendMissile())
                return;

            audio.Play(FlightSound.Missile);
            MissileLaunched?.Invoke(PlayerPosition);
            Select(actor => actor.Kind == ActorKind.Chicken || actor.Kind == ActorKind.Egg);
            foreach (CombatActor actor in selection)
            {
                if (actor.IsBoss)
                    actor.TakeMissileDamage();
                else
                    actor.TakeDamage(int.MaxValue);
            }
            selection.Clear();
        }

        internal void DropEgg(Vector2 origin, float difficulty)
        {
            if (progress.Lives == 0)
                return;

            audio.Play(FlightSound.Egg);
            Vector2 motion = new Vector2(Random.Range(-0.45f, 0.45f), -3.4f * difficulty);
            Spawn(eggPrefab, origin).ConfigureMotion(motion);
        }

        internal void FireBossSpread(Vector2 origin, int count, float speed, float separation, bool energyFeathers = false)
        {
            audio.Play(FlightSound.Egg);
            for (int shot = 0; shot < count; ++shot)
            {
                float angle = (shot - (count - 1) * 0.5f) * separation;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.down;
                Spawn(eggPrefab, origin + direction * 0.4f)
                    .ConfigureMotion(direction * speed, overrideSprite: energyFeathers ? feather : null);
            }
        }

        internal void SummonBossMinion(Vector2 origin)
        {
            if (minions.Count >= 3)
                return;

            WaveDataSO wave = bossRound.Wave;
            int side = summonIndex++ % 3 - 1;
            Vector2 slot = new Vector2(Mathf.Clamp(origin.x + side * 2.2f, -6.5f, 6.5f), 1.25f);
            CombatActor actor = Spawn(wave.Prefab, origin);
            actor.ConfigureChicken(wave, slot, side + 1, bossRound.Difficulty, false);
            minions.Add(actor);
        }

        internal void ReportBossHealth(float health) => BossHealthChanged?.Invoke(health);

        internal void Resolve(CombatActor actor, bool destroyedByWeapon)
        {
            if (!active.Remove(actor))
                return;

            minions.Remove(actor);
            if (destroyedByWeapon)
            {
                progress.AddScore(actor.Score);
                audio.Play(FlightSound.DefeatEnemy);
                EnemyDestroyed?.Invoke(actor.Kind, actor.Position);
                if (actor.DropsGift)
                    Spawn(giftPrefab, actor.Position).ConfigureMotion(Vector2.down * 2.15f);
                else if (actor.DropsFood)
                    Spawn(foodPrefab, actor.Position).ConfigureMotion(Vector2.down * 2.4f);
            }
            ActorReleased?.Invoke(actor);
            actor.Pool.Return(actor);
        }

        internal void ClearProjectiles() => Clear(actor => actor.Kind == ActorKind.Laser || actor.Kind == ActorKind.Egg);

        internal void ClearAll() => Clear(_ => true);

        private void Clear(Predicate<CombatActor> match)
        {
            Select(match);
            foreach (CombatActor actor in selection)
                Resolve(actor, false);
            selection.Clear();
        }

        private void Select(Predicate<CombatActor> match)
        {
            foreach (CombatActor actor in active)
            {
                if (match(actor))
                    selection.Add(actor);
            }
        }
    }
}
