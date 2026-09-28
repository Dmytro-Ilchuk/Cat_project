using System.Collections.Generic;
using UnityEngine;

namespace NineLives
{
    public static class StreetPowerLines
    {
        private static Material cableMaterial;

        public static void Connect(Transform room, List<Vector2> anchors)
        {
            if (anchors.Count < 2) return;
            anchors.Sort((a, b) => a.x.CompareTo(b.x));
            if (cableMaterial == null)
                cableMaterial = new Material(Shader.Find("Sprites/Default"));

            for (int i = 1; i < anchors.Count; i++)
            {
                Vector2 left = anchors[i - 1];
                Vector2 right = anchors[i];
                if (right.x - left.x < 2f) continue;
                for (int cable = 0; cable < 2; cable++)
                {
                    var wire = new GameObject("Overhead power cable", typeof(LineRenderer));
                    wire.transform.SetParent(room, false);
                    var line = wire.GetComponent<LineRenderer>();
                    line.sharedMaterial = cableMaterial;
                    line.sortingOrder = 1;
                    line.useWorldSpace = true;
                    line.startWidth = line.endWidth = .0625f;
                    line.startColor = line.endColor = cable == 0
                        ? new Color(.22f, .25f, .29f, .9f)
                        : new Color(.16f, .19f, .23f, .82f);
                    line.positionCount = 13;
                    float sag = Mathf.Min(.55f, .16f + (right.x - left.x) * .012f);
                    for (int point = 0; point < line.positionCount; point++)
                    {
                        float t = point / (line.positionCount - 1f);
                        float x = Mathf.Lerp(left.x, right.x, t);
                        float y = Mathf.Lerp(left.y, right.y, t) -
                            sag * 4f * t * (1f - t) - cable * .18f;
                        line.SetPosition(point, new Vector3(
                            Mathf.Round(x * 16f) / 16f, Mathf.Round(y * 16f) / 16f, 0f));
                    }
                }
            }
        }
    }
}
