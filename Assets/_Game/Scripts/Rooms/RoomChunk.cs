using UnityEngine;

namespace NineLives
{
    public enum ChunkRole { Entry, Encounter, Exit }

    [DisallowMultipleComponent]
    public sealed class RoomChunk : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private RoomBiome biome;
        [SerializeField] private ChunkRole role;
        [SerializeField, Min(1)] private int width = 10;
        [SerializeField] private Transform leftSeam;
        [SerializeField] private Transform rightSeam;
        [SerializeField] private Transform[] enemySpawns;

        public string StableId => stableId;
        public RoomBiome Biome => biome;
        public ChunkRole Role => role;
        public int Width => width;
        public Transform LeftSeam => leftSeam;
        public Transform RightSeam => rightSeam;
        public Transform[] EnemySpawns => enemySpawns;
    }
}
