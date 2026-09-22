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

        private void Awake() => pixels = GetComponent<PixelPerfectCamera>();

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
            transform.position = position;
        }
    }
}
