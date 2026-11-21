using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OrbitFlock
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        internal const string MainMenuScene = "MainMenu";
        public static GameManager Instance { get; private set; }

        [SerializeField] private LevelDataSO[] campaign;

        private FlightCheckpoint checkpoint;
        private LevelDataSO pendingLevel;
        private int pendingRound = 1;

        internal FlightProgress Progress { get; } = new FlightProgress();
        internal FlightAudio Audio { get; private set; }
        internal LevelDataSO[] Campaign => campaign;

        public int Lives => Progress.Lives;
        public int Score => Progress.Score;
        public int Bombs => Progress.Missiles;
        public int PowerLevel => Progress.Power;
        public int Food => Progress.Food;
        public int Round { get; private set; }
        public const int FoodPerMissile = FlightProgress.FoodPerMissile;
        public bool HasCheckpoint => checkpoint != null;

        public event Action<int> RoundChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Audio = gameObject.AddComponent<FlightAudio>();

            FlightCheckpoint saved = CheckpointStore.Read();
            if (saved != null && saved.Fits(FindLevel(saved.Level)))
                checkpoint = saved;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void NewGame()
        {
            Progress.Reset();
            LoadStage(campaign[0], 1);
        }

        public void ContinueGame()
        {
            if (!HasCheckpoint)
                return;

            Progress.Restore(checkpoint);
            LoadStage(FindLevel(checkpoint.Level), checkpoint.Round);
        }

        internal string CheckpointLabel => HasCheckpoint
            ? $"{FindLevel(checkpoint.Level).Theme.ToUpperInvariant()}  /  ROUND {checkpoint.Round:00}"
            : "NO CHECKPOINT";

        internal void LoadStage(LevelDataSO level, int round)
        {
            pendingLevel = level;
            pendingRound = round;
            Round = round;
            SceneManager.LoadScene(level.SceneName);
        }

        internal void LoadMenu() => SceneManager.LoadScene(MainMenuScene);

        internal int TakeStartingRound(LevelDataSO level)
        {
            int round = pendingLevel == level ? pendingRound : 1;
            pendingLevel = null;
            pendingRound = 1;
            return round;
        }

        public bool TryGetNextLevel(LevelDataSO level, out LevelDataSO nextLevel)
        {
            int index = Array.IndexOf(campaign, level);
            nextLevel = index >= 0 && index + 1 < campaign.Length ? campaign[index + 1] : null;
            return nextLevel != null;
        }

        internal void BeginRound(LevelDataSO level, int round)
        {
            Round = round;
            SaveCheckpoint(level, round, Lives);
            RoundChanged?.Invoke(round);
        }

        internal void SaveCheckpoint(LevelDataSO level, int round, int lives)
        {
            checkpoint = Progress.Capture(level.Number, round, lives);
            CheckpointStore.Write(checkpoint);
        }

        internal void ClearCheckpoint()
        {
            checkpoint = null;
            CheckpointStore.Clear();
        }

        public void LoseLife() => Progress.LoseLife();
        public void UpgradeWeapon() => Progress.UpgradeWeapon();
        public bool TrySpendBomb() => Progress.TrySpendMissile();
        public void AddScore(int amount) => Progress.AddScore(amount);
        internal void CollectFood() => Progress.CollectFood();

        private LevelDataSO FindLevel(int number) => Array.Find(campaign, level => level.Number == number);
    }
}
