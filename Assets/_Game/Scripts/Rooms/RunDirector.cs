using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NineLives
{
    // First playable route slice. Rewards, shop and boss behaviour get their own later pass.
    public sealed class RunDirector : MonoBehaviour
    {
        [SerializeField] private RoomDefinition[] rooms;
        [SerializeField] private RoomChunk[] chunks;
        [SerializeField] private PlayerController player;
        [SerializeField] private RoomCamera roomCamera;
        [SerializeField] private RatController ratPrefab;
        [SerializeField] private Text status;
        [SerializeField] private Sprite exitSprite;
        [SerializeField] private Button trialButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;

        private RunState run;
        private RouteStep[] route;
        private RoomLayout active;
        private bool trialStarted;
        private bool exitOpen;
        private bool finished;
        private SpriteRenderer exitSign;

        private void Start()
        {
            Time.timeScale = 1f;
            InputSystem.actions.FindActionMap("Gameplay", true).Enable();
            if (trialButton != null) trialButton.onClick.AddListener(ActivateTrial);
            if (normalButton != null) normalButton.onClick.AddListener(ChooseNormal);
            if (hardButton != null) hardButton.onClick.AddListener(ChooseHard);
            var cycle = CycleController.Ensure();
            if (!cycle.Run.IsActive) cycle.StartRun();
            run = cycle.Run;
            Load();
        }

        private void Update()
        {
            if (finished || active == null || !run.IsActive) return;
            if (!exitOpen && (route[run.Position].Room.Kind == RoomKind.Combat ||
                              route[run.Position].Room.Kind == RoomKind.Boss || trialStarted))
            {
                foreach (var rat in active.GetComponentsInChildren<RatController>())
                {
                    if (!active.ArenaBounds.Contains(rat.transform.position)) Destroy(rat.gameObject);
                    else if (rat.GetComponent<Health>().IsAlive) return;
                }
                exitOpen = true;
                Refresh();
            }
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame && exitOpen &&
                Vector2.Distance(player.transform.position, active.Exit.position) < 2.5f)
            {
                if (run.Position == 4) Refresh(); // Choose one of the two branch buttons.
                else Advance();
            }
        }

        public void ActivateTrial()
        {
            if (run == null || !run.IsActive || run.Position != 4 || trialStarted || finished) return;
            trialStarted = true;
            exitOpen = false;
            SpawnEnemies();
            Refresh();
        }

        public void ChooseNormal() => ChooseBranch(0);
        public void ChooseHard() => ChooseBranch(1);

        private void ChooseBranch(int choice)
        {
            if (run == null || !run.IsActive || run.Position != 4 || !exitOpen || run.Branch != -1) return;
            run.Branch = choice;
            Advance();
        }

        private void Advance()
        {
            if (!exitOpen) return;
            string key = run.Position + ":" + route[run.Position].LayoutId;
            if (!run.CompletedRooms.Contains(key))
            {
                run.CompletedRooms.Add(key);
                if (route[run.Position].Room.Kind == RoomKind.Combat || (run.Position == 4 && trialStarted))
                    CycleController.Instance.AddMemories(1);
                if (run.Position == 2) run.Food += 20;
                if (run.Position == 5) run.Food += run.Branch == 0 ? 20 : 30;
            }
            if (run.Position == 8)
            {
                finished = true;
                player.SetInputBlocked(true);
                CycleController.Instance.CompleteRun(RunEndReason.Victory);
                if (status != null) status.text = "МАРШРУТ ЗАВЕРШЕНО — перемога (тестовий бос)";
                return;
            }
            run.Position++;
            Load();
        }

        private void Load()
        {
            if (run.Position == 5 && run.Branch < 0) throw new System.InvalidOperationException("Choose a branch first.");
            route = RunRoute.Build(run.Seed, Mathf.Max(0, run.Branch), rooms, chunks);
            var step = route[run.Position];
            var loaded = step.Room.Kind == RoomKind.Combat
                ? ChunkRoomBuilder.Build(step.Room, step.ChunkSeed, chunks)
                : Instantiate(step.Room.Prefab);
            if (active != null) { active.gameObject.SetActive(false); Destroy(active.gameObject); }
            active = loaded;
            var sign = new GameObject("Exit light");
            sign.transform.SetParent(active.Exit, false);
            sign.transform.localScale = new Vector3(.25f, 2.5f, 1);
            exitSign = sign.AddComponent<SpriteRenderer>();
            exitSign.sprite = exitSprite;
            exitSign.sortingOrder = 12;
            roomCamera.SetBounds(active.CameraBounds);
            roomCamera.GetComponent<RoomParallax>()?.SetRoom(active);
            roomCamera.GetComponent<RoomAtmosphere>()?.SetRoom(active);
            roomCamera.GetComponent<RoomEnvironmentArt>()?.DressRoom(active);
            player.SetSpawn(active.SafeSpawn.position);
            Physics2D.SyncTransforms();
            trialStarted = false;
            exitOpen = step.Room.Kind == RoomKind.Entrance || step.Room.Kind == RoomKind.Trial ||
                       step.Room.Kind == RoomKind.Merchant;
            if (step.Room.Kind == RoomKind.Combat || step.Room.Kind == RoomKind.Boss) SpawnEnemies();
            if (step.Room.Kind == RoomKind.Merchant && !run.CompletedRooms.Contains("merchant_heal"))
            {
                player.GetComponent<Health>().Heal(10);
                run.CompletedRooms.Add("merchant_heal");
            }
            Refresh();
        }

        private void SpawnEnemies()
        {
            if (ratPrefab == null) throw new System.InvalidOperationException("Run needs an enemy prefab.");
            for (int i = 0; i < active.EnemySpawns.Length; i++)
            {
                var rat = Instantiate(ratPrefab, active.EnemySpawns[i].position, Quaternion.identity, active.transform);
                if (active.EnemySpawns.Length > 1 && i % 2 == 1) rat.SetAttackStyle(RatAttackStyle.Spit);
            }
        }

        private void Refresh()
        {
            if (status != null)
                status.text = $"{run.Position + 1}/9  {route[run.Position].Room.StableId}  Seed {run.Seed}  Корм {run.Food}\n" +
                    (run.Position == 4 && exitOpen ? "Оберіть один бій для наступної кімнати" :
                     exitOpen ? "Вихід відкритий: E біля правого краю" : "Переможіть ворогів, щоб відкрити вихід");
            if (exitSign != null) exitSign.color = exitOpen ? new Color(.42f, .95f, .75f) : new Color(.95f, .32f, .36f);
            if (trialButton != null) trialButton.gameObject.SetActive(run.Position == 4 && !trialStarted);
            if (normalButton != null) normalButton.gameObject.SetActive(run.Position == 4 && exitOpen);
            if (hardButton != null) hardButton.gameObject.SetActive(run.Position == 4 && exitOpen);
        }
    }
}
