using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static OrbitFlock.FlightUi;

namespace OrbitFlock
{
    public sealed class MenuController : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Sprite ship;
        private GameManager game;
        private RectTransform routeShip;

        private void Start()
        {
            game = GameManager.Instance;
            game.Audio.PlayMenuMusic();
            RectTransform page = Page(canvas);
            Block(page, "Console background", 0, 0, Width, Height, Ink);
            Block(page, "Console index", 32, 33, 10, 10, Amber);
            Label(page, "FLIGHT CONSOLE", 53, 24, 240, 27, 15, Paper, FontStyle.Bold);
            Block(page, "Header line", 32, 62, 1216, 1, Line);

            Transform briefing = Block(page, "Flight briefing", 32, 89, 440, 560, Paper).transform;
            Block(briefing, "Binding stripe", 0, 0, 7, 560, Amber);
            Label(briefing, "ORBIT", 24, 11, 395, 112, 88, Ink, FontStyle.BoldAndItalic);
            Label(briefing, "FLOCK", 24, 87, 403, 112, 88, Ink, FontStyle.BoldAndItalic);
            Label(briefing, "Defend three sectors of the galaxy.", 29, 194, 377, 29, 18, PaperMuted);
            Block(briefing, "Briefing rule", 28, 237, 384, 1, PaperLine);
            Label(briefing, game.CheckpointLabel, 28, 248, 384, 32, 14, PaperMuted, FontStyle.Bold);

            Button start = Button(briefing, "New Game", 28, 287, 384, 49, Ink, Paper, NewGame);
            Button resume = Button(briefing, "Continue", 28, 348, 286, 45, PaperLine, Ink, Continue);
            resume.interactable = game.HasCheckpoint;
            resume.GetComponentInChildren<Text>().color = game.HasCheckpoint ? Ink : new Color32(108, 93, 79, 255);
            Button(briefing, "Exit", 326, 348, 86, 45, PaperLine, Ink, Exit);

            Block(briefing, "Audio rule", 28, 417, 384, 1, PaperLine);
            Label(briefing, "AUDIO", 28, 428, 384, 26, 13, PaperMuted, FontStyle.Bold);
            Slider(briefing, "Music", 462, game.Audio.MusicVolume, game.Audio.SetMusicVolume);
            Slider(briefing, "Sound effects", 500, game.Audio.EffectsVolume, game.Audio.SetEffectsVolume);

            BuildChart(page);
            Label(page, "WASD / ARROWS   MOVE", 32, 671, 310, 23, 15, Paper, FontStyle.Bold);
            Label(page, "SPACE   FIRE", 372, 671, 200, 23, 15, Paper, FontStyle.Bold);
            Label(page, "X   MISSILE", 713, 671, 200, 23, 15, Paper, FontStyle.Bold);
            Label(page, "ESC   PAUSE", 1054, 671, 194, 23, 15, Paper, FontStyle.Bold, TextAnchor.MiddleRight);
            if (EventSystem.current != null)
            {
                GameObject defaultButton = (game.HasCheckpoint ? resume : start).gameObject;
                EventSystem.current.firstSelectedGameObject = defaultButton;
                EventSystem.current.SetSelectedGameObject(defaultButton);
            }
        }

        private void BuildChart(RectTransform page)
        {
            Transform chart = Block(page, "Mission route", 500, 89, MissionChart.ChartWidth, MissionChart.ChartHeight, Panel).transform;
            var graphic = Rect(chart, "Navigation chart", 0, 0, MissionChart.ChartWidth, MissionChart.ChartHeight)
                .gameObject.AddComponent<MissionChart>();
            graphic.raycastTarget = false;
            Label(chart, "Mission route", 22, 12, 470, 49, 31, Paper, FontStyle.Bold);
            int totalRounds = game.Campaign.Sum(level => level.Rounds.Length);
            Label(chart, $"{totalRounds} ROUNDS", 525, 17, 199, 38, 15, Amber, FontStyle.Bold, TextAnchor.MiddleRight);
            Block(chart, "Chart header rule", 22, 66, 704, 1, Line);

            Color[] accents = { Mint, Amber, Rose };
            for (int index = 0; index < game.Campaign.Length && index < accents.Length; ++index)
            {
                LevelDataSO level = game.Campaign[index];
                Vector2 marker = MissionChart.Sector(index);
                float left = marker.x - 86;
                float top = marker.y + 78;
                Block(chart, "Sector caption", left, top, 172, 57, Ink);
                Block(chart, "Sector color", left, top, 3, 57, accents[index]);
                Label(chart, level.Theme, left + 12, top + 3, 156, 28, 18, Paper, FontStyle.Bold);
                Label(chart, $"LEVEL {level.Number}  /  {level.Rounds.Length} ROUNDS", left + 12, top + 30, 152, 20, 11,
                    accents[index], FontStyle.Bold);
            }

            routeShip = Icon(chart, "Patrol ship", ship, 0, 0, 33, 33, Color.white).rectTransform;
            routeShip.pivot = Vector2.one * 0.5f;
            routeShip.localRotation = Quaternion.Euler(0, 0, -45f);
        }

        private void Update()
        {
            float t = Mathf.PingPong(Time.unscaledTime * 0.13f, 1f) * 0.34f + 0.33f;
            Vector2 start = MissionChart.Sector(0);
            Vector2 end = MissionChart.Sector(1);
            Vector2 normal = Vector2.Perpendicular((end - start).normalized);
            Vector2 position = Vector2.Lerp(start, end, t) - normal * (63f * Mathf.Sin(t * Mathf.PI));
            routeShip.anchoredPosition = new Vector2(position.x, -position.y);
        }

        public void NewGame()
        {
            game.Audio.SaveSettings();
            game.NewGame();
        }

        public void Continue()
        {
            game.Audio.SaveSettings();
            game.ContinueGame();
        }

        public void Exit()
        {
            game.Audio.SaveSettings();
            Application.Quit();
        }
    }
}
