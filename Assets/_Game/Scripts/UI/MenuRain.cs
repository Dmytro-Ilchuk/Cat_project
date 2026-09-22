using UnityEngine;
using UnityEngine.UI;

namespace NineLives
{
    // One UI mesh, no per-drop GameObjects. Runs even when gameplay is paused.
    public sealed class MenuRain : MaskableGraphic
    {
        [SerializeField, Range(20, 240)] private int drops = 130;
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        private void Update() => SetVerticesDirty();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (PlayerPrefs.GetInt("MenuRain", 1) == 0) return;
            var rect = rectTransform.rect;
            float now = Time.unscaledTime;
            for (int i = 0; i < drops; i++)
            {
                float seed = Mathf.Repeat(i * .618034f, 1);
                float depth = .35f + Mathf.Repeat(i * .37231f, .65f);
                float speed = 210 + depth * 420;
                float y = Mathf.Repeat(i * 137.3f - now * speed, rect.height + 40) - 20 + rect.yMin;
                float x = Mathf.Repeat(seed * rect.width - now * 36 * depth, rect.width) + rect.xMin;
                float length = 9 + depth * 24;
                var c = new Color(.55f, .73f, 1, .07f + .16f * depth);
                int start = vh.currentVertCount;
                vh.AddVert(new Vector3(x, y), c, Vector2.zero);
                vh.AddVert(new Vector3(x + 1.2f, y), c, Vector2.zero);
                vh.AddVert(new Vector3(x + 4.2f, y + length), c, Vector2.zero);
                vh.AddVert(new Vector3(x + 3, y + length), c, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
