using System;
using System.Collections.Generic;
using UnityEngine;

public enum ResearchCategory
{
    EquipmentAndConsumables,
    Progression,
    Mutations,
    Special

}

public enum ResearchBenefitType
{
    Recipe,
    CrewSlot,
    UnitSlot,
    MutationResearch,
    MutationExperience,
    Injections,
    Surgeries,
    MutationStorage,
    EquipmentUpgrades,
    CraftingDiscount,
    ResearchSpeed,
    ResearchTier,
    ResourceCraftTier,
    EquipmentCraftTier

}

[Serializable]
public sealed class ResearchCost
{
    public string resourceId;
    [Min(1)] public int amount = 1;

}

[Serializable]
public sealed class ResearchBenefit
{
    public ResearchBenefitType type;
    [Tooltip("Recipe ID for Recipe unlocks; ignored for other benefits.")]
    public string recipeId;
    [Tooltip("Equipment/consumable slot for UnitSlot unlocks.")]
    public UnitSlotId slot;
    [Tooltip("Amount for discounts, speed, experience and tiers. Use 1 for crew slots.")]
    public int amount = 1;

}

[Serializable]
public sealed class ResearchNode
{
    public string id;
    public string displayName;
    [TextArea(2, 5)] public string description;
    public ResearchCategory category;
    [Min(0)] public int requiredTier;
    public List<string> prerequisiteIds = new List<string>();
    public List<ResearchCost> costs = new List<ResearchCost>();
    [Min(1)] public int battlesRequired = 1;
    public List<ResearchBenefit> benefits = new List<ResearchBenefit>();

}

[CreateAssetMenu(fileName = "MutaBornResearchDatabase", menuName = "MutaBorn/Research Database")]
public sealed class ResearchDatabase : ScriptableObject
{
    [SerializeField] private List<ResearchNode> nodes = new List<ResearchNode>();

    public IReadOnlyList<ResearchNode> Nodes => nodes;

    public ResearchNode Find(string id)
    {
        foreach (ResearchNode node in nodes)
        {
            if (node != null && string.Equals(node.id, id, StringComparison.Ordinal))
            {

                return node;

            }

        }

        return null;

    }

#if UNITY_EDITOR
    public List<ResearchNode> EditorNodes => nodes;
#endif

    private void OnValidate()
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (ResearchNode node in nodes)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.id) || !ids.Add(node.id))
            {
                Debug.LogWarning("Research nodes need unique, non-empty stable IDs.", this);

            }

        }

    }

}
