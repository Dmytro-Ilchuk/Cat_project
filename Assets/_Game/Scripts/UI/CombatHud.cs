using UnityEngine;
using UnityEngine.UI;

namespace NineLives
{
    public sealed class CombatHud : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private Text healthLabel;

        private int displayedHealth = -1;
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
                : $"HP {playerHealth.CurrentHealth} / {playerHealth.MaximumHealth}   Життя: {cycle.Cycle.LivesRemaining}\nСпогади: {cycle.Cycle.Memories}";
        }
    }
}
