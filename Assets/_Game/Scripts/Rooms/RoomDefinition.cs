using UnityEngine;

namespace NineLives
{
    public enum RoomKind { Entrance, Combat, Trial, Merchant, Boss }
    public enum RoomBiome { CityStreet, Canals }

    [CreateAssetMenu(menuName = "NineLives/Room Definition")]
    public sealed class RoomDefinition : ScriptableObject
    {
        [SerializeField] private string stableId;
        [SerializeField] private RoomKind kind;
        [SerializeField] private RoomBiome biome;
        [SerializeField, Range(0, 5)] private int difficulty;
        [SerializeField] private RoomLayout prefab;

        public string StableId => stableId;
        public RoomKind Kind => kind;
        public RoomBiome Biome => biome;
        public int Difficulty => difficulty;
        public RoomLayout Prefab => prefab;
    }
}
