using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DefenseHud : MonoBehaviour
{
    [SerializeField] private DefenseDirector director;

    [Header("Labels")]
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text frontlineText;
    [SerializeField] private TMP_Text wallText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text resourcesText;
    [SerializeField] private TMP_Text upgradesText;
    [SerializeField] private TMP_Text costsText;

    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button repairButton;
    [SerializeField] private Button toggleModeButton;
    [SerializeField] private Button wallUpgradeButton;
    [SerializeField] private Button towerUpgradeButton;
    [SerializeField] private Button defenderUpgradeButton;

    private readonly StringBuilder builder = new StringBuilder(256);

    private void Awake()
    {
        if (director == null)
        {
            director = FindAnyObjectByType<DefenseDirector>();
        }

        WireButtons();
    }

    private void OnEnable()
    {
        if (director != null)
        {
            director.Changed += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (director != null)
        {
            director.Changed -= Refresh;
        }
    }

    private void Update()
    {
        if (director != null && director.Runtime.IsRunning)
        {
            Refresh();
        }
    }

    private void OnDestroy()
    {
        UnwireButtons();
    }

    public void StartFrontline()
    {
        if (director != null)
        {
            director.StartDefense();
        }
    }

    public void RepairWall()
    {
        if (director != null)
        {
            director.TryRepairWall();
        }
    }

    public void ToggleMode()
    {
        if (director != null)
        {
            director.ToggleMode();
        }
    }

    public void UpgradeWall()
    {
        if (director != null)
        {
            director.TryUpgradeWall();
        }
    }

    public void UpgradeTower()
    {
        if (director != null)
        {
            director.TryUpgradeTower();
        }
    }

    public void UpgradeDefender()
    {
        if (director != null)
        {
            director.TryUpgradeDefender();
        }
    }

    public void Refresh()
    {
        if (director == null)
        {
            SetText(stateText, "DefenseDirector not assigned");
            return;
        }

        DefenseRuntimeState runtime = director.Runtime;
        DefenseUpgradeModel upgrades = director.Upgrades;

        SetText(stateText, $"State: {runtime.State} / Mode: {runtime.Mode}");
        SetText(frontlineText, $"Frontline Lv.{runtime.FrontlineLevel}");
        SetText(wallText, $"Wall: {Mathf.CeilToInt(runtime.WallHealth)} / {Mathf.CeilToInt(runtime.WallMaxHealth)}");
        SetText(progressText, BuildProgressText(runtime));
        SetText(resourcesText, director.Wallet == null ? "Wallet: none" : director.Wallet.FormatAll());
        SetText(upgradesText, BuildUpgradeText(upgrades));
        SetText(costsText, BuildCostsText(upgrades));
        RefreshButtons(runtime, upgrades);
    }

    private string BuildProgressText(DefenseRuntimeState runtime)
    {
        builder.Clear();
        builder.Append("Progress: ");
        builder.Append(Mathf.RoundToInt(runtime.FrontlineProgressPercent * 100f));
        builder.Append("% / Pressure: ");
        builder.Append(Mathf.CeilToInt(runtime.EnemyPressure));
        builder.Append(" / ");
        builder.Append(Mathf.CeilToInt(runtime.EnemyPressureCapacity));
        return builder.ToString();
    }

    private string BuildUpgradeText(DefenseUpgradeModel upgrades)
    {
        if (upgrades == null)
        {
            return "Upgrades: none";
        }

        builder.Clear();
        builder.Append("Wall Lv.");
        builder.Append(upgrades.WallLevel);
        builder.Append(" / Tower Lv.");
        builder.Append(upgrades.TowerLevel);
        builder.Append(" / Defenders Lv.");
        builder.Append(upgrades.DefenderLevel);
        builder.Append("\nDPS ");
        builder.Append(upgrades.TotalDefensePower.ToString("0.0"));
        builder.Append(" / Wall HP ");
        builder.Append(Mathf.CeilToInt(upgrades.MaxWallHealth));
        return builder.ToString();
    }

    private string BuildCostsText(DefenseUpgradeModel upgrades)
    {
        if (upgrades == null)
        {
            return "Costs: none";
        }

        builder.Clear();
        builder.Append("Wall: ");
        AppendCost(upgrades.GetWallUpgradeCost());
        builder.Append("\nTower: ");
        AppendCost(upgrades.GetTowerUpgradeCost());
        builder.Append("\nDefenders: ");
        AppendCost(upgrades.GetDefenderUpgradeCost());
        return builder.ToString();
    }

    private void AppendCost(ResourceAmount[] costs)
    {
        if (costs == null || costs.Length == 0)
        {
            builder.Append("Free");
            return;
        }

        for (int i = 0; i < costs.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(costs[i].Resource);
            builder.Append(" ");
            builder.Append(costs[i].Amount);
        }
    }

    private void RefreshButtons(DefenseRuntimeState runtime, DefenseUpgradeModel upgrades)
    {
        bool hasDirector = director != null;
        bool hasWallet = hasDirector && director.Wallet != null;
        bool canUseUpgradeButtons = hasWallet && upgrades != null;

        SetInteractable(startButton, hasDirector && !runtime.IsRunning && runtime.WallHealth > 0f);
        SetInteractable(repairButton, canUseUpgradeButtons && runtime.WallHealth < runtime.WallMaxHealth
            && director.Wallet.CanSpend(upgrades.GetRepairCost(runtime.WallMaxHealth - runtime.WallHealth)));
        SetInteractable(toggleModeButton, hasDirector && runtime.State != DefenseState.Breached);
        SetInteractable(wallUpgradeButton, canUseUpgradeButtons && director.Wallet.CanSpend(upgrades.GetWallUpgradeCost()));
        SetInteractable(towerUpgradeButton, canUseUpgradeButtons && director.Wallet.CanSpend(upgrades.GetTowerUpgradeCost()));
        SetInteractable(defenderUpgradeButton, canUseUpgradeButtons && director.Wallet.CanSpend(upgrades.GetDefenderUpgradeCost()));
    }

    private void WireButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartFrontline);
        }

        if (repairButton != null)
        {
            repairButton.onClick.AddListener(RepairWall);
        }

        if (toggleModeButton != null)
        {
            toggleModeButton.onClick.AddListener(ToggleMode);
        }

        if (wallUpgradeButton != null)
        {
            wallUpgradeButton.onClick.AddListener(UpgradeWall);
        }

        if (towerUpgradeButton != null)
        {
            towerUpgradeButton.onClick.AddListener(UpgradeTower);
        }

        if (defenderUpgradeButton != null)
        {
            defenderUpgradeButton.onClick.AddListener(UpgradeDefender);
        }
    }

    private void UnwireButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartFrontline);
        }

        if (repairButton != null)
        {
            repairButton.onClick.RemoveListener(RepairWall);
        }

        if (toggleModeButton != null)
        {
            toggleModeButton.onClick.RemoveListener(ToggleMode);
        }

        if (wallUpgradeButton != null)
        {
            wallUpgradeButton.onClick.RemoveListener(UpgradeWall);
        }

        if (towerUpgradeButton != null)
        {
            towerUpgradeButton.onClick.RemoveListener(UpgradeTower);
        }

        if (defenderUpgradeButton != null)
        {
            defenderUpgradeButton.onClick.RemoveListener(UpgradeDefender);
        }
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private void SetInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
}
