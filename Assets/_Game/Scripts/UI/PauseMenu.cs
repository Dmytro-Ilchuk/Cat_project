using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NineLives
{
    [DefaultExecutionOrder(-100)]
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private GameObject panel;
        [SerializeField] private UnityEngine.UI.Button resumeButton;
        private InputAction pause;
        private InputActionMap gameplay;
        public bool IsPaused { get; private set; }

        private void Awake()
        {
            gameplay = InputSystem.actions.FindActionMap("Gameplay", true);
            pause = gameplay.FindAction("Pause", true).Clone();
            pause.Enable();
            gameplay.Enable();
            panel.SetActive(false);
        }

        private void Update()
        {
            if (pause.WasPressedThisFrame()) SetPaused(!IsPaused);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            player.SetInputBlocked(paused);
            if (paused) gameplay.Disable(); else gameplay.Enable();
            Time.timeScale = paused ? 0f : 1f;
            panel.SetActive(paused);
            EventSystem.current.SetSelectedGameObject(paused ? resumeButton.gameObject : null);
        }

        public void Resume() => SetPaused(false);
        public void Restart()
        {
            player.Respawn();
            SetPaused(false);
        }
        public void MainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused && !IsPaused) SetPaused(true);
        }
        private void OnDestroy()
        {
            pause?.Dispose();
            Time.timeScale = 1f;
        }
    }
}
