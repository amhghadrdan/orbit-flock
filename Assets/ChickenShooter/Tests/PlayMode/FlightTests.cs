using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OrbitFlock.Tests
{
    public sealed class FlightTests
    {
        private Keyboard keyboard;
        private GameManager game;
        private string testSaveDirectory;
        private InputSettings.BackgroundBehavior backgroundBehavior;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode editorInputBehavior;
#endif

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            testSaveDirectory = Path.Combine(Path.GetTempPath(), $"orbit-flock-test-{Guid.NewGuid():N}");
            CheckpointStore.DataDirectory = testSaveDirectory;
            Time.timeScale = 2f;
            UnityEngine.Random.InitState(61);
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard = InputSystem.AddDevice<Keyboard>();
            SceneManager.LoadScene("MainMenu");
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "MainMenu"
                && GameObject.Find("New Game") != null, "Main menu did not load.");
            game = GameManager.Instance;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = backgroundBehavior;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInputBehavior;
#endif
            Time.timeScale = 1f;
            if (game != null)
                Object.Destroy(game.gameObject);
            yield return null;
            CheckpointStore.DataDirectory = null;
            if (Directory.Exists(testSaveDirectory))
                Directory.Delete(testSaveDirectory, true);
        }

        [UnityTest]
        public IEnumerator All22RoundsAdvanceAndRetainPlayerStateAcrossScenes()
        {
            Time.timeScale = 9f;
            Assert.That(Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
                .Select(index => System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(index)))
                .Take(4),
                Is.EqualTo(new[] { "MainMenu", "Level1", "Level2", "Level3" }));

            yield return StartFlight();
            var visited = new HashSet<(int Level, int Round)>();
            var asteroidRounds = new HashSet<(int Level, int Round)>();
            var bossRounds = new HashSet<(int Level, int Round)>();
            var transitioned = new List<int>();
            game.UpgradeWeapon();
            game.UpgradeWeapon();
            Assert.That(game.TrySpendBomb(), Is.True);

            StageController stage = null;
            Vector2 safeCorner = new Vector2(-7.45f, -4f);
            var deadline = Time.realtimeSinceStartup + 55f;
            int levelNumber = 0, priorScore = 0, priorBombs = game.Bombs, priorFood = game.Food;
            while (SceneManager.GetActiveScene().name != "MainMenu")
            {
                GameObject victory = GameObject.Find("Victory screen");
                if (victory != null)
                {
                    Assert.That(game.HasCheckpoint, Is.False);
                    Assert.That(File.Exists(CheckpointStore.FilePath), Is.False);
                    victory.GetComponentInChildren<Button>().onClick.Invoke();
                    yield return WaitFor(() => SceneManager.GetActiveScene().name == "MainMenu", "Victory did not return to the menu.");
                    break;
                }

                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                    $"Campaign stalled at level {levelNumber}, round {game.Round}.");
                if (stage == null)
                {
                    stage = Object.FindObjectOfType<StageController>();
                    if (stage == null || (!stage.IsRoundActive && game.Round != 1))
                    {
                        stage = null;
                        yield return null;
                        continue;
                    }

                    levelNumber = stage.Level.Number;
                    Place(Object.FindObjectOfType<PlayerShip>(), safeCorner);
                    transitioned.Add(levelNumber);
                    Assert.That(GameManager.Instance, Is.SameAs(game));
                    Assert.That(game.Round, Is.EqualTo(1));
                    Assert.That(game.Lives, Is.EqualTo(3));
                    Assert.That(game.Bombs, Is.EqualTo(priorBombs));
                    Assert.That(game.Food, Is.EqualTo(priorFood));
                    Assert.That(game.PowerLevel, Is.EqualTo(3));
                    Assert.That(game.Score, Is.EqualTo(priorScore));
                }

                visited.Add((levelNumber, game.Round));
                foreach (CombatActor actor in ActiveActors())
                {
                    if (actor.Kind == ActorKind.Asteroid)
                        asteroidRounds.Add((levelNumber, game.Round));
                    if (actor.IsBoss)
                        bossRounds.Add((levelNumber, game.Round));
                    if (actor.Kind == ActorKind.Chicken || (actor.Kind == ActorKind.Asteroid && levelNumber != 1))
                        actor.TakeDamage(100);
                }

                priorScore = game.Score;
                priorBombs = game.Bombs;
                priorFood = game.Food;
                yield return null;
            }

            Assert.That(transitioned, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(visited.Count(round => round.Level == 1), Is.EqualTo(5));
            Assert.That(visited.Count(round => round.Level == 2), Is.EqualTo(7));
            Assert.That(visited.Count(round => round.Level == 3), Is.EqualTo(10));
            Assert.That(asteroidRounds, Is.EquivalentTo(new[] { (1, 3), (2, 3), (2, 6), (3, 4), (3, 8) }));
            Assert.That(bossRounds, Is.EquivalentTo(new[] { (1, 5), (2, 7), (3, 10) }));
            Assert.That(game.Score, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator InputMovesFiresAndCollectsGiftThenCollisionLowersPowerOnce()
        {
            yield return StartFlight();
            var ship = Object.FindObjectOfType<PlayerShip>();
            Keys(Key.W, Key.D);
            yield return WaitFor(() => keyboard.wKey.isPressed && keyboard.dKey.isPressed, "The virtual keyboard event was not delivered.");
            yield return new WaitForSeconds(0.33f);
            Assert.That(ship.Position.x, Is.GreaterThan(0.8f));
            Assert.That(ship.Position.y, Is.GreaterThan(-2.5f));
            Assert.That(ship.Position.x, Is.EqualTo(ship.Position.y + 3.25f).Within(0.22f));

            Keys(Key.RightArrow, Key.DownArrow);
            yield return new WaitForSeconds(2f);
            Keys();
            Assert.That(ship.Position.x, Is.InRange(7.45f, 7.55f));
            Assert.That(ship.Position.y, Is.InRange(-4.05f, -3.95f));

            yield return WaitFor(() => Actors(ActorKind.Chicken).Length >= 3, "First wave did not spawn.");
            yield return TapAndCheckPattern(1, false);
            Keys(Key.Space);
            yield return new WaitForSeconds(0.36f);
            Assert.That(Actors(ActorKind.Laser).Length, Is.GreaterThanOrEqualTo(2));
            Keys();
            yield return WaitFor(() => Actors(ActorKind.Laser).Length == 0, "Lasers did not leave the playfield.");

            CombatActor carrier = Actors(ActorKind.Chicken)[1];
            carrier.TakeDamage(100);
            yield return WaitFor(() => Actors(ActorKind.Gift).Length == 1, "Gift carrier did not drop a gift.");
            Place(Actors(ActorKind.Gift)[0], ship.Position);
            yield return WaitFor(() => game.PowerLevel == 2, "Contact did not collect the gift.");
            Place(ship, new Vector2(7f, -3f));
            yield return TapAndCheckPattern(2, false);
            game.UpgradeWeapon();
            game.UpgradeWeapon();
            Assert.That(game.PowerLevel, Is.EqualTo(3));
            yield return TapAndCheckPattern(3, true);

            CombatActor chicken = Actors(ActorKind.Chicken)[0];
            Assert.That(game.Lives, Is.EqualTo(3));
            Place(ship, chicken.Position);
            yield return WaitFor(() => game.Lives == 2, "Chicken contact did not damage the ship.");
            Assert.That(game.PowerLevel, Is.EqualTo(2));
            Place(ship, Actors(ActorKind.Chicken).First(actor => actor != chicken).Position);
            yield return new WaitForSeconds(0.22f);
            Assert.That(game.Lives, Is.EqualTo(2), "Invincibility did not prevent a second hit.");
        }

        [UnityTest]
        public IEnumerator EggsBreakInFlightDivesLeaveAndBombsRecycleLiveTargets()
        {
            Time.timeScale = 3f;
            yield return StartFlight();
            var ship = Object.FindObjectOfType<PlayerShip>();
            Place(ship, new Vector2(-7.25f, -3.5f));
            yield return WaitFor(() => Actors(ActorKind.Chicken).Length == 6, "The opening flock did not fully enter.");
            CombatActor diver = null;
            var deadline = Time.realtimeSinceStartup + 8f;
            while (diver == null)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "A diver never left formation.");
                diver = Actors(ActorKind.Chicken).FirstOrDefault(actor => actor.IsDiveAttack);
                yield return null;
            }
            yield return WaitFor(() => !diver.IsActive, "The diver did not resolve after exiting.");
            yield return WaitFor(() => Actors(ActorKind.Egg).Length > 0, "The formation did not drop eggs.");
            CombatActor egg = Actors(ActorKind.Egg)[0];
            int livesBeforeShot = game.Lives;
            Place(egg, ship.Position + Vector2.up * 1.45f);
            Keys(Key.Space);
            yield return WaitFor(() => !egg.IsActive, "Laser did not destroy the egg.");
            Keys();
            Assert.That(game.Lives, Is.EqualTo(livesBeforeShot));

            yield return WaitFor(() => Actors(ActorKind.Egg).Length > 0, "No egg was available for the bomb.");
            CombatActor[] oldFlock = Actors(ActorKind.Chicken);
            Keys(Key.X);
            yield return WaitFor(() => game.Bombs == 1, "First bomb did not fire.");
            Assert.That(Actors(ActorKind.Egg), Is.Empty);
            Assert.That(Actors(ActorKind.Chicken), Is.Empty);
            yield return WaitFor(() => game.Round == 2 && Actors(ActorKind.Chicken).Length > 0, "Second wave did not start.");
            Assert.That(Actors(ActorKind.Chicken).Any(oldFlock.Contains), Is.True, "The scene did not reuse its pooled actors.");
            Assert.That(game.Bombs, Is.EqualTo(1), "Holding X used a second bomb.");
            Keys();
            yield return null;
            Keys(Key.X);
            yield return WaitFor(() => game.Bombs == 0, "Second press did not spend the last bomb.");
            Keys();
            yield return WaitFor(() => Actors(ActorKind.Chicken).Length > 0, "Bomb incorrectly canceled scheduled spawns.");
            Keys(Key.X);
            yield return new WaitForSeconds(0.1f);
            Assert.That(game.Bombs, Is.Zero);
            Assert.That(Actors(ActorKind.Chicken), Is.Not.Empty);
        }

        [UnityTest]
        public IEnumerator ConfiguredWaveCountIsUsedAndAsteroidsIgnoreBombs()
        {
            Time.timeScale = 4f;
            yield return StartFlight();
            var stage = Object.FindObjectOfType<StageController>();
            var ship = Object.FindObjectOfType<PlayerShip>();
            Place(ship, new Vector2(-7.45f, -4f));

            RoundData openingRound = stage.Level.Rounds[0];
            int originalCount = openingRound.CountOverride;
            try
            {
                openingRound.CountOverride = 1;
                yield return WaitFor(() => Actors(ActorKind.Chicken).Length == 1, "Configured chicken did not spawn.");
                Actors(ActorKind.Chicken)[0].TakeDamage(100);
                yield return WaitFor(() => game.Round == 2, "Round required more enemies than its configured count.");
            }
            finally
            {
                openingRound.CountOverride = originalCount;
            }

            while (game.Round == 2)
            {
                foreach (CombatActor chicken in Actors(ActorKind.Chicken))
                    chicken.TakeDamage(100);
                yield return null;
            }

            yield return WaitFor(() => Actors(ActorKind.Asteroid).Length >= 2, "Asteroid wave did not start.");
            CombatActor first = Actors(ActorKind.Asteroid)[0];
            CombatActor second = Actors(ActorKind.Asteroid)[1];
            Keys(Key.X);
            yield return WaitFor(() => game.Bombs == 1, "The bomb did not consume one charge.");
            Keys();
            Assert.That(first.IsActive && second.IsActive, Is.True, "Bomb removed an asteroid.");

            int score = game.Score;
            first.TakeDamage(1);
            Assert.That(first.IsActive, Is.True, "Asteroid did not have its configured health.");
            first.TakeDamage(1);
            Assert.That(first.IsActive, Is.False);
            Assert.That(game.Score, Is.GreaterThan(score));
            Place(second, ship.Position);
            yield return WaitFor(() => game.Lives == 2, "Asteroid collision did not cost a life.");
        }

        [UnityTest]
        public IEnumerator LethalHitsOfferRetryAndNewGameRestoresDefaults()
        {
            yield return StartFlight();
            var ship = Object.FindObjectOfType<PlayerShip>();
            yield return WaitFor(() => Actors(ActorKind.Chicken).Length >= 2, "No target spawned.");
            game.AddScore(350);
            game.UpgradeWeapon();
            Assert.That(game.TrySpendBomb(), Is.True);
            for (int remaining = 2; remaining >= 0; remaining--)
            {
                Place(ship, Actors(ActorKind.Chicken).First(actor => !actor.IsDiveAttack).Position);
                yield return WaitFor(() => game.Lives == remaining, "The ship did not lose the expected life.");
                if (remaining > 0)
                {
                    Assert.That(game.PowerLevel, Is.EqualTo(1));
                    Place(ship, new Vector2(-7.45f, -4f));
                    yield return new WaitForSeconds(1.54f);
                }
            }

            yield return WaitFor(() => GameObject.Find("Game Over screen") != null, "Game over screen did not appear.");
            FlightCheckpoint retry = CheckpointStore.Read();
            Assert.That(retry.Level, Is.EqualTo(1));
            Assert.That(retry.Round, Is.EqualTo(1));
            Assert.That(retry.Lives, Is.EqualTo(3));
            GameObject.Find("Retry").GetComponent<Button>().onClick.Invoke();
            yield return WaitFor(() => game.Lives == 3 && Object.FindObjectOfType<StageController>()?.IsRoundActive == true,
                "Retry did not restart the sector.");
            Assert.That(game.Round, Is.EqualTo(1));
            Assert.That(game.Score, Is.EqualTo(350));

            SceneManager.LoadScene("MainMenu");
            yield return WaitFor(() => GameObject.Find("New Game") != null, "Main menu did not open.");
            yield return StartFlight();
            Assert.That(game.Lives, Is.EqualTo(3));
            Assert.That(game.Bombs, Is.EqualTo(2));
            Assert.That(game.PowerLevel, Is.EqualTo(1));
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.Round, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ContinueReloadsTheSavedRoundAndPauseDoesNotOverwriteIt()
        {
            Assert.That(GameObject.Find("Continue").GetComponent<Button>().interactable, Is.False);
            yield return StartFlight();
            Place(Object.FindObjectOfType<PlayerShip>(), new Vector2(-7.45f, -4f));
            game.UpgradeWeapon();
            for (int count = 0; count < GameManager.FoodPerMissile + 2; ++count)
                game.CollectFood();

            float deadline = Time.realtimeSinceStartup + 8f;
            while (game.Round == 1)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "The opening round stalled.");
                foreach (CombatActor actor in Actors(ActorKind.Chicken))
                    actor.TakeDamage(100);
                yield return null;
            }

            FlightCheckpoint saved = CheckpointStore.Read();
            Assert.That(saved.Round, Is.EqualTo(2));
            Assert.That(saved.Bombs, Is.EqualTo(3));
            Assert.That(saved.Power, Is.EqualTo(2));
            Assert.That(saved.Food, Is.EqualTo(2));
            game.AddScore(900);

            Keys(Key.Escape);
            yield return WaitFor(() => GameObject.Find("Flight paused screen") != null, "Escape did not pause the game.");
            Keys();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(CheckpointStore.Read().Score, Is.EqualTo(saved.Score));
            GameObject.Find("Main Menu").GetComponent<Button>().onClick.Invoke();
            yield return WaitFor(() => GameObject.Find("Continue") != null, "Pause did not return to the menu.");
            Assert.That(Time.timeScale, Is.EqualTo(2f));

            Object.Destroy(game.gameObject);
            yield return null;
            SceneManager.LoadScene("MainMenu");
            yield return WaitFor(() => GameObject.Find("Continue") != null && GameManager.Instance != null,
                "The main menu did not load the checkpoint.");
            game = GameManager.Instance;
            Button continueButton = GameObject.Find("Continue").GetComponent<Button>();
            Assert.That(continueButton.interactable, Is.True);
            continueButton.onClick.Invoke();
            yield return WaitFor(() => Object.FindObjectOfType<StageController>()?.IsRoundActive == true,
                "Continue did not start the saved round.");
            Assert.That(game.Round, Is.EqualTo(2));
            Assert.That(game.Score, Is.EqualTo(saved.Score));
            Assert.That(game.Bombs, Is.EqualTo(3));
            Assert.That(game.PowerLevel, Is.EqualTo(2));
            Assert.That(game.Food, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator BossHealthMeterTracksDamageAndMissileLeavesTheBossAlive()
        {
            game.LoadStage(game.Campaign[0], 5);
            yield return WaitFor(() => Object.FindObjectOfType<StageController>()?.IsRoundActive == true,
                "The final round did not start.");
            Place(Object.FindObjectOfType<PlayerShip>(), new Vector2(-7.45f, -4f));
            float deadline = Time.realtimeSinceStartup + 8f;
            while (!Actors(ActorKind.Chicken).Any(actor => actor.IsBoss))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "The boss did not arrive.");
                foreach (CombatActor actor in Actors(ActorKind.Chicken))
                    actor.TakeDamage(100);
                yield return null;
            }

            CombatActor boss = Actors(ActorKind.Chicken).Single(actor => actor.IsBoss);
            GameObject health = GameObject.Find("Boss health");
            Assert.That(health.GetComponentsInChildren<Text>().Any(label => label.text == "100%"), Is.True);
            Keys(Key.X);
            yield return WaitFor(() => game.Bombs == 1, "The missile did not launch.");
            Keys();
            Assert.That(boss.IsActive, Is.True);
            Assert.That(boss.HealthFraction, Is.InRange(0.80f, 0.85f));
            Assert.That(health.GetComponentsInChildren<Text>().Any(label => label.text == $"{Mathf.CeilToInt(boss.HealthFraction * 100f)}%"),
                Is.True);
        }

        private IEnumerator StartFlight()
        {
            GameObject.Find("New Game").GetComponent<Button>().onClick.Invoke();
            yield return WaitFor(() =>
            {
                var stage = Object.FindObjectOfType<StageController>();
                return stage != null && stage.IsRoundActive;
            }, "New Game did not start the first round.");
        }

        private IEnumerator TapAndCheckPattern(int count, bool spread)
        {
            yield return WaitFor(() => Actors(ActorKind.Laser).Length == 0, "The previous salvo never expired.");
            Keys(Key.Space);
            yield return WaitFor(() => Actors(ActorKind.Laser).Length == count, "Unmatched salvo size.");
            Keys();
            if (spread)
            {
                Assert.That(Actors(ActorKind.Laser).Select(laser => Mathf.DeltaAngle(0, laser.GetComponent<Rigidbody2D>().rotation)).OrderBy(angle => angle),
                    Is.EqualTo(new[] { -14f, 0f, 14f }).Within(0.05f));
            }
            else
            {
                Assert.That(Actors(ActorKind.Laser).All(laser => Mathf.Abs(laser.GetComponent<Rigidbody2D>().rotation) < 0.05f));
                if (count == 2)
                    Assert.That(Mathf.Abs(Actors(ActorKind.Laser)[0].Position.x - Actors(ActorKind.Laser)[1].Position.x),
                        Is.EqualTo(0.42f).Within(0.03f));
            }
            yield return null;
        }

        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

        private static CombatActor[] ActiveActors() => Object.FindObjectsOfType<CombatActor>()
            .Where(actor => actor.IsActive).OrderBy(actor => actor.transform.GetSiblingIndex()).ToArray();

        private static CombatActor[] Actors(ActorKind kind) => ActiveActors().Where(actor => actor.Kind == kind).ToArray();

        private static void Place(Component component, Vector2 position)
        {
            component.transform.position = position;
            component.GetComponent<Rigidbody2D>().position = position;
            Physics2D.SyncTransforms();
        }

        private static IEnumerator WaitFor(Func<bool> condition, string message, float seconds = 8f)
        {
            float time = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(time), message);
                yield return null;
            }
        }
    }
}
