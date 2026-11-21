using UnityEngine;
using UnityEngine.InputSystem;

namespace OrbitFlock
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PlayerShip : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer artwork;
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private WeaponDataSO[] powerLevels;
        [SerializeField, Min(0.1f)] private float speed = 6.8f;

        private StageController stage;
        private CombatWorld world;
        private GameManager game;
        private Rigidbody2D body;
        private InputActionMap gameplay;
        private InputAction move, fire, bomb, pause;
        private float nextShot, protectedUntil;
        private Vector2 shipHalfSize;
        private bool fireBuffered;

        public Vector2 Position => body.position;

        internal void Initialize(StageController owner, CombatWorld simulation, GameManager session)
        {
            stage = owner;
            world = simulation;
            game = session;
            body = GetComponent<Rigidbody2D>();
            shipHalfSize = artwork.bounds.extents;
            gameplay = actions.FindActionMap("Gameplay", true);
            move = gameplay.FindAction("Move", true);
            fire = gameplay.FindAction("Fire", true);
            bomb = gameplay.FindAction("Bomb", true);
            pause = gameplay.FindAction("Pause", true);
            gameplay.Enable();
        }

        private void OnDisable()
        {
            gameplay?.Disable();
        }

        private void Update()
        {
            if (stage == null)
                return;

            if (pause.WasPressedThisFrame())
                stage.TogglePause();

            artwork.enabled = Time.time >= protectedUntil || Mathf.FloorToInt((protectedUntil - Time.time) * 14f) % 2 == 0;
            if (!stage.IsRoundActive)
            {
                fireBuffered = false;
                return;
            }

            fireBuffered |= fire.WasPressedThisFrame();
            if (Time.time >= nextShot && (fire.IsPressed() || fireBuffered))
            {
                WeaponDataSO weapon = powerLevels[game.PowerLevel - 1];
                world.Fire(weapon, Position + Vector2.up * 0.40f);
                nextShot = Time.time + 1f / weapon.ShotsPerSecond;
                fireBuffered = false;
            }

            if (bomb.WasPressedThisFrame())
                world.FireMissile();
        }

        private void FixedUpdate()
        {
            if (stage == null || !stage.CanMove)
                return;

            Vector2 motion = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) * (speed * Time.fixedDeltaTime);
            Vector2 next = body.position + motion;
            float horizontalLimit = PlayfieldCamera.HalfWidth - shipHalfSize.x;
            float verticalLimit = PlayfieldCamera.HalfHeight - shipHalfSize.y;
            next.x = Mathf.Clamp(next.x, -horizontalLimit, horizontalLimit);
            next.y = Mathf.Clamp(next.y, -verticalLimit, verticalLimit);
            body.MovePosition(next);
        }

        internal void TakeHit()
        {
            if (Time.time < protectedUntil || !stage.IsRoundActive)
                return;

            protectedUntil = Time.time + 1.5f;
            game.Audio.Play(FlightSound.Hit);
            game.LoseLife();
        }

        internal void CollectGift()
        {
            if (stage.CanMove)
            {
                game.Audio.Play(FlightSound.Pickup);
                game.UpgradeWeapon();
            }
        }

        internal void CollectFood()
        {
            if (stage.CanMove)
            {
                game.Audio.Play(FlightSound.Pickup);
                game.CollectFood();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (stage != null && stage.CanMove && other.TryGetComponent(out CombatActor actor))
                actor.TouchPlayer(this);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (stage != null && stage.IsRoundActive && other.TryGetComponent(out CombatActor actor)
                && actor.Kind == ActorKind.Chicken)
                actor.TouchPlayer(this);
        }
    }
}
