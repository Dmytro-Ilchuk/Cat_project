using UnityEngine;

namespace NineLives
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PuddleRipple : MonoBehaviour
    {
        private SpriteRenderer ring;
        private float phase;

        public void Initialize(float offset)
        {
            ring = GetComponent<SpriteRenderer>();
            phase = offset;
        }

        private void Update()
        {
            if (ring == null) return;
            float progress = Mathf.Repeat(Time.time * .42f + phase, 1f);
            float size = Mathf.Lerp(.2f, 1.1f, progress);
            transform.localScale = new Vector3(size, size * .75f, 1f);
            ring.color = new Color(.64f, .85f, 1f, .3f * (1f - progress) * (1f - progress));
        }
    }
}
