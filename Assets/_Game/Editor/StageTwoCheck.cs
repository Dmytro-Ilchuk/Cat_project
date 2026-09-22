using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering.Universal;

namespace NineLives.Editor
{
    public static class StageTwoCheck
    {
        private static bool running;
        [MenuItem("NineLives/Check Stage 2 (Play Mode)")]
        private static async void FromMenu()
        {
            try { Debug.Log(await RunAsync()); }
            catch (Exception e) { Debug.LogException(e); }
        }

        public static async Task<string> RunAsync()
        {
            if (running || !EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "PlatformTest")
                throw new InvalidOperationException("Open PlatformTest, enter Play Mode, run once with default prefab settings.");
            running = true;
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var pause = UnityEngine.Object.FindAnyObjectByType<PauseMenu>();
            var body = player.GetComponent<Rigidbody2D>();
            var floor = GameObject.Find("Start Floor");
            var originalPosition = body.position;
            bool wasPaused = pause.IsPaused;
            bool background = Application.runInBackground;
            var focus = InputSystem.settings.backgroundBehavior;
            var pad = InputSystem.AddDevice<Gamepad>();
            void Input(float x = 0, bool jump = false, bool dash = false, bool start = false)
            {
                var state = new GamepadState { leftStick = new Vector2(x, 0) };
                state = state.WithButton(GamepadButton.South, jump).WithButton(GamepadButton.RightShoulder, dash).WithButton(GamepadButton.Start, start);
                InputSystem.QueueStateEvent(pad, state);
            }
            void Check(bool ok, string message)
            {
                if (!ok) throw new InvalidOperationException("Stage 2: " + message);
                Debug.Log("Stage 2 PASS: " + message);
            }
            async Task Reset(float x = -5, float y = 0.6f)
            {
                Input(); player.Respawn(); body.gravityScale = 3;
                body.position = new Vector2(x, y);
                await Wait(0.15f);
            }
            async Task<float> JumpHeight(float hold)
            {
                await Reset();
                float startY = body.position.y, peak = startY;
                Input(jump: true);
                float began = Time.time;
                while (Time.time - began < 0.85f)
                {
                    if (Time.time - began >= hold) Input();
                    peak = Mathf.Max(peak, body.position.y);
                    await Wait(0.016f);
                }
                return peak - startY;
            }
            try
            {
                Application.runInBackground = true;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                pause.SetPaused(false);
                float shortJump = await JumpHeight(0.06f);
                float fullJump = await JumpHeight(0.5f);
                Check(shortJump < 1.1f && fullJump > 1.7f && fullJump > shortJump + 0.7f, $"variable jump height ({shortJump:F2} / {fullJump:F2})");

                await Reset(); floor.SetActive(false); await Wait(0.035f);
                Input(jump: true); await Wait(0.05f);
                Check(body.linearVelocity.y > 5 && !player.IsGrounded, "coyote jump");
                floor.SetActive(true);
                await Reset(); floor.SetActive(false); await Wait(0.2f);
                Input(jump: true); await Wait(0.05f);
                Check(body.linearVelocity.y < 0, "expired coyote time cannot jump");
                floor.SetActive(true);

                await Reset(-5, 3);
                while (body.position.y > 0.85f) await Wait(0.016f);
                Input(jump: true); await Wait(0.12f);
                Check(body.linearVelocity.y > 5, "buffered jump on landing");
                Input(); await Wait(0.05f);
                float before = body.linearVelocity.y;
                Input(jump: true); await Wait(0.05f);
                Check(body.linearVelocity.y < before, "no double jump");

                await Reset(-3.5f);
                foreach (var ledge in new[] { new Vector2(-1, 1.25f), new Vector2(2, 2.5f), new Vector2(5, 3.75f), new Vector2(8, 5f), new Vector2(12, 3.75f) })
                {
                    Input(); await Wait(0.04f);
                    Input(1, true);
                    float deadline = Time.time + 1.5f;
                    while (body.position.x < ledge.x - 0.35f && Time.time < deadline) await Wait(0.016f);
                    Input(0, true);
                    await Wait(0.6f);
                    Check(player.IsGrounded && Mathf.Abs(body.position.y - ledge.y - 0.45f) < 0.12f, "reachable ledge at x=" + ledge.x);
                }
                await Reset(16.5f);
                Input(1); await Wait(0.14f);
                Input(1, true); await Wait(0.28f);
                Input(1, true, true); await Wait(0.9f);
                Check(body.position.x > 24 && body.position.x < 28 && player.IsGrounded, "wide pit crossed with jump and dash");

                await Reset(-6);
                float dashStart = body.position.x;
                Input(dash: true); await Wait(0.05f);
                Check(player.IsDashing && body.linearVelocity.x > 15, "dash starts");
                await Wait(0.2f);
                Check(!player.IsDashing && body.gravityScale == 3 && body.position.x - dashStart < 3.2f, "dash duration and gravity restoration");
                Input(); await Wait(0.04f); Input(dash: true); await Wait(0.05f);
                Check(!player.IsDashing, "dash cooldown");

                await Reset(-5, 9); body.gravityScale = 0;
                Input(dash: true); await Wait(0.22f); body.gravityScale = 0;
                Input(); await Wait(0.7f); Input(dash: true); await Wait(0.05f);
                Check(!player.IsDashing, "one air dash even after cooldown");
                Input(); body.position = new Vector2(-5, 0.7f); body.linearVelocity = Vector2.zero; body.gravityScale = 3;
                await Wait(0.25f); Input(dash: true); await Wait(0.05f);
                Check(player.IsDashing, "landing restores dash");

                await Reset(40); Input(dash: true); await Wait(0.25f);
                Check(body.position.x <= 41.22f, "dash cannot cross wall");
                await Reset(26); Input(1); await Wait(1.8f);
                Check(body.position.x > 34.8f && body.position.y < 0.6f, "narrow passage is traversable");

                await Reset(); Input(1, true, true, true); await Task.Delay(150);
                Check(pause.IsPaused && player.InputBlocked && Time.timeScale == 0, "Start pauses and blocks gameplay");
                Vector2 pausedPosition = body.position;
                Input(1, true, true); await Task.Delay(150);
                Check(body.position == pausedPosition && !player.IsDashing, "paused body stays still");
                pause.Resume(); await Wait(0.05f);
                Check(body.linearVelocity.y <= 0.01f && !player.IsDashing, "held buttons do not jump or dash after resume");

                Input(); body.position = new Vector2(20, -7); await Wait(0.08f);
                Check(Vector2.Distance(body.position, new Vector2(-5, 0.6f)) < 0.3f && !player.IsDashing, "fall returns to safe spawn");
                var cam = Camera.main;
                var pixels = cam.GetComponent<PixelPerfectCamera>();
                Check(pixels != null && pixels.assetsPPU == 16 && pixels.cropFrame == PixelPerfectCamera.CropFrame.Windowbox, "URP Pixel Perfect configuration");
                body.position = new Vector2(40, 9); body.gravityScale = 0; body.linearVelocity = Vector2.zero; await Wait(0.05f);
                Check(cam.transform.position.x <= 33.01f && cam.transform.position.y <= 6.38f && cam.transform.position.x > 30, "camera follows within room bounds");
                Check(Mathf.Abs(cam.transform.position.x * 16 - Mathf.Round(cam.transform.position.x * 16)) < 0.001f, "camera aligns to pixel grid");
                return "PASS: Stage 2 jump, buffer, coyote, dash, walls, passage, pause, respawn and camera.";
            }
            finally
            {
                floor.SetActive(true);
                InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior = focus;
                Application.runInBackground = background;
                if (player != null)
                {
                    player.Respawn(); body.gravityScale = 3; body.position = originalPosition;
                    pause.SetPaused(wasPaused);
                }
                running = false;
            }
        }

        private static async Task Wait(float seconds)
        {
            float end = Time.fixedTime + seconds;
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (Time.fixedTime < end)
            {
                if (!EditorApplication.isPlaying || timeout.Elapsed.TotalSeconds > 10)
                    throw new InvalidOperationException("Physics stopped or paused during Stage 2 check.");
                await Task.Delay(5);
            }
        }
    }
}
