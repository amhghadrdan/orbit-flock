using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static OrbitFlock.FlightUi;

namespace OrbitFlock
{
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public sealed class FlightHud : MonoBehaviour
    {
        [SerializeField] private StageController stage;
        [SerializeField] private Sprite ship;

        private GameManager game;
        private Color sectorColor;
        private Text round, score, bombs, bossPercentage, announcement, finalScore;
        private Image[] rounds, lives, power, food;
        private RectTransform bossFill;
        private GameObject bossPanel, announcementPanel;
        private Button pauseButton;
        private GameObject pausePanel, defeatPanel, victoryPanel;
        private Button resumeButton, retryButton, victoryButton;

        private void Awake()
        {
            sectorColor = stage.Level.Number == 1 ? Mint : stage.Level.Number == 2 ? Amber : Rose;
            RectTransform page = Page(GetComponent<Canvas>());
            BuildFrame(page);
            BuildCombatPanels(page);
            pausePanel = Overlay(page, "Flight paused", "Returning to the menu keeps your last checkpoint.",
                "Resume", stage.TogglePause, out resumeButton, true, sectorColor);
            defeatPanel = Overlay(page, "Game Over", $"Retry from round 1 of {stage.Level.Theme} with three lives.",
                "Retry", stage.Retry, out retryButton, true, Rose);
            victoryPanel = Overlay(page, "Victory", "All three sectors are clear.",
                "Main Menu", stage.ReturnToMenu, out victoryButton, false, Mint);
            finalScore = Label(victoryPanel.transform, "", 380, 371, 520, 31, 23, PaperMuted,
                FontStyle.Bold, TextAnchor.MiddleCenter);

            if (EventSystem.current == null)
                new GameObject("Flight EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void OnEnable()
        {
            game = GameManager.Instance;
            game.RoundChanged += SetRound;
            game.Progress.LivesChanged += SetLives;
            game.Progress.ScoreChanged += SetScore;
            game.Progress.MissilesChanged += SetBombs;
            game.Progress.PowerChanged += SetPower;
            game.Progress.FoodChanged += SetFood;
            stage.AnnouncementChanged += SetAnnouncement;
            stage.BossHealthChanged += SetBossHealth;
            stage.ScreenChanged += SetScreen;
            SetRound(game.Round);
            SetLives(game.Lives);
            SetScore(game.Score);
            SetBombs(game.Bombs);
            SetPower(game.PowerLevel);
            SetFood(game.Food);
        }

        private void OnDisable()
        {
            if (game != null)
            {
                game.RoundChanged -= SetRound;
                game.Progress.LivesChanged -= SetLives;
                game.Progress.ScoreChanged -= SetScore;
                game.Progress.MissilesChanged -= SetBombs;
                game.Progress.PowerChanged -= SetPower;
                game.Progress.FoodChanged -= SetFood;
            }
            if (stage != null)
            {
                stage.AnnouncementChanged -= SetAnnouncement;
                stage.BossHealthChanged -= SetBossHealth;
                stage.ScreenChanged -= SetScreen;
            }
        }

        private void BuildFrame(RectTransform page)
        {
            Block(page, "Top instruments", 0, 0, Width, FlightTop, Ink);
            Block(page, "Bottom instruments", 0, Height - FlightBottom, Width, FlightBottom, Ink);
            Block(page, "Left rail", 0, FlightTop, FlightLeft, FlightHeight, Panel);
            Block(page, "Right rail", Width - FlightLeft, FlightTop, FlightLeft, FlightHeight, Panel);
            Block(page, "Top edge", FlightLeft, FlightTop, FlightWidth, 2, sectorColor);
            Block(page, "Bottom edge", FlightLeft, Height - FlightBottom - 1, FlightWidth, 1, Line);
            for (int mark = 1; mark < 12; ++mark)
            {
                float y = FlightTop + mark * FlightHeight / 12f;
                float length = mark % 3 == 0 ? 22f : 10f;
                Block(page, "Left gauge mark", FlightLeft - length - 10, y, length, 1, Line);
                Block(page, "Right gauge mark", Width - FlightLeft + 10, y, length, 1, Line);
            }

            Block(page, "Level tile", 29, 15, 42, 36, sectorColor);
            Label(page, stage.Level.Number.ToString("00"), 29, 15, 42, 36, 20, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label(page, "SECTOR", 84, 9, 250, 20, 11, Muted, FontStyle.Bold);
            Label(page, stage.Level.Theme, 84, 27, 290, 28, 23, Paper, FontStyle.Bold);
            Block(page, "Round separator", 355, 13, 1, 40, Line);
            round = Label(page, "", 376, 8, 255, 30, 17, Paper, FontStyle.Bold);
            rounds = Segments(page, "Round marker", stage.Level.Rounds.Length, 376, 43, 226, 4, 4);
            Label(page, "SCORE", 746, 9, 322, 20, 11, Muted, FontStyle.Bold, TextAnchor.MiddleRight);
            score = Label(page, "", 744, 25, 324, 30, 25, Paper, FontStyle.Bold, TextAnchor.MiddleRight);
            pauseButton = Button(page, "ESC  Pause", 1103, 15, 148, 37, Panel, Paper, stage.TogglePause, 15);

            int bottom = Height - FlightBottom;
            Label(page, "LIVES", 29, bottom + 12, 80, 44, 14, Muted, FontStyle.Bold);
            lives = new Image[3];
            for (int index = 0; index < lives.Length; ++index)
                lives[index] = Icon(page, "Life " + (index + 1), ship, 104 + index * 34, bottom + 17, 30, 30, Paper);

            Block(page, "Missile separator", 236, bottom + 13, 1, 42, Line);
            Label(page, "MISSILES", 258, bottom + 10, 103, 22, 14, Muted, FontStyle.Bold);
            Label(page, "[ X ]", 258, bottom + 34, 97, 20, 12, sectorColor, FontStyle.Bold);
            bombs = Label(page, "", 368, bottom + 12, 78, 43, 27, Paper, FontStyle.Bold);
            Block(page, "Power separator", 448, bottom + 13, 1, 42, Line);
            Label(page, "LASER POWER", 469, bottom + 9, 200, 23, 14, Muted, FontStyle.Bold);
            power = Segments(page, "Power marker", 3, 469, bottom + 41, 161, 8, 5);

            Block(page, "Food separator", 656, bottom + 13, 1, 42, Line);
            Label(page, "FOOD  /  NEXT MISSILE", 678, bottom + 9, 361, 27, 14, Muted, FontStyle.Bold);
            food = Segments(page, "Food marker", GameManager.FoodPerMissile, 678, bottom + 42, 474, 8, 6);
            Label(page, "+1 MISSILE", 1170, bottom + 25, 82, 29, 11, Amber, FontStyle.Bold, TextAnchor.MiddleRight);
        }

        private void BuildCombatPanels(RectTransform page)
        {
            bossPanel = Block(page, "Boss health", 394, FlightTop + 11, 492, 48, Ink).gameObject;
            string bossName = stage.Level.Boss?.Name ?? "Boss";
            Label(bossPanel.transform, bossName, 14, 3, 384, 23, 15, Paper, FontStyle.Bold);
            bossPercentage = Label(bossPanel.transform, "", 401, 3, 78, 23, 14, Rose, FontStyle.Bold,
                TextAnchor.MiddleRight);
            Block(bossPanel.transform, "Boss health track", 14, 31, 464, 7, Line);
            bossFill = Block(bossPanel.transform, "Boss health fill", 14, 31, 464, 7, Rose).rectTransform;
            bossPanel.SetActive(false);

            announcementPanel = Block(page, "Flight dispatch", 431, 320, 418, 70, Ink).gameObject;
            Block(announcementPanel.transform, "Dispatch rule", 0, 0, 418, 3, sectorColor);
            announcement = Label(announcementPanel.transform, "", 0, 4, 418, 65, 22, Paper, FontStyle.Bold,
                TextAnchor.MiddleCenter);
            announcementPanel.SetActive(false);
        }

        private GameObject Overlay(Transform page, string title, string detail, string action, System.Action callback,
            out Button firstButton, bool menuButton, Color accent)
        {
            Image shade = Block(page, title + " screen", 0, 0, Width, Height, new Color32(6, 9, 14, 218));
            shade.raycastTarget = true;
            Transform panel = Block(shade.transform, "Dispatch card", 380, 206, 520, 305, Paper).transform;
            Block(panel, "Dispatch binding", 0, 0, 520, 7, accent);
            Label(panel, title, 24, 49, 472, 79, 62, Ink, FontStyle.BoldAndItalic, TextAnchor.MiddleCenter);
            Label(panel, detail, 23, 132, 474, 28, 17, PaperMuted, FontStyle.Normal, TextAnchor.MiddleCenter);
            if (menuButton)
            {
                firstButton = Button(panel, action, 32, 223, 220, 51, Ink, Paper, callback);
                Button(panel, "Main Menu", 266, 223, 222, 51, PaperLine, Ink, stage.ReturnToMenu);
            }
            else
                firstButton = Button(panel, action, 127, 223, 266, 51, Ink, Paper, callback);
            shade.gameObject.SetActive(false);
            return shade.gameObject;
        }

        private static Image[] Segments(Transform parent, string name, int count, float x, float y,
            float width, float height, float gap)
        {
            var result = new Image[count];
            float segmentWidth = (width - (count - 1) * gap) / count;
            for (int index = 0; index < count; ++index)
                result[index] = Block(parent, name + " " + (index + 1), x + index * (segmentWidth + gap),
                    y, segmentWidth, height, Line);
            return result;
        }

        private static void SetSegments(Image[] segments, int count, Color lit)
        {
            for (int index = 0; index < segments.Length; ++index)
                segments[index].color = index < count ? lit : Line;
        }

        private void SetRound(int number)
        {
            round.text = $"ROUND {number:00} / {stage.Level.Rounds.Length:00}";
            SetSegments(rounds, number, sectorColor);
        }

        private void SetLives(int number) => SetSegments(lives, number, Paper);
        private void SetScore(int number) => score.text = number.ToString("000000");
        private void SetBombs(int number) => bombs.text = number.ToString("00");
        private void SetPower(int number) => SetSegments(power, number, sectorColor);
        private void SetFood(int number) => SetSegments(food, number, Amber);

        private void SetAnnouncement(string message)
        {
            announcement.text = message;
            announcementPanel.SetActive(message.Length != 0);
        }

        private void SetBossHealth(float? fraction)
        {
            bossPanel.SetActive(fraction.HasValue);
            if (fraction.HasValue)
            {
                SetFill(bossFill, fraction.Value, 464f);
                bossPercentage.text = $"{Mathf.CeilToInt(fraction.Value * 100f)}%";
            }
        }

        private void SetScreen(FlightScreen screen)
        {
            pausePanel.SetActive(screen == FlightScreen.Paused);
            defeatPanel.SetActive(screen == FlightScreen.Defeat);
            victoryPanel.SetActive(screen == FlightScreen.Victory);
            pauseButton.interactable = screen == FlightScreen.Playing;
            if (screen == FlightScreen.Victory)
                finalScore.text = $"FINAL SCORE  {game.Score:000000}";

            GameObject selected = screen switch
            {
                FlightScreen.Paused => resumeButton.gameObject,
                FlightScreen.Defeat => retryButton.gameObject,
                FlightScreen.Victory => victoryButton.gameObject,
                _ => null
            };
            EventSystem.current?.SetSelectedGameObject(selected);
        }
    }
}
