using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NineLives
{
    // First playable city stage: combat sectors, merchant stops and a between-location room.
    public sealed class RunDirector : MonoBehaviour
    {
        [SerializeField] private RoomDefinition[] rooms;
        [SerializeField] private RoomChunk[] chunks;
        [SerializeField] private PlayerController player;
        [SerializeField] private RoomCamera roomCamera;
        [SerializeField] private RatController ratPrefab;
        [SerializeField] private GameObject merchantPrefab;
        [SerializeField] private Text status;
        [SerializeField] private GameObject decisionBackdrop;
        [SerializeField] private Sprite exitSprite;
        [SerializeField] private Button trialButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;

        private RunState run;
        private RouteStep[] route;
        private RoomLayout active;
        private bool trialStarted;
        private bool exitOpen;
        private bool shopVisible;
        private SpriteRenderer exitSign;
        private Transform activeMerchant;

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
            ApplyHealthBonuses(true);
            Load();
        }

        private void Update()
        {
            if (active == null || !run.IsActive) return;
            var keyboard = Keyboard.current;
            if (run.InInterlude)
                return;
            if (!exitOpen && (route[run.Position].Room.Kind == RoomKind.Combat ||
                              route[run.Position].Room.Kind == RoomKind.Boss || trialStarted))
            {
                foreach (var rat in active.GetComponentsInChildren<RatController>())
                {
                    if (!active.ArenaBounds.Contains(rat.transform.position))
                    {
                        var health = rat.GetComponent<Health>();
                        health.TakeDamage(health.MaximumHealth, Vector2.zero);
                    }
                    if (rat.GetComponent<Health>().IsAlive) return;
                }
                exitOpen = true;
                Refresh();
            }
            bool canShop = CanShopHere();
            if (canShop != shopVisible) { shopVisible = canShop; Refresh(); }
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame && exitOpen &&
                Vector2.Distance(player.transform.position, active.Exit.position) < 2.5f)
            {
                if (run.Position == 4) Refresh(); // Choose one of the two branch buttons.
                else Advance();
            }
        }

        public void ActivateTrial()
        {
            if (run == null || !run.IsActive || run.Position != 4 || trialStarted || run.InInterlude) return;
            trialStarted = true;
            exitOpen = false;
            SpawnEnemies();
            Refresh();
        }

        public void ChooseNormal()
        {
            if (run.InInterlude) BuyResilience();
            else if (CanShopHere()) BuyHealing();
            else ChooseBranch(0);
        }

        public void ChooseHard()
        {
            if (run.InInterlude) RepeatCityPrototype();
            else if (CanShopHere()) BuyStamina();
            else ChooseBranch(1);
        }

        private void ChooseBranch(int choice)
        {
            if (run == null || !run.IsActive || run.Position != 4 || !exitOpen || run.Branch != -1) return;
            run.Branch = choice;
            Advance();
        }

        private void Advance()
        {
            if (!exitOpen || run.InInterlude) return;
            string key = run.Position + ":" + route[run.Position].LayoutId;
            if (!run.CompletedRooms.Contains(key))
            {
                run.CompletedRooms.Add(key);
                if (route[run.Position].Room.Kind == RoomKind.Combat || (run.Position == 4 && trialStarted))
                    CycleController.Instance.AddMemories(1);
                else if (route[run.Position].Room.Kind == RoomKind.Boss)
                    CycleController.Instance.AddMemories(2);
            }
            if (run.Position == route.Length - 1)
            {
                EnterInterlude();
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
            PresentRoom(loaded);
            if (run.Position == 5 || run.Position == 7) PlaceMerchant();
            trialStarted = false;
            shopVisible = false;
            exitOpen = step.Room.Kind == RoomKind.Entrance || step.Room.Kind == RoomKind.Trial ||
                       step.Room.Kind == RoomKind.Merchant;
            if (step.Room.Kind == RoomKind.Combat || step.Room.Kind == RoomKind.Boss) SpawnEnemies();
            Refresh();
        }

        private void PresentRoom(RoomLayout loaded)
        {
            if (active != null) { active.gameObject.SetActive(false); Destroy(active.gameObject); }
            active = loaded;
            activeMerchant = null;
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
        }

        private bool CanShopHere() => exitOpen && !run.InInterlude &&
            activeMerchant != null && Vector2.Distance(player.transform.position, activeMerchant.position) < 3.2f;

        private void PlaceMerchant()
        {
            if (merchantPrefab == null)
                throw new System.InvalidOperationException("Run needs a merchant prefab.");
            var merchant = Instantiate(merchantPrefab, active.transform);
            merchant.name = "Merchant Cat";
            merchant.transform.position = new Vector3(active.Exit.position.x - 3f, .02f, 0f);
            activeMerchant = merchant.transform;
        }

        private void BuyHealing()
        {
            string purchase = "merchant-heal:" + run.Position;
            var health = player.GetComponent<Health>();
            if (run.BoonIds.Contains(purchase) || health.CurrentHealth >= health.MaximumHealth ||
                !CycleController.Instance.TrySpendFood(20)) return;
            run.BoonIds.Add(purchase);
            health.Heal(20);
            Refresh();
        }

        private void BuyStamina()
        {
            string purchase = "merchant-stamina:" + run.Position;
            if (run.BoonIds.Contains(purchase) || !CycleController.Instance.TrySpendFood(30)) return;
            run.BoonIds.Add(purchase);
            ApplyHealthBonuses(true);
            Refresh();
        }

        private void ApplyHealthBonuses(bool healIncrease)
        {
            int bonus = CycleController.Instance.HasResilience ? 10 : 0;
            foreach (string purchase in run.BoonIds)
                if (purchase.StartsWith("merchant-stamina:", System.StringComparison.Ordinal)) bonus += 10;
            player.GetComponent<Health>().SetMaximumHealthBonus(bonus, healIncrease);
        }

        private void EnterInterlude()
        {
            var merchant = System.Array.Find(rooms, room => room != null && room.Kind == RoomKind.Merchant);
            if (merchant == null || merchant.Prefab == null)
                throw new System.InvalidOperationException("The between-locations room needs a safe room prefab.");
            run.InInterlude = true;
            PresentRoom(Instantiate(merchant.Prefab));
            exitOpen = true;
            shopVisible = false;
            Refresh();
        }

        private void BuyResilience()
        {
            if (!CycleController.Instance.TryBuyResilience()) return;
            ApplyHealthBonuses(true);
            Refresh();
        }

        private void RepeatCityPrototype()
        {
            if (!CycleController.Instance.ContinueToNextLocation()) return;
            Load();
        }

        private void SpawnEnemies()
        {
            if (ratPrefab == null) throw new System.InvalidOperationException("Run needs an enemy prefab.");
            bool riskyBranch = run.Position == 5 && run.Branch == 1;
            int totalFood = run.Position == 4 && trialStarted ? 15 : route[run.Position].FoodReward;
            int count = active.EnemySpawns.Length;
            for (int i = 0; i < active.EnemySpawns.Length; i++)
            {
                var rat = Instantiate(ratPrefab, active.EnemySpawns[i].position, Quaternion.identity, active.transform);
                int foodShare = count > 0 ? totalFood / count + (i < totalFood % count ? 1 : 0) : 0;
                rat.GetComponent<Health>().Died += dead => FoodDrop.Spawn(
                    dead.GetComponent<SpriteRenderer>().bounds.center, player, foodShare);
                if (riskyBranch || (active.EnemySpawns.Length > 1 && i % 2 == 1))
                    rat.SetAttackStyle(RatAttackStyle.Spit);
                if (riskyBranch) rat.GetComponent<Health>().SetMaximumHealthBonus(10, true);
            }
        }

        private void Refresh()
        {
            var cycle = CycleController.Instance;
            if (run.InInterlude)
            {
                if (status != null)
                    status.text = $"МІЖ ЛОКАЦІЯМИ  ·  Спогади {cycle.Cycle.Memories}\n" +
                        "Покращення лишається цьому коту між життями. Наступний район ще в розробці.";
                trialButton.gameObject.SetActive(false);
                normalButton.gameObject.SetActive(true);
                hardButton.gameObject.SetActive(true);
                SetButton(normalButton, "МІЦНІСТЬ I · +10 HP\n2 Спогади");
                SetButton(hardButton, "ПОВТОРИТИ МІСТО\nпрототип");
                normalButton.interactable = !cycle.HasResilience && cycle.Cycle.Memories >= CycleController.ResilienceCost;
                hardButton.interactable = true;
                UpdateDecisionLayout(true);
                return;
            }

            var step = route[run.Position];
            if (status != null)
                status.text = $"МІСТО · сектор {run.Position + 1}/{route.Length}  Корм {run.Food}  Спогади {cycle.Cycle.Memories}\n" +
                    (run.Position == 4 && exitOpen ? "Вибери небезпеку й нагороду наступного бою" :
                     shopVisible ? "Торговець: корм витрачається лише на покупки" :
                     step.Room.Kind == RoomKind.Merchant ? "Торговець чекає біля виходу" :
                     exitOpen ? "Вихід відкритий: E біля правого краю" :
                     $"Небезпека {step.DangerRank} · нагорода {step.FoodReward} корму");
            if (exitSign != null) exitSign.color = exitOpen ? new Color(.42f, .95f, .75f) : new Color(.95f, .32f, .36f);
            if (trialButton != null) trialButton.gameObject.SetActive(run.Position == 4 && !trialStarted);
            bool branchChoice = run.Position == 4 && exitOpen;
            normalButton.gameObject.SetActive(branchChoice || shopVisible);
            hardButton.gameObject.SetActive(branchChoice || shopVisible);
            if (branchChoice)
            {
                SetButton(normalButton, "ЗВИЧАЙНИЙ ШЛЯХ\n+20 корму");
                SetButton(hardButton, "РИЗИКОВИЙ ШЛЯХ\n+30 корму");
                normalButton.interactable = hardButton.interactable = true;
            }
            else if (shopVisible)
            {
                var health = player.GetComponent<Health>();
                SetButton(normalButton, "ЛІКУВАННЯ +20 HP\n20 корму");
                SetButton(hardButton, "+10 МАКС. HP\n30 корму");
                normalButton.interactable = run.Food >= 20 && health.CurrentHealth < health.MaximumHealth &&
                    !run.BoonIds.Contains("merchant-heal:" + run.Position);
                hardButton.interactable = run.Food >= 30 &&
                    !run.BoonIds.Contains("merchant-stamina:" + run.Position);
            }
            UpdateDecisionLayout(branchChoice || shopVisible || (trialButton != null && trialButton.gameObject.activeSelf));
        }

        private void UpdateDecisionLayout(bool visible)
        {
            if (decisionBackdrop != null) decisionBackdrop.SetActive(visible);
            if (status != null)
            {
                var rect = status.rectTransform;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, visible ? 204f : 92f);
            }
        }

        private static void SetButton(Button button, string caption) =>
            button.GetComponentInChildren<Text>().text = caption;
    }
}
