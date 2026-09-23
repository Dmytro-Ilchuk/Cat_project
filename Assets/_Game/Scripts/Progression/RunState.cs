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
        public readonly List<string> BoonIds = new List<string>();
        public readonly List<string> CompletedRooms = new List<string>();

        public void ClearTemporaryState()
        {
            IsActive = false;
            Seed = 0;
            Position = 0;
            Branch = -1;
            Food = 0;
            BoonIds.Clear();
            CompletedRooms.Clear();
        }
    }
}
