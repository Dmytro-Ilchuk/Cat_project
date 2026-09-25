using UnityEngine;
using UnityEngine.UI;

namespace NineLives
{
    // Builds lightweight ambient menu layers at runtime so the menu remains editable as a normal uGUI scene.
    [DisallowMultipleComponent]
    public sealed class MenuAtmosphere : MonoBehaviour
    {
        private readonly Image[] haze = new Image[3];
        private readonly Image[] lamps = new Image[4];
        private float[] lampSeeds;

        private void Awake()
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null) return;
            var parent = canvas.transform;
            for (int i = 0; i < haze.Length; i++)
            {
                haze[i] = Create(parent, "Mist layer " + (i + 1), new Color(.12f, .18f, .34f, .12f - i * .025f));
                var rect = haze[i].rectTransform;
                rect.anchorMin = new Vector2(-.1f, .1f + i * .08f);
                rect.anchorMax = new Vector2(1.1f, .75f + i * .08f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                haze[i].transform.SetAsFirstSibling();
            }
            lampSeeds = new float[lamps.Length];
            for (int i = 0; i < lamps.Length; i++)
            {
                lamps[i] = Create(parent, "Flickering lamp " + (i + 1), new Color(1f, .62f, .2f, .35f));
                var rect = lamps[i].rectTransform;
                float x = .16f + i * .23f;
                rect.anchorMin = new Vector2(x, .57f + (i % 2) * .15f);
                rect.anchorMax = new Vector2(x, .57f + (i % 2) * .15f);
                rect.sizeDelta = new Vector2(30, 92);
                lampSeeds[i] = 13.37f * (i + 1);
                lamps[i].transform.SetAsFirstSibling();
            }
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            for (int i = 0; i < haze.Length; i++)
            {
                if (haze[i] == null) continue;
                haze[i].rectTransform.anchoredPosition = new Vector2(Mathf.Sin(time * (.08f + i * .03f)) * (26 + i * 14), 0);
            }
            for (int i = 0; i < lamps.Length; i++)
            {
                if (lamps[i] == null) continue;
                float pulse = Mathf.PerlinNoise(lampSeeds[i], time * (1.6f + i * .2f));
                lamps[i].color = new Color(1f, .62f, .2f, Mathf.Lerp(.12f, .48f, pulse));
            }
        }

        private static Image Create(Transform parent, string name, Color color)
        {
            var layer = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            layer.transform.SetParent(parent, false);
            var image = layer.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
