using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// One campaign-wide owner of research progress. Resources stay in ResourceStockpile.
[DisallowMultipleComponent]
public sealed class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance { get; private set; }

    [SerializeField] private ResearchDatabase database;
    [SerializeField] private List<string> completedIds = new List<string>();
    [SerializeField] private string activeResearchId;
    [SerializeField, Min(0)] private int remainingBattles;
    [SerializeField] private List<string> pendingCompletionIds = new List<string>();

    public ResearchDatabase Database => database;
    public string ActiveResearchId => activeResearchId;
    public int RemainingBattles => remainingBattles;
    public bool HasPendingNotification => pendingCompletionIds.Count > 0;
    public event Action StateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);

            return;

        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (database == null)
        {
            database = Resources.Load<ResearchDatabase>("MutaBornResearchDatabase");

        }

        SceneManager.sceneLoaded += OnSceneLoaded;

    }

    private void Start()
    {
        SynchronizeUnlocks();

    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
        {
            Instance = null;

        }

    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SynchronizeUnlocks();

    }

    public bool IsComplete(string id)
    {

        return !string.IsNullOrWhiteSpace(id) && completedIds.Contains(id);

    }

    public bool HasBenefit(ResearchBenefitType type)
    {

        return BenefitTotal(type) > 0;

    }

    public int BenefitTotal(ResearchBenefitType type)
    {
        int total = 0;
        if (database == null)
        {

            return total;

        }

        foreach (ResearchNode node in database.Nodes)
        {
            if (node == null || !IsComplete(node.id) || node.benefits == null)
            {

                continue;

            }

            foreach (ResearchBenefit benefit in node.benefits)
            {
                if (benefit != null && benefit.type == type)
                {
                    total += Mathf.Max(0, benefit.amount);

                }

            }

        }

        return total;

    }

    public bool IsRecipeAvailable(string recipeId)
    {
        if (database == null)
        {

            return true;

        }

        foreach (ResearchNode node in database.Nodes)
        {
            if (node == null || node.benefits == null)
            {

                continue;

            }

            foreach (ResearchBenefit benefit in node.benefits)
            {
                if (benefit != null && benefit.type == ResearchBenefitType.Recipe &&
                    string.Equals(benefit.recipeId, recipeId, StringComparison.Ordinal) &&
                    !IsComplete(node.id))
                {

                    return false;

                }

            }

        }

        return true;

    }

    public int DiscountedCraftingCost(int baseCost)
    {
        int discount = Mathf.Clamp(BenefitTotal(ResearchBenefitType.CraftingDiscount), 0, 95);

        return Mathf.Max(1, Mathf.CeilToInt(baseCost * (100 - discount) / 100f));

    }

    public bool CanStart(string id, out string reason)
    {
        reason = string.Empty;
        ResearchNode node = database != null ? database.Find(id) : null;
        if (node == null)
        {
            reason = "Research project not found.";

            return false;

        }

        if (IsComplete(id))
        {
            reason = "Research already completed.";

            return false;

        }

        if (!string.IsNullOrEmpty(activeResearchId))
        {
            reason = "Finish the active research project first.";

            return false;

        }

        if (BenefitTotal(ResearchBenefitType.ResearchTier) < node.requiredTier)
        {
            reason = $"Research tier {node.requiredTier} required.";

            return false;

        }

        foreach (string prerequisite in node.prerequisiteIds)
        {
            if (!IsComplete(prerequisite))
            {
                ResearchNode required = database.Find(prerequisite);
                reason = "Complete " + (required != null ? required.displayName : prerequisite) + " first.";

                return false;

            }

        }

        ResourceStockpile stockpile = ResourceStockpile.Instance;
        if (stockpile == null)
        {
            reason = "Resource stockpile is not available.";

            return false;

        }

        Dictionary<string, int> totals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (ResearchCost cost in node.costs)
        {
            if (cost == null || cost.amount <= 0 || string.IsNullOrWhiteSpace(cost.resourceId))
            {
                reason = "Research cost is invalid.";

                return false;

            }

            totals.TryGetValue(cost.resourceId, out int previous);
            totals[cost.resourceId] = previous + cost.amount;

        }

        foreach (KeyValuePair<string, int> cost in totals)
        {
            if (stockpile.GetResourceData(cost.Key) == null ||
                stockpile.GetQuantity(cost.Key) < cost.Value)
            {
                ResourceData resource = stockpile.GetResourceData(cost.Key);
                reason = "Requires " + cost.Value + " " +
                    (resource != null ? resource.ResourceName : cost.Key) + ".";

                return false;

            }

        }

        return true;

    }

    public bool TryStart(string id, out string reason)
    {
        if (!CanStart(id, out reason))
        {

            return false;

        }

        ResearchNode node = database.Find(id);
        ResourceStockpile stockpile = ResourceStockpile.Instance;
        Dictionary<string, int> totals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (ResearchCost cost in node.costs)
        {
            totals.TryGetValue(cost.resourceId, out int previous);
            totals[cost.resourceId] = previous + cost.amount;

        }

        List<KeyValuePair<string, int>> paid = new List<KeyValuePair<string, int>>();
        foreach (KeyValuePair<string, int> cost in totals)
        {
            if (!stockpile.RemoveResource(cost.Key, cost.Value))
            {
                foreach (KeyValuePair<string, int> refund in paid)
                {
                    stockpile.AddResource(refund.Key, refund.Value);

                }

                reason = "Resources changed before research could start.";

                return false;

            }

            paid.Add(cost);

        }

        activeResearchId = id;
        remainingBattles = Mathf.Max(1, node.battlesRequired -
            BenefitTotal(ResearchBenefitType.ResearchSpeed));
        reason = "Research started: " + node.displayName;
        StateChanged?.Invoke();

        return true;

    }

    // Called after spoils are successfully claimed from a victorious mission.
    public void RecordClaimedVictory()
    {
        if (string.IsNullOrWhiteSpace(activeResearchId))
        {

            return;

        }

        remainingBattles = Mathf.Max(0, remainingBattles - 1);
        if (remainingBattles == 0)
        {
            string id = activeResearchId;
            activeResearchId = string.Empty;
            if (!IsComplete(id))
            {
                completedIds.Add(id);
                pendingCompletionIds.Add(id);
                SynchronizeUnlocks();

            }

        }

        StateChanged?.Invoke();

    }

    public string CurrentNotificationTitle()
    {
        if (pendingCompletionIds.Count == 0)
        {

            return string.Empty;

        }

        ResearchNode node = database != null ? database.Find(pendingCompletionIds[0]) : null;

        return node != null ? node.displayName : pendingCompletionIds[0];

    }

    public void DismissNotification()
    {
        if (pendingCompletionIds.Count == 0)
        {

            return;

        }

        pendingCompletionIds.RemoveAt(0);
        StateChanged?.Invoke();

    }

    public void ApplyToUnit(UnitData unit)
    {
        if (unit == null || database == null)
        {

            return;

        }

        foreach (ResearchNode node in database.Nodes)
        {
            if (node == null || !IsComplete(node.id))
            {

                continue;

            }

            foreach (ResearchBenefit benefit in node.benefits)
            {
                if (benefit != null && benefit.type == ResearchBenefitType.UnitSlot)
                {
                    unit.TryUnlockProgressionSlot(benefit.slot);

                }

            }

        }

    }

    private void SynchronizeUnlocks()
    {
        UnitRoster roster = UnitRoster.Instance;
        if (roster != null)
        {
            int desired = Mathf.Min(UnitRoster.MaximumCrewSlots,
                UnitRoster.StartingCrewSlots + BenefitTotal(ResearchBenefitType.CrewSlot));
            while (roster.UnlockedCrewSlots < desired)
            {
                roster.TryUnlockNextCrewSlotFromResearch();

            }

            foreach (UnitData unit in roster.Units)
            {
                ApplyToUnit(unit);

            }

        }

        ItemStorage storage = ItemStorage.Instance;
        if (storage != null && HasBenefit(ResearchBenefitType.MutationStorage))
        {
            storage.SetMutationStorageUnlocked(true);

        }

    }

}
