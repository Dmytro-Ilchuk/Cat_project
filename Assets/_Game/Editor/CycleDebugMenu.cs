using UnityEditor;
using UnityEngine;

namespace NineLives.Editor
{
    public static class CycleDebugMenu
    {
        [MenuItem("NineLives/Stage 4/Deal lethal damage")]
        private static void DealLethalDamage()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            player?.GetComponent<Health>()?.TakeDamage(999, Vector2.zero);
        }

        [MenuItem("NineLives/Stage 4/Add 10 memories")]
        private static void AddMemories() => CycleController.Ensure().AddMemories(10);

        [MenuItem("NineLives/Stage 4/Complete run as victory")]
        private static void CompleteRun() => CycleController.Ensure().CompleteRun(RunEndReason.Victory);
    }
}
