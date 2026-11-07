using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NineLives
{
    public sealed class RunDirector : MonoBehaviour
    {
        [SerializeField] private RoomDefinition[] rooms;
        [SerializeField] private PlayerController player;
        [SerializeField] private RoomCamera roomCamera;
        [SerializeField] private RatController ratPrefab;
        [SerializeField] private Text status;
        [SerializeField] private Sprite exitSprite;

        private RunState run;
        private RoomDefinition[] route;
        private RoomLayout active;
        private bool exitOpen;
        private SpriteRenderer exitSign;
        private InputAction interact;

        private void Start()
        {
            Time.timeScale = 1f;
            InputSystem.actions.FindActionMap("Gameplay", true).Enable();
            interact = InputSystem.actions.FindAction("Gameplay/Interact", true);
            var cycle = CycleController.Ensure();
            if (!cycle.Run.IsActive) cycle.StartRun(continueFromSave: true);
            run = cycle.Run;
            route = RunRoute.Build(rooms);
            Load();
        }

        private void Update()
        {
            if (active == null || !run.IsActive || player.InputBlocked || Time.timeScale == 0f) return;
            if (!exitOpen)
            {
                bool enemiesAlive = false;
                foreach (var rat in active.GetComponentsInChildren<RatController>())
                {
                    var health = rat.GetComponent<Health>();
                    if (!active.ArenaBounds.Contains(rat.transform.position))
                        health.TakeDamage(health.MaximumHealth, Vector2.zero);
                    enemiesAlive |= health.IsAlive;
                }
                if (enemiesAlive) return;
                exitOpen = true;
                Refresh();
            }
            if (interact.WasPressedThisFrame() &&
                Vector2.Distance(player.transform.position, active.Exit.position) < 2.5f)
                Advance();
        }

        private void Advance()
        {
            if (!run.IsActive || !exitOpen) return;
            if (run.Position == route.Length - 1)
            {
                CycleController.Instance.CompleteRun(RunEndReason.Victory);
                return;
            }
            run.Position++;
            Load();
        }

        private void Load()
        {
            if (active != null) { active.gameObject.SetActive(false); Destroy(active.gameObject); }
            var room = route[run.Position];
            active = Instantiate(room.Prefab);
            var sign = new GameObject("Exit light");
            sign.transform.SetParent(active.Exit, false);
            sign.transform.localScale = new Vector3(.25f, 2.5f, 1f);
            exitSign = sign.AddComponent<SpriteRenderer>();
            exitSign.sprite = exitSprite;
            exitSign.sortingOrder = 12;
            roomCamera.SetBounds(active.CameraBounds);
            roomCamera.GetComponent<RoomParallax>()?.SetRoom(active);
            roomCamera.GetComponent<RoomAtmosphere>()?.SetRoom(active);
            roomCamera.GetComponent<RoomEnvironmentArt>()?.DressRoom(active);
            player.SetSpawn(active.SafeSpawn.position);
            Physics2D.SyncTransforms();

            exitOpen = room.Kind == RoomKind.Entrance;
            if (!exitOpen)
            {
                if (ratPrefab == null) throw new System.InvalidOperationException("Campaign needs an enemy prefab.");
                for (int i = 0; i < active.EnemySpawns.Length; i++)
                {
                    var rat = Instantiate(ratPrefab, active.EnemySpawns[i].position, Quaternion.identity, active.transform);
                    if (i % 2 == 1) rat.SetAttackStyle(RatAttackStyle.Spit);
                }
            }
            CycleController.Instance.SaveCheckpoint(run.Position);
            Refresh();
        }

        private void Refresh()
        {
            if (status != null)
                status.text = $"МІСТО · ділянка {run.Position + 1}/{route.Length}\n" +
                    (exitOpen ? run.Position == route.Length - 1
                        ? "Шлях додому відкрито: E біля зеленого виходу"
                        : "Далі: E біля зеленого виходу праворуч"
                        : "Переможи ворогів, щоб відкрити вихід");
            exitSign.color = exitOpen ? new Color(.42f, .95f, .75f) : new Color(.95f, .32f, .36f);
        }
    }
}
