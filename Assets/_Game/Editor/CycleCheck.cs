using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NineLives.Editor
{
    public static class CycleCheck
    {
        private static bool running;

        [MenuItem("NineLives/Campaign/Check Checkpoints and Walls (Play Mode)")]
        private static async void FromMenu()
        {
            try { Debug.Log(await RunAsync()); }
            catch (Exception error) { Debug.LogException(error); }
        }

        public static async Task<string> RunAsync()
        {
            if (running || !EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "MainMenu")
                throw new InvalidOperationException("Start Bootstrap and run this check from MainMenu in Play Mode.");
            running = true;
            bool hadSave = PlayerPrefs.HasKey(CycleController.CheckpointKey);
            int oldSave = PlayerPrefs.GetInt(CycleController.CheckpointKey);
            try
            {
                Check(UnityEngine.Object.FindAnyObjectByType<MainMenu>().transform.Find("Main buttons/Continue Button") != null,
                    "Continue button missing from scene");
                var cycle = CycleController.Ensure();
                PlayerPrefs.SetInt(CycleController.CheckpointKey, -3);
                Check(cycle.SavedPosition == 0, "negative saved index not clamped");
                PlayerPrefs.SetInt(CycleController.CheckpointKey, 900);
                Check(cycle.SavedPosition == RunRoute.RoomCount - 1, "oversized saved index not clamped");
                bool rejected = false;
                try { cycle.SaveCheckpoint(RunRoute.RoomCount); }
                catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "invalid checkpoint accepted");
                cycle.SaveCheckpoint(2);
                UnityEngine.Object.FindAnyObjectByType<MainMenu>().Continue();
                await WaitForRun();
                Check(cycle.Run.Position == 2, "Continue did not load saved section");
                int enemyCount = UnityEngine.Object.FindAnyObjectByType<RoomLayout>().GetComponentsInChildren<RatController>().Length;
                Check(enemyCount > 0, "test room has no enemies");
                for (int attempt = 0; attempt < 11; attempt++)
                {
                    var room = UnityEngine.Object.FindAnyObjectByType<RoomLayout>();
                    var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                    var health = player.GetComponent<Health>();
                    var pause = UnityEngine.Object.FindAnyObjectByType<PauseMenu>();
                    Check(health.CurrentHealth == health.MaximumHealth, "retry HP not full");
                    Check(Mathf.Abs(player.transform.position.x - room.SafeSpawn.position.x) < .05f,
                        "retry did not use SafeSpawn");
                    var enemies = room.GetComponentsInChildren<RatController>();
                    Check(enemies.Length == enemyCount, "retry changed enemy count");
                    foreach (var enemy in enemies)
                        Check(enemy.GetComponent<Health>().CurrentHealth == enemy.GetComponent<Health>().MaximumHealth,
                            "enemy damage survived retry");
                    Check(UnityEngine.Object.FindObjectsByType<FoodDrop>().Length == 0, "old food/shop rewards spawned");
                    var damagedEnemy = enemies[0].GetComponent<Health>();
                    damagedEnemy.TakeDamage(1, Vector2.zero);
                    pause.SetPaused(false);
                    health.ResetToFull();
                    Check(health.TakeDamage(999, Vector2.zero), "test death rejected");
                    Check(pause.ShowingResult && !cycle.Run.IsActive && cycle.SavedPosition == 2 && cycle.Run.Position == 2,
                        "death lost section or failed to show retry");
                    Check(!cycle.CompleteRun(RunEndReason.Death), "duplicate death accepted");
                    pause.Resume();
                    await Wait(() => pause == null);
                    await WaitForRun();
                }
                UnityEngine.Object.FindAnyObjectByType<PauseMenu>().MainMenu();
                await Wait(() => SceneManager.GetActiveScene().name == "MainMenu");
                UnityEngine.Object.Destroy(cycle.gameObject);
                await Task.Delay(40);
                cycle = CycleController.Ensure();
                Check(cycle.HasCheckpoint && cycle.SavedPosition == 2, "fresh controller lost persisted progress");
                UnityEngine.Object.FindAnyObjectByType<MainMenu>().Continue();
                await WaitForRun();
                Check(cycle.Run.Position == 2, "fresh session Continue started the wrong room");
                await CheckWalls();
                UnityEngine.Object.FindAnyObjectByType<PauseMenu>().MainMenu();
                await Wait(() => SceneManager.GetActiveScene().name == "MainMenu");
                UnityEngine.Object.FindAnyObjectByType<MainMenu>().Play();
                await WaitForRun();
                Check(cycle.Run.Position == 0 && cycle.SavedPosition == 0, "New game did not reset progress");
                Check(cycle.CompleteRun(RunEndReason.Victory) && !cycle.HasCheckpoint, "victory did not clear checkpoint");
                UnityEngine.Object.FindAnyObjectByType<PauseMenu>().MainMenu();
                await Wait(() => SceneManager.GetActiveScene().name == "MainMenu");
                return "Campaign checkpoint PASS: 11 deaths, SafeSpawn, full player/enemy HP, persistent Continue, New game, victory; claw blocked by wall and damages exposed target.";
            }
            finally
            {
                if (hadSave) PlayerPrefs.SetInt(CycleController.CheckpointKey, oldSave);
                else PlayerPrefs.DeleteKey(CycleController.CheckpointKey);
                PlayerPrefs.Save();
                running = false;
            }
        }

        private static async Task CheckWalls()
        {
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var pause = UnityEngine.Object.FindAnyObjectByType<PauseMenu>();
            pause.SetPaused(false);
            player.SetSpawn(new Vector2(0, 30));
            var body = player.GetComponent<Rigidbody2D>();
            var previousType = body.bodyType;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            Vector2 origin = player.GetComponent<Collider2D>().bounds.center;
            Vector2 direction = Vector2.right * player.FacingDirection;
            var target = new GameObject("Wall regression target", typeof(BoxCollider2D), typeof(Health));
            var wall = new GameObject("Wall regression obstacle", typeof(BoxCollider2D));
            try
            {
                target.layer = LayerMask.NameToLayer("Enemy");
                target.transform.position = origin + direction * 1.2f;
                target.GetComponent<BoxCollider2D>().size = new Vector2(.5f, .7f);
                wall.layer = LayerMask.NameToLayer("Ground");
                wall.transform.position = origin + direction * .65f;
                wall.GetComponent<BoxCollider2D>().size = new Vector2(.2f, 2f);
                Physics2D.SyncTransforms();
                var combat = player.GetComponent<PlayerCombat>();
                var claw = typeof(PlayerCombat).GetMethod("ClawAttack", BindingFlags.Instance | BindingFlags.NonPublic);
                var reach = typeof(PlayerCombat).GetMethod("CanReach", BindingFlags.Instance | BindingFlags.NonPublic);
                var health = target.GetComponent<Health>();
                Check(!(bool)reach.Invoke(combat, new object[] { target.GetComponent<Collider2D>() }), "wall did not block reach");
                combat.StartCoroutine((IEnumerator)claw.Invoke(combat, null));
                await Pump(450);
                Check(health.CurrentHealth == health.MaximumHealth, "claw hit through wall");
                wall.SetActive(false);
                Physics2D.SyncTransforms();
                Check((bool)reach.Invoke(combat, new object[] { target.GetComponent<Collider2D>() }), "exposed target not reachable");
                combat.StartCoroutine((IEnumerator)claw.Invoke(combat, null));
                await Pump(450);
                Check(health.CurrentHealth == health.MaximumHealth - 10, "exposed target must receive one claw hit");
            }
            finally
            {
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(wall);
                body.bodyType = previousType;
            }
        }

        private static async Task Pump(int milliseconds)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < milliseconds)
            {
                UnityEngine.Object.FindAnyObjectByType<PauseMenu>()?.SetPaused(false);
                await Task.Delay(10);
            }
        }

        private static Task WaitForRun() => Wait(() => SceneManager.GetActiveScene().name == "Run" &&
            UnityEngine.Object.FindAnyObjectByType<RoomLayout>() != null &&
            UnityEngine.Object.FindAnyObjectByType<PlayerController>()?.GetComponent<Health>().IsAlive == true);

        private static async Task Wait(Func<bool> condition)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (!condition())
            {
                if (!EditorApplication.isPlaying || watch.Elapsed.TotalSeconds > 5)
                    throw new InvalidOperationException("Checkpoint check transition timed out.");
                await Task.Delay(10);
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Campaign checkpoint: " + message);
        }
    }
}
