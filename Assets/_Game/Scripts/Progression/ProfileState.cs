using System.Collections.Generic;

namespace NineLives
{
    public sealed class ProfileState
    {
        public readonly List<string> UnlockedItemIds = new List<string>();
        public readonly List<string> Records = new List<string>();
        public readonly List<string> Cosmetics = new List<string>();
    }
}
