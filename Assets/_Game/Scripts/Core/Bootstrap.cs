using UnityEngine;
using UnityEngine.SceneManagement;

namespace NineLives
{
    public sealed class Bootstrap : MonoBehaviour
    {
        private void Start() => SceneManager.LoadScene("MainMenu");
    }
}
