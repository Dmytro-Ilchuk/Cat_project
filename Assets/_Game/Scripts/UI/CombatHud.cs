using UnityEngine;
using UnityEngine.UI;

namespace NineLives
{
    public sealed class CombatHud : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private Text healthLabel;

        private int displayedHealth = -1;

        private void OnEnable()
        {
            if (playerHealth == null)
                playerHealth = FindAnyObjectByType<PlayerController>()?.GetComponent<Health>();
            Refresh();
        }

        private void Update()
        {
            if (playerHealth != null && displayedHealth != playerHealth.CurrentHealth)
                Refresh();
        }

        private void Refresh()
        {
            if (playerHealth == null || healthLabel == null) return;
            displayedHealth = playerHealth.CurrentHealth;
            var cycle = CycleController.Instance;
            healthLabel.text = cycle == null
                ? $"HP {playerHealth.CurrentHealth} / {playerHealth.MaximumHealth}"
                : $"HP {playerHealth.CurrentHealth} / {playerHealth.MaximumHealth}   Життя: {cycle.Cycle.LivesRemaining}   Спогади: {cycle.Cycle.Memories}";
        }
    }
}
