using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace NineLives.Editor
{
    public static class PlayerMovementCheck
    {
        private static bool running;

        [MenuItem("NineLives/Check Player Movement (Play Mode)")]
        private static async void RunFromMenu()
        {
            try { Debug.Log(await RunAsync()); }
            catch (Exception error) { Debug.LogException(error); }
        }

        public static async Task<string> RunAsync()
        {
            if (running || !EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MovementTest")
                throw new InvalidOperationException("Open MovementTest, enter Play Mode, then run this check once.");

            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            var visual = player.transform.Find("Visual");
            var move = InputSystem.actions.FindAction("Gameplay/Move", true);
            Vector2 originalPosition = body.position;
            Vector2 originalVelocity = body.linearVelocity;
            Vector3 originalScale = visual.localScale;
            float gravity = body.gravityScale;
            bool enabled = move.enabled;
            var background = InputSystem.settings.backgroundBehavior;
            bool runInBackground = Application.runInBackground;
            var pad = InputSystem.AddDevice<Gamepad>();
            running = true;
            void Input(float x) => InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(x, 0) });
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException("Player movement: " + message);
            }

            try
            {
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                Application.runInBackground = true;
                move.Enable();
                body.position = new Vector2(-3, 0.7f);
                body.linearVelocity = Vector2.zero;
                Input(0);
                await WaitPhysics(0.4f);
                Check(player.IsGrounded, "floor was not detected");

                Input(1);
                await WaitPhysics(0.06f);
                Check(body.linearVelocity.x > 0 && body.linearVelocity.x < 6, "acceleration must be gradual");
                await WaitPhysics(0.25f);
                Check(Mathf.Abs(body.linearVelocity.x - 6) < 0.05f && visual.localScale.x > 0, "right movement/facing");
                float releaseX = body.position.x;
                Input(0);
                await WaitPhysics(0.25f);
                Check(Mathf.Abs(body.linearVelocity.x) < 0.01f && body.position.x - releaseX < 0.6f, "release braking");

                Input(1);
                await WaitPhysics(0.25f);
                Input(-1);
                await WaitPhysics(0.4f);
                Check(Mathf.Abs(body.linearVelocity.x + 6) < 0.05f && visual.localScale.x < 0, "reversal/facing");
                Check(player.transform.localScale == Vector3.one, "facing changed the physics root scale");

                body.position = new Vector2(-1, 0.46f);
                body.linearVelocity = Vector2.zero;
                Input(1);
                await WaitPhysics(0.55f);
                Check(body.position.x > 1 && player.IsGrounded, "stuck on floor seam");
                move.Disable();
                await WaitPhysics(0.25f);
                Check(Mathf.Abs(body.linearVelocity.x) < 0.01f, "disabled input still moves the player");

                move.Enable();
                body.gravityScale = 0;
                body.position = new Vector2(7, 2);
                body.linearVelocity = Vector2.zero;
                Input(1);
                await WaitPhysics(0.4f);
                Check(body.position.x > 7.1f && body.position.x < 7.25f, "wall did not block movement");
                Check(!player.IsGrounded, "a wall was mistaken for ground");
                return "PASS: ground, acceleration, speed, braking, reversal, visual flip, floor seam, input disable, wall collision.";
            }
            finally
            {
                InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior = background;
                Application.runInBackground = runInBackground;
                if (!enabled) move.Disable(); else move.Enable();
                if (body != null)
                {
                    body.gravityScale = gravity;
                    body.position = originalPosition;
                    body.linearVelocity = originalVelocity;
                    visual.localScale = originalScale;
                }
                running = false;
            }
        }

        private static async Task WaitPhysics(float seconds)
        {
            float end = Time.fixedTime + seconds;
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (Time.fixedTime < end)
            {
                if (!EditorApplication.isPlaying || timeout.Elapsed.TotalSeconds > 10)
                    throw new InvalidOperationException("Play Mode stopped or physics is paused.");
                await Task.Delay(10);
            }
        }
    }
}
