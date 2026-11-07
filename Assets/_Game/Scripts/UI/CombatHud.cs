using UnityEngine;
using UnityEngine.UI;

namespace NineLives
{
    public sealed class CombatHud : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private Text healthLabel;
        [SerializeField] private Text mapLabel;
        [SerializeField] private RawImage[] hearts;
        [SerializeField] private RectTransform mapMarker;
        [SerializeField] private RectTransform[] mapStops;
        private int displayedHealth = -1;
        private int displayedPosition = -1;

        private void Update()
        {
            if (playerHealth == null) return;
            int position = CycleController.Instance?.Run.Position ?? 0;
            if (displayedHealth == playerHealth.CurrentHealth && displayedPosition == position) return;
            displayedHealth = playerHealth.CurrentHealth;
            displayedPosition = position;
            if (healthLabel != null) healthLabel.text = $"{displayedHealth}/{playerHealth.MaximumHealth}";
            for (int i = 0; hearts != null && i < hearts.Length; i++)
            {
                float amount = Mathf.Clamp01(displayedHealth * hearts.Length / (float)playerHealth.MaximumHealth - i);
                if (hearts[i] != null) hearts[i].color = Color.Lerp(new Color(.22f, .2f, .29f, .9f), Color.white, amount);
            }
            if (mapLabel != null) mapLabel.text = $"МІСТО {position + 1}/{RunRoute.RoomCount}";
            if (mapMarker != null && mapStops != null && mapStops.Length > 0)
                mapMarker.anchoredPosition = mapStops[Mathf.Clamp(position, 0, mapStops.Length - 1)].anchoredPosition;
        }
    }
}
