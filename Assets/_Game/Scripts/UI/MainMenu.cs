using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NineLives
{
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button quitButton;

        private void Start()
        {
            InputSystem.actions.FindActionMap("Gameplay", true).Disable();
            EventSystem.current.SetSelectedGameObject(quitButton.gameObject);
            Debug.Log("NineLives: MainMenu ready.");
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
