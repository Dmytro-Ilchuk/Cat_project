using System;
using UnityEngine;

namespace NineLives
{
    [DisallowMultipleComponent]
    public sealed class CycleController : MonoBehaviour
    {
        public static CycleController Instance { get; private set; }

        public ProfileState Profile { get; private set; }
        public CycleState Cycle { get; private set; }
        public RunState Run { get; private set; }

        public event Action StateChanged;
        public event Action<RunEndReason> RunCompleted;

        public static CycleController Ensure()
        {
            if (Instance != null) return Instance;
            var host = new GameObject("Cycle Controller");
            return host.AddComponent<CycleController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Profile = new ProfileState();
            StartNewCat();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void StartNewCat()
        {
            Cycle = new CycleState();
            Run = new RunState();
            StateChanged?.Invoke();
        }

        public bool StartRun(int? seed = null)
        {
            if (Cycle.IsComplete || Run.IsActive) return false;
            Run.ClearTemporaryState();
            Run.IsActive = true;
            Run.Seed = seed ?? Environment.TickCount;
            StateChanged?.Invoke();
            return true;
        }

        public bool CompleteRun(RunEndReason reason)
        {
            if (!Run.IsActive) return false;

            Run.ClearTemporaryState();
            if (reason == RunEndReason.Victory)
            {
                Cycle.IsVictory = true;
                Cycle.IsComplete = true;
            }
            else
            {
                Cycle.LivesRemaining = Mathf.Max(0, Cycle.LivesRemaining - 1);
                Cycle.IsComplete = Cycle.LivesRemaining == 0;
            }

            RunCompleted?.Invoke(reason);
            StateChanged?.Invoke();
            return true;
        }

        public void AddMemories(int amount)
        {
            if (amount <= 0 || Cycle.IsComplete) return;
            Cycle.Memories += amount;
            StateChanged?.Invoke();
        }
    }
}
