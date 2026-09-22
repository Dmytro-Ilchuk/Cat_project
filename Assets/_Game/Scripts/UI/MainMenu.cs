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

        public void Play() => UnityEngine.SceneManagement.SceneManager.LoadScene("PlatformTest");

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
