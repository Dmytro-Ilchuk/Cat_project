using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NineLives.Editor
{
    public static class CycleCheck
    {
        private static bool running;

        [MenuItem("NineLives/Stage 4/Check full cycle (Play Mode)")]
        private static async void FromMenu()
        {
            try { Debug.Log(await RunAsync()); }
            catch (Exception error) { Debug.LogException(error); }
        }

        // Uses the current in-memory test cycle; stop Play Mode afterwards.
        public static async Task<string> RunAsync()
        {
            if (running || !EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "PlatformTest")
                throw new InvalidOperationException("Enter Play Mode in PlatformTest and run once.");
            running = true;
            void Check(bool ok, string message)
            {
                if (!ok) throw new InvalidOperationException("Stage 4: " + message);
            }
            async Task Reload(PauseMenu old, bool startNext = true)
            {
                if (startNext) old.Resume();
                else SceneManager.LoadScene("PlatformTest");
                for (int i = 0; i < 200; i++)
                {
                    await Task.Delay(25);
                    if (old == null && UnityEngine.Object.FindAnyObjectByType<PauseMenu>() != null) return;
                }
                throw new Exception("Next run did not load.");
            }
            try
            {
                await Reload(UnityEngine.Object.FindAnyObjectByType<PauseMenu>(), false);
                var cycle = CycleController.Ensure();
                cycle.StartNewCat();
                cycle.StartRun();
                cycle.Profile.UnlockedItemIds.Add("cycle-check");
                var profile = cycle.Profile;
                cycle.AddMemories(12);
                cycle.Cycle.PurchasedUpgradeIds.Add("test-upgrade");
                for (int lives = 9; lives > 0; lives--)
                {
                    var menu = UnityEngine.Object.FindAnyObjectByType<PauseMenu>();
                    menu.SetPaused(true);
                    var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                    var hp = player.GetComponent<Health>();
                    Check(hp.CurrentHealth == hp.MaximumHealth, "fresh run HP");
                    Check(UnityEngine.Object.FindObjectsByType<RatController>().Length == 2, "enemies restored");
                    cycle.Run.Food = 3;
                    cycle.Run.BoonIds.Add("test-boon");
                    hp.TakeDamage(999, Vector2.zero);
                    Check(menu.ShowingResult && Time.timeScale == 0, "death result freezes gameplay");
                    Check(cycle.Cycle.LivesRemaining == lives - 1, "one life lost");
                    Check(!cycle.CompleteRun(RunEndReason.Death), "duplicate death rejected");
                    Check(cycle.Run.Food == 0 && cycle.Run.BoonIds.Count == 0, "temporary state cleared");
                    Check(cycle.Cycle.Memories == 12 && cycle.Cycle.PurchasedUpgradeIds.Count == 1, "cycle progress retained");
                    menu.SetPaused(false);
                    player.SetInputBlocked(false);
                    Check(player.InputBlocked && Time.timeScale == 0, "dead player stays blocked");
                    if (lives == 1) Check(cycle.Cycle.IsComplete && !cycle.StartRun(), "zero lives closes cycle");
                    await Reload(menu);
                }
                Check(cycle.Cycle.LivesRemaining == 9 && cycle.Cycle.Memories == 0 && cycle.Cycle.PurchasedUpgradeIds.Count == 0, "new cat resets power");
                Check(cycle.Profile == profile && profile.UnlockedItemIds.Contains("cycle-check"), "profile survives");
                var pause = UnityEngine.Object.FindAnyObjectByType<PauseMenu>();
                pause.Restart();
                Check(cycle.Run.IsActive && cycle.Cycle.LivesRemaining == 9, "abandon needs confirmation");
                pause.Resume();
                Check(cycle.Run.IsActive && !pause.ShowingResult, "cancel abandonment");
                pause.Restart();
                pause.Restart();
                Check(pause.ShowingResult && cycle.Cycle.LivesRemaining == 8, "confirmed abandonment loses one life");
                await Reload(pause);
                Check(cycle.CompleteRun(RunEndReason.Victory) && cycle.Cycle.IsVictory && cycle.Cycle.IsComplete && cycle.Cycle.LivesRemaining == 8, "early victory preserves remaining lives");
                pause = UnityEngine.Object.FindAnyObjectByType<PauseMenu>();
                Check(pause.ShowingResult, "victory result");
                pause.MainMenu();
                for (int i = 0; i < 200 && UnityEngine.Object.FindAnyObjectByType<MainMenu>() == null; i++) await Task.Delay(25);
                Check(SceneManager.GetActiveScene().name == "MainMenu" && Time.timeScale == 1, "return to menu restores time");
                profile.UnlockedItemIds.Remove("cycle-check");
                return "Stage 4 PASS: nine deaths, duplicate death, fresh runs, reset/profile, pause guard, abandonment, victory, menu.";
            }
            finally { running = false; }
        }
    }
}
