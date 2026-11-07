using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NineLives
{
    [DefaultExecutionOrder(-100)]
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Text title;
        private InputAction pause;
        private InputActionMap gameplay;
        private CycleController cycle;
        private bool leaving;
        public bool IsPaused { get; private set; }
        public bool ShowingResult { get; private set; }

        private void Awake()
        {
            gameplay = InputSystem.actions.FindActionMap("Gameplay", true);
            pause = gameplay.FindAction("Pause", true).Clone();
            pause.Enable();
            panel.SetActive(false);
            cycle = CycleController.Ensure();
            cycle.RunCompleted += ShowResult;
        }

        private void Update()
        {
            if (pause.WasPressedThisFrame() && !ShowingResult) SetPaused(!IsPaused);
        }

        public void SetPaused(bool paused)
        {
            if (ShowingResult || leaving) return;
            title.text = "ПАУЗА\nПрогрес секції збережено";
            resumeButton.GetComponentInChildren<Text>().text = "Продовжити";
            restartButton.GetComponentInChildren<Text>().text = "Повторити секцію";
            restartButton.gameObject.SetActive(true);
            menuButton.gameObject.SetActive(true);
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
            if (leaving) return;
            ShowingResult = true;
            bool victory = reason == RunEndReason.Victory;
            title.text = victory ? "ПЕРЕМОГА!\nКіт дістався додому." : "СПРОБУЙ ЩЕ\nСекція почнеться з безпечної точки";
            resumeButton.GetComponentInChildren<Text>().text = victory ? "Зіграти ще" : "Повторити секцію";
            restartButton.gameObject.SetActive(false);
            menuButton.gameObject.SetActive(true);
            SetOverlay(true);
        }

        public void Resume()
        {
            if (leaving) return;
            if (!ShowingResult) { SetPaused(false); return; }
            bool victory = cycle.Cycle.IsVictory;
            if (victory) cycle.StartNewCat();
            ReloadSection(!victory);
        }

        public void Restart()
        {
            if (!leaving) ReloadSection(true);
        }

        private void ReloadSection(bool fromSave)
        {
            leaving = true;
            if (cycle.Run.IsActive) cycle.CompleteRun(RunEndReason.Abandoned);
            if (!cycle.StartRun(continueFromSave: fromSave)) { leaving = false; return; }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void MainMenu()
        {
            if (leaving) return;
            leaving = true;
            if (cycle.Run.IsActive) cycle.CompleteRun(RunEndReason.Abandoned);
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
