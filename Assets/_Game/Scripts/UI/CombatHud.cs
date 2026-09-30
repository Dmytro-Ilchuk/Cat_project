using UnityEngine;
using UnityEngine.UI;

namespace NineLives
{
    public sealed class CombatHud : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private Text healthLabel;
        [SerializeField] private Text foodLabel;
        [SerializeField] private Text livesLabel;
        [SerializeField] private Text mapLabel;
        [SerializeField] private RawImage[] hearts;
        [SerializeField] private RectTransform mapMarker;
        [SerializeField] private RectTransform[] mapStops;

        private int displayedHealth = -1;
        private int displayedFood = -1;
        private int displayedLives = -1;
        private int displayedPosition = -1;
        private bool displayedInterlude;
        private CycleController cycle;

        private void OnEnable()
        {
            if (playerHealth == null)
                playerHealth = FindAnyObjectByType<PlayerController>()?.GetComponent<Health>();
            cycle = CycleController.Ensure();
            cycle.StateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (cycle != null) cycle.StateChanged -= Refresh;
        }

        private void Update()
        {
            var state = CycleController.Instance;
            if (playerHealth != null && (displayedHealth != playerHealth.CurrentHealth ||
                (state != null && (displayedFood != state.Run.Food ||
                displayedLives != state.Cycle.LivesRemaining || displayedPosition != state.Run.Position ||
                displayedInterlude != state.Run.InInterlude))))
                Refresh();
        }

        private void Refresh()
        {
            if (playerHealth == null) return;
            displayedHealth = playerHealth.CurrentHealth;
            if (healthLabel != null)
                healthLabel.text = $"{displayedHealth}/{playerHealth.MaximumHealth}";
            if (hearts != null)
                for (int i = 0; i < hearts.Length; i++)
                {
                    float amount = Mathf.Clamp01(displayedHealth * hearts.Length /
                        (float)playerHealth.MaximumHealth - i);
                    if (hearts[i] != null)
                        hearts[i].color = Color.Lerp(new Color(.22f, .2f, .29f, .9f),
                            Color.white, amount);
                }
            var state = CycleController.Instance;
            if (state == null) return;
            displayedFood = state.Run.Food;
            displayedLives = state.Cycle.LivesRemaining;
            displayedPosition = state.Run.Position;
            displayedInterlude = state.Run.InInterlude;
            if (foodLabel != null) foodLabel.text = $"× {displayedFood}";
            if (livesLabel != null) livesLabel.text = $"× {displayedLives}";
            if (mapLabel != null) mapLabel.text = displayedInterlude
                ? "ПЕРЕХІД" : $"МІСТО {displayedPosition + 1}/9";
            if (mapMarker != null && mapStops != null && mapStops.Length > 0)
                mapMarker.anchoredPosition = mapStops[Mathf.Clamp(displayedPosition, 0,
                    mapStops.Length - 1)].anchoredPosition;
        }
    }
}
