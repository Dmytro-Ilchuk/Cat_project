using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NineLives
{
    [RequireComponent(typeof(Camera), typeof(PixelPerfectCamera))]
    public sealed class RoomCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Rect room = new Rect(-9, -2, 52, 14);
        [SerializeField] private Vector2 offset = new Vector2(0, 2);
        private PixelPerfectCamera pixels;
        private static float shake;

        public static void Impulse(float amount)
        {
            if (PlayerPrefs.GetInt("ScreenShake", 1) != 0) shake = Mathf.Max(shake, amount);
        }

        public void SetBounds(Bounds bounds) => room = new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);

        private void Awake() { pixels = GetComponent<PixelPerfectCamera>(); shake = 0; }

        private void LateUpdate()
        {
            if (target == null) return;
            // Windowbox keeps the visible area equal to the reference resolution.
            float halfWidth = pixels.refResolutionX / (2f * pixels.assetsPPU);
            float halfHeight = pixels.refResolutionY / (2f * pixels.assetsPPU);
            Vector3 position = pixels.RoundToPixel(target.position + (Vector3)offset);
            position.x = room.width <= halfWidth * 2 ? room.center.x : Mathf.Clamp(position.x, room.xMin + halfWidth, room.xMax - halfWidth);
            position.y = room.height <= halfHeight * 2 ? room.center.y : Mathf.Clamp(position.y, room.yMin + halfHeight, room.yMax - halfHeight);
            position.z = transform.position.z;
            if (shake > 0 && Time.timeScale > 0)
            {
                position += (Vector3)(Random.insideUnitCircle * shake);
                position = pixels.RoundToPixel(position);
                shake = Mathf.MoveTowards(shake, 0, Time.deltaTime);
            }
            transform.position = position;
        }
    }
}
