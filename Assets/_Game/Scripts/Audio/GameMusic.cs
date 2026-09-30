using UnityEngine;

namespace NineLives
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip music;
        private static GameMusic instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            var source = GetComponent<AudioSource>();
            source.clip = music;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        private void Start()
        {
            if (instance != this) return;
            var source = GetComponent<AudioSource>();
            if (music != null && !source.isPlaying) source.Play();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
