using System;
using UnityEngine;

namespace NineLives
{
    [DisallowMultipleComponent]
    public sealed class CycleController : MonoBehaviour
    {
        public const string ResilienceUpgradeId = "resilience-1";
        public const int ResilienceCost = 2;
        public static CycleController Instance { get; private set; }
        // shortcut: the saved index assumes this route order; migrate the key when reordering rooms.
        public const string CheckpointKey = "Campaign.Checkpoint.v1";
        public bool HasCheckpoint => PlayerPrefs.HasKey(CheckpointKey);
        public int SavedPosition => Mathf.Clamp(PlayerPrefs.GetInt(CheckpointKey, 0), 0, RunRoute.RoomCount - 1);

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

        public bool StartRun(int? seed = null, bool continueFromSave = false)
        {
            if (Run.IsActive) return false;
            Cycle.IsComplete = false;
            Cycle.IsVictory = false;
            Run.ClearTemporaryState();
            Run.Position = continueFromSave ? SavedPosition : 0;
            Run.IsActive = true;
            Run.Seed = seed ?? Environment.TickCount;
            StateChanged?.Invoke();
            return true;
        }

        public void SaveCheckpoint(int position)
        {
            if (position < 0 || position >= RunRoute.RoomCount)
                throw new ArgumentOutOfRangeException(nameof(position));
            PlayerPrefs.SetInt(CheckpointKey, position);
            PlayerPrefs.Save();
            StateChanged?.Invoke();
        }

        public bool CompleteRun(RunEndReason reason)
        {
            if (!Run.IsActive) return false;

            Run.IsActive = false;
            if (reason == RunEndReason.Victory)
            {
                Cycle.IsVictory = true;
                Cycle.IsComplete = true;
                PlayerPrefs.DeleteKey(CheckpointKey);
                PlayerPrefs.Save();
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

        public bool HasResilience => Cycle.PurchasedUpgradeIds.Contains(ResilienceUpgradeId);

        public bool TryBuyResilience()
        {
            if (Cycle.IsComplete || HasResilience || Cycle.Memories < ResilienceCost) return false;
            Cycle.Memories -= ResilienceCost;
            Cycle.PurchasedUpgradeIds.Add(ResilienceUpgradeId);
            StateChanged?.Invoke();
            return true;
        }

        public void AddFood(int amount)
        {
            if (!Run.IsActive || amount <= 0) return;
            Run.Food += amount;
            StateChanged?.Invoke();
        }

        public bool TrySpendFood(int amount)
        {
            if (!Run.TrySpendFood(amount)) return false;
            StateChanged?.Invoke();
            return true;
        }

        public bool ContinueToNextLocation(int? seed = null)
        {
            if (!Run.ContinueToNextLocation(seed ?? Environment.TickCount)) return false;
            StateChanged?.Invoke();
            return true;
        }
    }
}
