using System.Collections.Generic;
using UnityEngine;

namespace OrbitFlock
{
    public enum ActorKind { Chicken, Asteroid, Laser, Egg, Gift, Food }

    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CombatActor : MonoBehaviour
    {
        [field: SerializeField] public ActorKind Kind { get; private set; }
        [SerializeField] private SpriteRenderer artwork;
        [SerializeField] private LayerMask laserTargets;

        private Rigidbody2D body;
        private Sprite defaultSprite;
        private Color defaultColor;
        private Vector3 defaultScale;
        private CombatWorld world;
        private WaveDataSO wave;
        private BossFight boss;
        private Vector2 velocity;
        private Vector2 entryStart, controlA, controlB, slot;
        private float entryProgress, entryRate, nextAction, difficulty, phaseOffset;
        private bool diver, diving, arrived, enteredPlayfield;
        private bool giftCarrier, foodCarrier;
        private int health, maxHealth, damage;
        private float flashedUntil;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);

        public bool IsActive { get; private set; }
        public bool IsDiveAttack => diving && IsActive;
        public bool IsBoss => boss != null;
        internal float HealthFraction => Mathf.Clamp01((float)health / maxHealth);
        public Vector2 Position => body.position;
        internal ActorPool Pool { get; private set; }
        internal int Score => IsBoss ? wave.Score * 10 : wave != null ? wave.Score : 0;
        internal bool DropsGift => giftCarrier;
        internal bool DropsFood => foodCarrier;

        internal void Rent(CombatWorld owner, ActorPool pool, Vector2 position, int hitPoints)
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
                defaultSprite = artwork.sprite;
                defaultColor = artwork.color;
                defaultScale = transform.localScale;
            }

            world = owner;
            Pool = pool;
            wave = null;
            boss = null;
            maxHealth = health = hitPoints;
            damage = 0;
            velocity = Vector2.zero;
            entryProgress = nextAction = 0f;
            diver = diving = arrived = giftCarrier = foodCarrier = enteredPlayfield = false;
            flashedUntil = 0f;
            transform.localScale = defaultScale;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            body.position = position;
            body.rotation = 0f;
            artwork.sprite = defaultSprite;
            artwork.color = defaultColor;
            artwork.transform.localRotation = Quaternion.identity;
            IsActive = true;
            gameObject.SetActive(true);
        }

        internal void Sleep()
        {
            IsActive = false;
            gameObject.SetActive(false);
            wave = null;
            boss = null;
            world = null;
        }

        internal void ConfigureMotion(Vector2 movement, int hitDamage = 0, Sprite overrideSprite = null)
        {
            velocity = movement;
            damage = hitDamage;
            if (overrideSprite != null)
                artwork.sprite = overrideSprite;
            if (Kind == ActorKind.Laser)
                body.rotation = Vector2.SignedAngle(Vector2.up, movement);
        }

        internal void ConfigureAsteroid(WaveDataSO data, Vector2 movement)
        {
            wave = data;
            ConfigureMotion(movement);
        }

        internal void ConfigureChicken(WaveDataSO data, Vector2 formationSlot, int index, float tuning, bool canDropPickups = true)
        {
            wave = data;
            slot = formationSlot;
            difficulty = tuning;
            float mirror = data.MirrorAlternate && index % 2 == 1 ? -1f : 1f;
            entryStart = Position;
            controlA = new Vector2(data.EntryControlA.x * mirror, data.EntryControlA.y);
            controlB = new Vector2(data.EntryControlB.x * mirror + slot.x * 0.35f, data.EntryControlB.y);
            float lengthEstimate = (Vector2.Distance(entryStart, controlA) + Vector2.Distance(controlA, controlB)
                + Vector2.Distance(controlB, slot) + Vector2.Distance(entryStart, slot)) * 0.5f;
            entryRate = data.Speed * tuning / Mathf.Max(1f, lengthEstimate);
            phaseOffset = slot.y * 0.5f;
            giftCarrier = canDropPickups && index == data.GiftCarrier;
            foodCarrier = canDropPickups && !giftCarrier && (index + 1) % 3 == 0;
            diver = data.DiveEvery > 0 && (index + 1) % data.DiveEvery == 0;
        }

        internal void ConfigureBoss(WaveDataSO wave, BossData data)
        {
            this.wave = wave;
            boss = new BossFight(world, data);
            transform.localScale = defaultScale * 3.1f;
        }

        public void TakeDamage(int amount)
        {
            if (!IsActive || amount <= 0 || (Kind != ActorKind.Chicken && Kind != ActorKind.Asteroid && Kind != ActorKind.Egg))
                return;

            health -= amount;
            if (IsBoss)
                world.ReportBossHealth(HealthFraction);
            if (health <= 0)
                world.Resolve(this, true);
            else
            {
                artwork.color = new Color(2f, 2f, 2f, defaultColor.a);
                flashedUntil = Time.time + 0.08f;
            }
        }

        internal void TakeMissileDamage() => TakeDamage(Mathf.RoundToInt(maxHealth * 0.18f));

        internal void TouchPlayer(PlayerShip player)
        {
            if (!IsActive)
                return;

            switch (Kind)
            {
                case ActorKind.Gift:
                    player.CollectGift();
                    world.Resolve(this, false);
                    break;
                case ActorKind.Food:
                    player.CollectFood();
                    world.Resolve(this, false);
                    break;
                case ActorKind.Egg:
                case ActorKind.Asteroid:
                    player.TakeHit();
                    world.Resolve(this, false);
                    break;
                case ActorKind.Chicken:
                    player.TakeHit();
                    if (diving)
                        world.Resolve(this, false);
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (!IsActive)
                return;

            float step = Time.fixedDeltaTime;
            if (flashedUntil > 0f && Time.time >= flashedUntil)
            {
                artwork.color = defaultColor;
                flashedUntil = 0f;
            }
            if (IsBoss)
            {
                body.MovePosition(boss.Move(Position, HealthFraction, step));
                return;
            }

            switch (Kind)
            {
                case ActorKind.Chicken:
                    MoveChicken(step);
                    break;
                case ActorKind.Laser:
                    MoveLaser(step);
                    break;
                default:
                    MoveDriftingActor(step);
                    break;
            }
        }

        private void MoveChicken(float step)
        {
            if (diving)
            {
                Vector2 next = Position + velocity * step;
                if (PlayfieldCamera.IsOutside(next))
                    world.Resolve(this, false);
                else
                    body.MovePosition(next);
                return;
            }

            if (!arrived)
            {
                entryProgress = Mathf.Min(1f, entryProgress + entryRate * step);
                float t = entryProgress;
                float s = 1f - t;
                body.MovePosition(s * s * s * entryStart + 3f * s * s * t * controlA
                    + 3f * s * t * t * controlB + t * t * t * slot);
                if (t == 1f)
                {
                    arrived = true;
                    ScheduleAction();
                }
                return;
            }

            body.MovePosition(slot + new Vector2(Mathf.Sin(Time.time * difficulty + phaseOffset) * 0.38f, 0f));
            if (Time.time < nextAction)
                return;

            if (diver)
            {
                Vector2 aim = world.PlayerPosition - Position;
                aim.x = Mathf.Clamp(aim.x, -4f, 4f);
                aim.y = Mathf.Min(aim.y, -2.7f);
                velocity = aim.normalized * wave.DiveSpeed * difficulty;
                artwork.transform.localRotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.down, aim));
                diving = true;
                return;
            }

            world.DropEgg(Position + Vector2.down * 0.26f, difficulty);
            ScheduleAction();
        }

        private void ScheduleAction()
        {
            Vector2 interval = diver ? wave.DiveDelay : wave.EggInterval;
            nextAction = Time.time + Random.Range(interval.x, interval.y) / difficulty;
        }

        private void MoveLaser(float step)
        {
            float distance = velocity.magnitude * step;
            Vector2 direction = velocity.normalized;
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(laserTargets);
            Physics2D.CircleCast(Position, 0.075f, direction, filter, hits, distance);
            CombatActor target = null;
            float closest = float.PositiveInfinity;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.distance >= closest || !hit.collider.TryGetComponent(out CombatActor candidate) || !candidate.IsActive)
                    continue;

                target = candidate;
                closest = hit.distance;
            }
            if (target != null)
            {
                target.TakeDamage(damage);
                world.Resolve(this, false);
                return;
            }

            Vector2 next = Position + direction * distance;
            if (PlayfieldCamera.IsOutside(next))
                world.Resolve(this, false);
            else
                body.MovePosition(next);
        }

        private void MoveDriftingActor(float step)
        {
            Vector2 next = Position + velocity * step;
            bool outside = PlayfieldCamera.IsOutside(next);
            if ((enteredPlayfield && outside) || next.y < -PlayfieldCamera.HalfHeight - 0.7f)
            {
                world.Resolve(this, false);
                return;
            }

            enteredPlayfield |= !outside;
            body.MovePosition(next);
            if (Kind == ActorKind.Asteroid)
                artwork.transform.Rotate(0f, 0f, 55f * step);
        }
    }
}
