using System.Collections.Generic;

namespace NineLives
{
    public enum RunEndReason { Death, Abandoned, Victory }

    public sealed class RunState
    {
        public bool IsActive;
        public int Food;
        public readonly List<string> BoonIds = new List<string>();

        public void ClearTemporaryState()
        {
            IsActive = false;
            Food = 0;
            BoonIds.Clear();
        }
    }
}
