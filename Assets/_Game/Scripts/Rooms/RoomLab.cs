using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NineLives
{
    // Isolated playtest scene for traversal and combat spacing; not the run generator.
    public sealed class RoomLab : MonoBehaviour
    {
        [SerializeField] private RoomDefinition[] rooms;
        [SerializeField] private PlayerController player;
        [SerializeField] private RoomCamera roomCamera;
        [SerializeField] private RatController ratPrefab;
        [SerializeField] private Text status;
        [SerializeField] private RoomChunk[] chunks;

        private RoomLayout active;
        private int index;
        private bool enemies;
        private bool chunkMode;
        private int seed = 1;

        private void Start()
        {
            Time.timeScale = 1f;
            InputSystem.actions.FindActionMap("Gameplay", true).Enable();
            Load(0);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || rooms == null || rooms.Length == 0) return;
            if (keyboard.nKey.wasPressedThisFrame) Load((index + 1) % rooms.Length);
            if (keyboard.bKey.wasPressedThisFrame) Load((index + rooms.Length - 1) % rooms.Length);
            if (keyboard.rKey.wasPressedThisFrame) Load(index);
            if (keyboard.tKey.wasPressedThisFrame) { enemies = !enemies; Load(index); }
            if (keyboard.cKey.wasPressedThisFrame) { chunkMode = !chunkMode; Load(index); }
            if (keyboard.vKey.wasPressedThisFrame) { seed++; Load(index); }
        }

        private void Load(int next)
        {
            if (rooms == null || rooms.Length == 0 || rooms[next] == null || rooms[next].Prefab == null) return;
            bool assembled = chunkMode && rooms[next].Kind == RoomKind.Combat && chunks != null && chunks.Length > 0;
            RoomLayout loaded;
            try { loaded = assembled ? ChunkRoomBuilder.Build(rooms[next], seed, chunks) : Instantiate(rooms[next].Prefab); }
            catch (System.Exception error)
            {
                Debug.LogError("Chunk room failed, using authored fallback: " + error.Message);
                loaded = Instantiate(rooms[next].Prefab);
            }
            if (active != null) { active.gameObject.SetActive(false); Destroy(active.gameObject); }
            index = next;
            active = loaded;
            roomCamera.SetBounds(active.CameraBounds);
            roomCamera.GetComponent<RoomParallax>()?.SetRoom(active);
            roomCamera.GetComponent<RoomEnvironmentArt>()?.DressRoom(active);
            player.GetComponent<Health>().ResetToFull();
            player.SetInputBlocked(false);
            player.SetSpawn(active.SafeSpawn.position);
            Physics2D.SyncTransforms();
            if (enemies && ratPrefab != null)
                for (int i = 0; i < active.EnemySpawns.Length; i++)
                {
                    var rat = Instantiate(ratPrefab, active.EnemySpawns[i].position, Quaternion.identity, active.transform);
                    if (active.EnemySpawns.Length > 1 && i % 2 == 1) rat.SetAttackStyle(RatAttackStyle.Spit);
                }
            if (status != null)
                status.text = $"{rooms[index].StableId}  {rooms[index].Biome}  {index + 1}/{rooms.Length}  Seed {seed}\nN/B: кімната   R: знову   T: вороги {(enemies ? "ON" : "OFF")}   C: чанки {(chunkMode ? "ON" : "OFF")}   V: інший seed";
        }
    }
}
