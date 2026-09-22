using UnityEngine;
using UnityEngine.SceneManagement;

namespace NineLives
{
    public sealed class Bootstrap : MonoBehaviour
    {
        private void Awake() => CycleController.Ensure();

        private void Start() => SceneManager.LoadScene("MainMenu");
    }
}
