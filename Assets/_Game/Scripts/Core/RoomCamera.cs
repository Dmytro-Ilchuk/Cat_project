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
        [SerializeField] private float horizontalSmoothTime = .18f;
        [SerializeField] private float verticalSmoothTime = .4f;
        [SerializeField] private float jumpDeadZone = 1.15f;
        private PixelPerfectCamera pixels;
        private PlayerController player;
        private Vector2 focus;
        private Vector2 velocity;
        private bool snapNextFrame = true;
        private static float shake;

        public static void Impulse(float amount)
        {
            if (PlayerPrefs.GetInt("ScreenShake", 1) != 0) shake = Mathf.Max(shake, amount);
        }

        public void SetBounds(Bounds bounds)
        {
            // Leave vertical breathing room even when the authored camera box is shorter than the view.
            room = new Rect(bounds.min.x, bounds.min.y - 1f, bounds.size.x, bounds.size.y + 5f);
            snapNextFrame = true;
            velocity = Vector2.zero;
        }

        private void Awake()
        {
            pixels = GetComponent<PixelPerfectCamera>();
            if (target != null) player = target.GetComponent<PlayerController>();
            shake = 0;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            // Windowbox keeps the visible area equal to the reference resolution.
            float halfWidth = pixels.refResolutionX / (2f * pixels.assetsPPU);
            float halfHeight = pixels.refResolutionY / (2f * pixels.assetsPPU);
            Vector2 desired = (Vector2)target.position + offset;
            desired.x = room.width <= halfWidth * 2 ? room.center.x : Mathf.Clamp(desired.x, room.xMin + halfWidth, room.xMax - halfWidth);
            desired.y = room.height <= halfHeight * 2 ? room.center.y : Mathf.Clamp(desired.y, room.yMin + halfHeight, room.yMax - halfHeight);
            if (snapNextFrame)
            {
                focus = desired;
                snapNextFrame = false;
            }
            else
            {
                bool airborne = player != null && !player.IsGrounded;
                float deadZone = airborne ? jumpDeadZone : .35f;
                float verticalTarget = Mathf.Abs(desired.y - focus.y) <= deadZone
                    ? focus.y : desired.y - Mathf.Sign(desired.y - focus.y) * deadZone;
                focus.x = Mathf.SmoothDamp(focus.x, desired.x, ref velocity.x, horizontalSmoothTime);
                focus.y = Mathf.SmoothDamp(focus.y, verticalTarget, ref velocity.y,
                    airborne ? verticalSmoothTime * 1.4f : verticalSmoothTime);
            }
            Vector3 position = pixels.RoundToPixel(new Vector3(focus.x, focus.y, transform.position.z));
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
