using UnityEngine;

namespace NineLives
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(200)]
    public sealed class CatRimLight : MonoBehaviour
    {
        private static readonly Vector2[] Offsets =
        {
            Vector2.left, Vector2.right, Vector2.up, Vector2.down
        };

        private SpriteRenderer source;
        private readonly SpriteRenderer[] edge = new SpriteRenderer[4];

        public static void Ensure(SpriteRenderer renderer)
        {
            if (renderer != null && renderer.GetComponent<CatRimLight>() == null)
                renderer.gameObject.AddComponent<CatRimLight>();
        }

        private void Awake()
        {
            EnsureEdges();
        }

        private void EnsureEdges()
        {
            if (source == null) source = GetComponent<SpriteRenderer>();
            for (int i = 0; i < edge.Length; i++)
            {
                if (edge[i] != null) continue;
                var existing = transform.Find("Cat edge " + i);
                var part = existing != null ? existing.gameObject : new GameObject("Cat edge " + i,
                    typeof(SpriteRenderer));
                if (existing == null) part.transform.SetParent(transform, false);
                part.transform.localPosition = Offsets[i] * .075f;
                edge[i] = part.GetComponent<SpriteRenderer>();
                edge[i].sortingOrder = source.sortingOrder - 1;
                edge[i].color = new Color(.72f, .86f, .96f, .85f);
            }
        }

        private void LateUpdate()
        {
            EnsureEdges();
            foreach (var renderer in edge)
            {
                renderer.sprite = source.sprite;
                renderer.flipX = source.flipX;
                renderer.enabled = source.enabled && source.sprite != null;
            }
        }
    }
}
