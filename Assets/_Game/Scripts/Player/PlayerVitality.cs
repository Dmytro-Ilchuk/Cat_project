using UnityEngine;

namespace NineLives
{
    [RequireComponent(typeof(Health), typeof(PlayerController))]
    public sealed class PlayerVitality : MonoBehaviour
    {
        private Health health;
        private PlayerController controller;

        private void Awake()
        {
            health = GetComponent<Health>();
            controller = GetComponent<PlayerController>();
            health.Died += HandleDeath;
        }

        private void OnDestroy() => health.Died -= HandleDeath;

        private void LateUpdate()
        {
            health.SetExternalInvulnerability(controller.IsDashing);
        }

        private void HandleDeath(Health _)
        {
            controller.SetInputBlocked(true);
            CycleController.Instance?.CompleteRun(RunEndReason.Death);
        }
    }
}
