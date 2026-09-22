using UnityEngine;

namespace NineLives
{
    [DisallowMultipleComponent]
    public sealed class RoomLayout : MonoBehaviour
    {
        [SerializeField] private Transform entrance;
        [SerializeField] private Transform exit;
        [SerializeField] private Transform safeSpawn;
        [SerializeField] private Transform[] enemySpawns;
        [SerializeField] private BoxCollider2D cameraBounds;
        [SerializeField] private BoxCollider2D arenaBounds;

        public Transform Entrance => entrance;
        public Transform Exit => exit;
        public Transform SafeSpawn => safeSpawn;
        public Transform[] EnemySpawns => enemySpawns;
        public Bounds CameraBounds => cameraBounds.bounds;
        public Bounds ArenaBounds => arenaBounds.bounds;

        public void Initialize(Transform entry, Transform departure, Transform spawn, Transform[] enemies,
            BoxCollider2D cameraArea, BoxCollider2D arenaArea)
        {
            entrance = entry;
            exit = departure;
            safeSpawn = spawn;
            enemySpawns = enemies;
            cameraBounds = cameraArea;
            arenaBounds = arenaArea;
        }
    }
}
