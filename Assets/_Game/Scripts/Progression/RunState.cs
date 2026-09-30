using System.Collections.Generic;

namespace NineLives
{
    public enum RunEndReason { Death, Abandoned, Victory }

    public sealed class RunState
    {
        public bool IsActive;
        public int Seed;
        public int Position;
        public int Branch = -1;
        public int Food;
        public bool InInterlude;
        public readonly List<string> BoonIds = new List<string>();
        public readonly List<string> CompletedRooms = new List<string>();

        public bool TrySpendFood(int amount)
        {
            if (!IsActive || amount <= 0 || Food < amount) return false;
            Food -= amount;
            return true;
        }

        public bool ContinueToNextLocation(int seed)
        {
            if (!IsActive || !InInterlude) return false;
            Seed = seed;
            Position = 0;
            Branch = -1;
            InInterlude = false;
            CompletedRooms.Clear();
            return true;
        }

        public void ClearTemporaryState()
        {
            IsActive = false;
            Seed = 0;
            Position = 0;
            Branch = -1;
            Food = 0;
            InInterlude = false;
            BoonIds.Clear();
            CompletedRooms.Clear();
        }
    }
}
