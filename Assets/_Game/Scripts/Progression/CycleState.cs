using System.Collections.Generic;

namespace NineLives
{
    public sealed class CycleState
    {
        public const int StartingLives = 9;

        public int LivesRemaining = StartingLives;
        public int Memories;
        public bool IsComplete;
        public bool IsVictory;
        public readonly List<string> PurchasedUpgradeIds = new List<string>();
    }
}
