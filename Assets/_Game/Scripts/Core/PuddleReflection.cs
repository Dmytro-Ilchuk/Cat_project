using UnityEngine;

namespace NineLives
{
    // Mirrors the current animation frame into masked puddles on the actual ground.
    [DisallowMultipleComponent]
    public sealed class PuddleReflection : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float opacity = .38f;
        [SerializeField, Range(.1f, 1f)] private float height = .42f;

        private SpriteRenderer source;
        private SpriteRenderer reflection;
        private int groundMask;

        public static void Ensure(SpriteRenderer renderer)
        {
            if (renderer != null && renderer.GetComponent<PuddleReflection>() == null)
                renderer.gameObject.AddComponent<PuddleReflection>();
        }

        private void Awake()
        {
            source = GetComponent<SpriteRenderer>();
            groundMask = 1 << LayerMask.NameToLayer("Ground");
            var child = new GameObject("Puddle reflection", typeof(SpriteRenderer));
            child.transform.SetParent(transform, false);
            reflection = child.GetComponent<SpriteRenderer>();
            reflection.sortingOrder = source.sortingOrder - 1;
            reflection.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        private void LateUpdate()
        {
            if (source == null || reflection == null) return;
            reflection.sprite = source.sprite;
            reflection.flipX = source.flipX;
            float floorY = float.NegativeInfinity;
            foreach (var hit in Physics2D.RaycastAll(source.bounds.center + Vector3.up * .2f,
                         Vector2.down, 24f, groundMask))
            {
                string name = hit.collider.gameObject.name;
                if (name == "Road" || name.ToLowerInvariant().Contains("quay"))
                {
                    floorY = hit.point.y;
                    break;
                }
            }
            reflection.enabled = source.enabled && source.sprite != null &&
                                 floorY > float.NegativeInfinity;
            if (!reflection.enabled) return;
            float distance = Mathf.Max(0f, source.bounds.min.y - floorY);
            reflection.color = new Color(.52f, .68f, .82f,
                source.color.a * opacity * Mathf.Clamp01(1f - distance / 6f));
            float pivotToFeet = source.bounds.min.y - source.transform.position.y;
            reflection.transform.position = new Vector3(source.transform.position.x,
                floorY - .05f + pivotToFeet * height, source.transform.position.z);
            reflection.transform.localScale = new Vector3(1f, -height, 1f);
        }
    }
}
