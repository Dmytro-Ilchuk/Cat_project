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
        [SerializeField] private UnityEngine.UI.Button restartButton;
        [SerializeField] private UnityEngine.UI.Button menuButton;
        [SerializeField] private UnityEngine.UI.Text title;
        private InputAction pause;
        private InputActionMap gameplay;
        private CycleController cycle;
        private bool confirmingAbandon;
        private bool leaving;
        public bool IsPaused { get; private set; }
        public bool ShowingResult { get; private set; }

        private void Awake()
        {
            gameplay = InputSystem.actions.FindActionMap("Gameplay", true);
            pause = gameplay.FindAction("Pause", true).Clone();
            pause.Enable();
            gameplay.Enable();
            panel.SetActive(false);
            cycle = CycleController.Ensure();
            cycle.RunCompleted += ShowResult;
        }

        private void Start()
        {
            // Direct Play in PlatformTest starts a run too.
            if (cycle.Cycle.IsComplete) cycle.StartNewCat();
            if (!cycle.Run.IsActive) cycle.StartRun();
        }

        private void Update()
        {
            if (pause.WasPressedThisFrame() && !ShowingResult) SetPaused(!IsPaused);
        }

        public void SetPaused(bool paused)
        {
            if (ShowingResult || leaving) return;
            confirmingAbandon = false;
            title.text = "ПАУЗА";
            resumeButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "Продовжити";
            restartButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "Завершити забіг";
            restartButton.gameObject.SetActive(true);
            menuButton.gameObject.SetActive(false);
            SetOverlay(paused);
        }

        private void SetOverlay(bool visible)
        {
            IsPaused = visible;
            player.SetInputBlocked(visible);
            if (visible) gameplay.Disable(); else gameplay.Enable();
            Time.timeScale = visible ? 0f : 1f;
            panel.SetActive(visible);
            EventSystem.current.SetSelectedGameObject(visible ? resumeButton.gameObject : null);
        }

        private void ShowResult(RunEndReason reason)
        {
            ShowingResult = true;
            confirmingAbandon = false;
            string heading = reason == RunEndReason.Victory ? "ПЕРЕМОГА!"
                : cycle.Cycle.IsComplete ? "ІСТОРІЮ КОТА ЗАВЕРШЕНО"
                : reason == RunEndReason.Death ? "СПРОБУ ЗАВЕРШЕНО" : "ЗАБІГ ПРИПИНЕНО";
            title.text = $"{heading}\nЖиття: {cycle.Cycle.LivesRemaining} / 9   Спогади: {cycle.Cycle.Memories}";
            resumeButton.GetComponentInChildren<UnityEngine.UI.Text>().text = cycle.Cycle.IsComplete ? "Новий кіт" : "Наступна спроба";
            restartButton.gameObject.SetActive(false);
            menuButton.gameObject.SetActive(true);
            SetOverlay(true);
        }

        public void Resume()
        {
            if (leaving) return;
            if (!ShowingResult) { SetPaused(false); return; }
            if (cycle.Cycle.IsComplete) cycle.StartNewCat();
            if (!cycle.StartRun()) return;
            leaving = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void Restart()
        {
            if (ShowingResult || leaving) return;
            if (confirmingAbandon)
            {
                cycle.CompleteRun(RunEndReason.Abandoned);
                return;
            }
            SetPaused(true);
            confirmingAbandon = true;
            title.text = "ПРИПИНИТИ ЗАБІГ?\nБуде втрачено одне життя";
            resumeButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "Скасувати";
            restartButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "Так, завершити";
        }

        public void MainMenu()
        {
            if (leaving) return;
            if (!ShowingResult) { Restart(); return; }
            leaving = true;
            SceneManager.LoadScene("MainMenu");
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && !IsPaused) SetPaused(true);
        }

        private void OnDestroy()
        {
            if (cycle != null) cycle.RunCompleted -= ShowResult;
            pause?.Dispose();
            Time.timeScale = 1f;
        }
    }
}
