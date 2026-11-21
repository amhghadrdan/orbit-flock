using System;
using UnityEngine;

namespace OrbitFlock
{
    internal enum FlightScreen { Playing, Paused, Defeat, Victory }

    public sealed class StageController : MonoBehaviour
    {
        private enum Phase { Briefing, Wave, Boss, SectorCleared, Ended }

        [field: SerializeField] public LevelDataSO Level { get; private set; }
        [SerializeField] private PlayerShip player;
        [SerializeField] private SpriteRenderer backdrop;
        [SerializeField] private CombatActor eggPrefab;
        [SerializeField] private CombatActor giftPrefab;
        [SerializeField] private CombatActor foodPrefab;
        [SerializeField] private Sprite feather;

        private GameManager game;
        private CombatWorld world;
        private WaveRun wave;
        private Phase phase;
        private int roundIndex;
        private float phaseUntil;
        private bool leaving;
        private float flightTimeScale;
        private LevelDataSO nextLevel;
        private CombatActor boss;

        internal FlightScreen Screen { get; private set; }
        public bool IsRoundActive => (phase == Phase.Wave || phase == Phase.Boss) && CanMove;
        public bool CanMove => game != null && Screen == FlightScreen.Playing && !leaving && game.Lives > 0
            && phase != Phase.Ended && phase != Phase.SectorCleared;
        public Vector2 PlayerPosition => player.Position;

        public event Action<string> AnnouncementChanged;
        internal event Action<FlightScreen> ScreenChanged;
        internal event Action<float?> BossHealthChanged;

        private void Start()
        {
            game = GameManager.Instance;
            world = new CombatWorld(transform, player, game, eggPrefab, giftPrefab, foodPrefab, feather);
            world.ActorReleased += OnActorReleased;
            world.BossHealthChanged += health => BossHealthChanged?.Invoke(health);
            FlightEffects effects = gameObject.AddComponent<FlightEffects>();
            effects.Initialize(world, player.transform, Level.Number);
            backdrop.sprite = Level.Backdrop;
            game.Audio.PlayMusic(Level.Music);
            player.Initialize(this, world, game);
            PrepareRound(game.TakeStartingRound(Level) - 1);
        }

        private void PrepareRound(int index)
        {
            roundIndex = index;
            wave = new WaveRun(Level.Rounds[index]);
            phase = Phase.Briefing;
            phaseUntil = Time.time + 1f;
            game.BeginRound(Level, index + 1);
            AnnouncementChanged?.Invoke(wave.IsAsteroidWave ? "ASTEROID BELT" : "INCOMING FLOCK");
        }

        private void Update()
        {
            if (Screen != FlightScreen.Playing || leaving)
                return;

            if (phase != Phase.Ended && phase != Phase.SectorCleared && game.Lives == 0)
            {
                EndStage(true);
                return;
            }

            float now = Time.time;
            switch (phase)
            {
                case Phase.Briefing:
                    if (now >= phaseUntil)
                    {
                        phase = Phase.Wave;
                        wave.Launch(now);
                        AnnouncementChanged?.Invoke(string.Empty);
                    }
                    break;
                case Phase.Wave:
                    wave.Advance(now, world);
                    if (wave.IsComplete)
                        CompleteWave();
                    break;
                case Phase.Boss:
                    if (boss == null)
                        EndStage(false);
                    else if (phaseUntil > 0f && now >= phaseUntil)
                    {
                        phaseUntil = 0f;
                        AnnouncementChanged?.Invoke(string.Empty);
                    }
                    break;
                case Phase.SectorCleared:
                    if (now >= phaseUntil)
                    {
                        leaving = true;
                        game.LoadStage(nextLevel, 1);
                    }
                    break;
            }
        }

        private void CompleteWave()
        {
            world.ClearProjectiles();
            if (roundIndex + 1 < Level.Rounds.Length)
            {
                PrepareRound(roundIndex + 1);
                return;
            }

            if (Level.Boss == null || string.IsNullOrEmpty(Level.Boss.Name))
            {
                EndStage(false);
                return;
            }

            phase = Phase.Boss;
            boss = world.SpawnBoss(Level.Rounds[roundIndex], Level.Boss);
            phaseUntil = Time.time + 1.45f;
            BossHealthChanged?.Invoke(1f);
            AnnouncementChanged?.Invoke("BOSS INBOUND");
        }

        private void EndStage(bool defeated)
        {
            phase = Phase.Ended;
            world.ClearAll();
            AnnouncementChanged?.Invoke(string.Empty);
            if (defeated)
            {
                game.SaveCheckpoint(Level, 1, FlightProgress.StartingLives);
                SetScreen(FlightScreen.Defeat);
            }
            else if (game.TryGetNextLevel(Level, out nextLevel))
            {
                phase = Phase.SectorCleared;
                phaseUntil = Time.time + 2f;
                game.SaveCheckpoint(nextLevel, 1, game.Lives);
                AnnouncementChanged?.Invoke("STAGE CLEARED");
            }
            else
            {
                game.ClearCheckpoint();
                SetScreen(FlightScreen.Victory);
            }
        }

        internal void TogglePause()
        {
            if (Screen == FlightScreen.Paused)
            {
                Time.timeScale = flightTimeScale;
                SetScreen(FlightScreen.Playing);
            }
            else if (CanMove)
            {
                flightTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                SetScreen(FlightScreen.Paused);
            }
        }

        internal void ReturnToMenu()
        {
            if (leaving)
                return;

            leaving = true;
            game.LoadMenu();
        }

        internal void Retry()
        {
            if (Screen != FlightScreen.Defeat || leaving)
                return;

            leaving = true;
            game.ContinueGame();
        }

        private void OnDestroy()
        {
            if (Screen == FlightScreen.Paused)
                Time.timeScale = flightTimeScale;
        }

        private void SetScreen(FlightScreen screen)
        {
            Screen = screen;
            ScreenChanged?.Invoke(screen);
        }

        private void OnActorReleased(CombatActor actor)
        {
            if (actor == boss)
            {
                boss = null;
                BossHealthChanged?.Invoke(null);
            }
            else if (phase == Phase.Wave)
                wave.OnActorReleased(actor);
        }
    }
}
