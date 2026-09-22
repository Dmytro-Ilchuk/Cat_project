using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NineLives
{
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button quitButton;
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject mainButtons;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private UnityEngine.UI.Text rainLabel;
        [SerializeField] private UnityEngine.UI.Text shakeLabel;

        private void Start()
        {
            InputSystem.actions.FindActionMap("Gameplay", true).Disable();
            Time.timeScale = 1f;
#if UNITY_WEBGL && !UNITY_EDITOR
            quitButton.gameObject.SetActive(false);
#endif
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
            Debug.Log("NineLives: MainMenu ready.");
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !mainButtons.activeSelf) Back();
        }

        public void Settings()
        {
            mainButtons.SetActive(false);
            settingsPanel.SetActive(true);
            RefreshSettings();
            EventSystem.current.SetSelectedGameObject(settingsPanel.GetComponentInChildren<Button>().gameObject);
        }

        public void Credits()
        {
            mainButtons.SetActive(false);
            creditsPanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(creditsPanel.GetComponentInChildren<Button>().gameObject);
        }

        public void Back()
        {
            settingsPanel.SetActive(false);
            creditsPanel.SetActive(false);
            mainButtons.SetActive(true);
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
        }

        public void ToggleRain() { Toggle("MenuRain"); RefreshSettings(); }
        public void ToggleShake() { Toggle("ScreenShake"); RefreshSettings(); }
        private static void Toggle(string key) { PlayerPrefs.SetInt(key, 1 - PlayerPrefs.GetInt(key, 1)); PlayerPrefs.Save(); }
        private void RefreshSettings()
        {
            rainLabel.text = "ДОЩ: " + (PlayerPrefs.GetInt("MenuRain", 1) != 0 ? "УВІМК." : "ВИМК.");
            shakeLabel.text = "ТРЯСКА КАМЕРИ: " + (PlayerPrefs.GetInt("ScreenShake", 1) != 0 ? "УВІМК." : "ВИМК.");
        }

        public void Play()
        {
            var cycle = CycleController.Ensure();
            if (cycle.Cycle.IsComplete) cycle.StartNewCat();
            if (!cycle.StartRun()) return;
            UnityEngine.SceneManagement.SceneManager.LoadScene("PlatformTest");
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif !UNITY_WEBGL
            Application.Quit();
#endif
        }
    }
}
