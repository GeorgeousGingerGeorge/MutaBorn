using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using MutaBorn.Gameplay;

[Serializable]
public struct EquipmentStatRanges
{
    public Vector2Int armor;
    public Vector2Int ward;
    public Vector2Int movementPenalty;
    public Vector2Int meleeDamage;
    public Vector2Int rangeDamage;
    public Vector2Int magicDamage;
    public Vector2Int accuracy;

}

public enum EquipmentAttachmentSocket
{
    Auto = 0,
    RightHand = 1,
    LeftHand = 2,
    Head = 3,
    Chest = 4,
    Gloves = 5,
    Pants = 6,
    Boots = 7,
    Accessory1 = 8,
    Accessory2 = 9,
    Accessory3 = 10

}

public enum EquipmentHandednessOverride
{
    Inherit = 0,
    OneHanded = 1,
    TwoHanded = 2

}

public enum EquipmentArmorWeightOverride
{
    Inherit = 0,
    Light = 1,
    Medium = 2,
    Heavy = 3

}

[Serializable]
public struct EquipmentStarDice
{
    [Min(0)] public int rollCount;
    [Min(1)] public int dieSides;
    [Min(1)] public int minimumSuccess;

    public int Roll()
    {
        int total = 0;
        int sides = Mathf.Max(1, dieSides);
        for (int i = 0; i < rollCount && total < 5; i++)
        {
            if (UnityEngine.Random.Range(1, sides + 1) >= minimumSuccess)
            {
                total++;

            }

        }

        return total;

    }

}

[Serializable]
public struct EquipmentWeightedStatGrowth
{
    public EquipmentFlatStat stat;
    [Min(0f)] public float growthWeight;
    [Min(0f)] public float zeroWeight;
    public Vector2Int amountRange;

}

public enum EquipmentScalingAttribute
{
    Strength, Dexterity, Vitality, Constitution, Perception, Intelligence

}

[Serializable]
public struct EquipmentScalingOpportunity
{
    [Range(1, 5)] public int star;
    [Range(0f, 1f)] public float successChance;
    public EquipmentScalingAttribute[] eligibleAttributes;

}

[Serializable]
public struct EquipmentUpgradeResourceCost
{
    [Range(1, 5)] public int star;
    public string resourceId;
    [Min(0)] public int amount;

}

[Serializable]
public struct EquipmentUpgradeCostAdjustment
{
    [Range(1, 5)] public int star;
    public string resourceId;
    public bool replaceAmount;
    public int amount;

}

[Serializable]
public sealed class EquipmentCraftingRecipe
{
    [Min(0)] public int requiredCraftTier;
    public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();

}

[Serializable]
public sealed class EquipmentTemplate
{
    [Header("Stable Template Identity")]
    [SerializeField] private string templateId;
    [SerializeField] private string displayName;
    [TextArea(2, 5)] [SerializeField] private string description;
    [FormerlySerializedAs("icon")]
    [SerializeField] private Sprite templateIcon;

    [SerializeField, HideInInspector] private string legacyItemId;
    [SerializeField, HideInInspector] private string legacyRecipeId;

    [Header("Equipment Rule Overrides")]
    [SerializeField] private EquipmentHandednessOverride handednessOverride;
    [SerializeField] private EquipmentArmorWeightOverride armorWeightOverride;

    [Header("Starting Stats")]
    [SerializeField] private EquipmentGenerationType generationType = EquipmentGenerationType.Fixed;
    [SerializeField] private EquipmentStatValues fixedStats;
    [SerializeField] private EquipmentStatRanges randomRanges;
    [SerializeField] private EquipmentAttributeScaling startingScaling;

    [Header("Embedded Crafting Recipe")]
    [SerializeField] private EquipmentCraftingRecipe craftingRecipe = new EquipmentCraftingRecipe();

    [Header("Star Generation (Loot Only)")]
    [SerializeField] private EquipmentStarDice startingStarRolls;

    [Header("Upgrade Stat Growth (Two Independent Rolls per Star)")]
    [SerializeField] private EquipmentWeightedStatGrowth[] statGrowth = Array.Empty<EquipmentWeightedStatGrowth>();

    [Header("Upgrade Bonus Overrides")]
    [SerializeField] private string[] excludedTypeBonusIds = Array.Empty<string>();
    [SerializeField] private EquipmentBonusOption[] addedUpgradeBonuses = Array.Empty<EquipmentBonusOption>();

    [Header("Scaling Upgrade Opportunities")]
    [SerializeField] private EquipmentScalingOpportunity[] scalingOpportunities = Array.Empty<EquipmentScalingOpportunity>();

    [Header("Upgrade Cost Adjustments")]
    [SerializeField] private EquipmentUpgradeCostAdjustment[] costAdjustments = Array.Empty<EquipmentUpgradeCostAdjustment>();

    [Header("Model and Attachment Overrides")]
    [SerializeField] private EquipmentAttachmentSocket socketOverride;
    [SerializeField] private GameObject modelOverride;
    [SerializeField] private bool overrideAttachment;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private Vector3 rotationOffset;
    [SerializeField] private Vector3 scale = Vector3.one;

    [Header("Color Overrides")]
    [SerializeField] private bool overrideColors;
    [SerializeField] private Color primaryColor = Color.white;
    [SerializeField] private Color secondaryColor = Color.white;
    [SerializeField] private Color accentColor = Color.white;

    [Header("Starting Grants")]
    [SerializeField] private ActionDefinition[] excludedTypeActions = Array.Empty<ActionDefinition>();
    [SerializeField] private ActionDefinition[] addedActions = Array.Empty<ActionDefinition>();
    [SerializeField] private EffectGrant[] addedEffects = Array.Empty<EffectGrant>();
    [SerializeField] private DamageResistance[] startingResistances = Array.Empty<DamageResistance>();
    [SerializeField] private DamageComponent[] startingBonusDamage = Array.Empty<DamageComponent>();
    [Tooltip("Unspecified uses the Equipment Type's default damage type.")]
    [SerializeField] private DamageType damageTypeOverride = DamageType.Unspecified;

    public string TemplateId => templateId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite TemplateIcon => templateIcon;
    public Sprite Icon => templateIcon;
    public string LegacyItemId => legacyItemId;
    public string LegacyRecipeId => legacyRecipeId;
    public EquipmentHandednessOverride HandednessOverride => handednessOverride;
    public EquipmentArmorWeightOverride ArmorWeightOverride => armorWeightOverride;
    public EquipmentGenerationType GenerationType => generationType;
    public EquipmentStatValues FixedStats => fixedStats;
    public EquipmentStatRanges RandomRanges => randomRanges;
    public EquipmentAttributeScaling StartingScaling => startingScaling;
    public ActionDefinition[] ExcludedTypeActions => excludedTypeActions;
    public ActionDefinition[] AddedActions => addedActions;
    public EffectGrant[] AddedEffects => addedEffects;
    public DamageResistance[] StartingResistances => startingResistances;
    public DamageComponent[] StartingBonusDamage => startingBonusDamage;
    public DamageType DamageTypeOverride => damageTypeOverride;
    public EquipmentCraftingRecipe CraftingRecipe => craftingRecipe;
    public EquipmentStarDice StartingStarRolls => startingStarRolls;
    public EquipmentWeightedStatGrowth[] StatGrowth => statGrowth;
    public string[] ExcludedTypeBonusIds => excludedTypeBonusIds;
    public EquipmentBonusOption[] AddedUpgradeBonuses => addedUpgradeBonuses;
    public EquipmentScalingOpportunity[] ScalingOpportunities => scalingOpportunities;
    public EquipmentUpgradeCostAdjustment[] CostAdjustments => costAdjustments;
    public EquipmentAttachmentSocket SocketOverride => socketOverride;
    public GameObject ModelOverride => modelOverride;
    public bool OverrideAttachment => overrideAttachment;
    public Vector3 PositionOffset => positionOffset;
    public Vector3 RotationOffset => rotationOffset;
    public Vector3 Scale => scale;
    public bool OverrideColors => overrideColors;
    public Color PrimaryColor => primaryColor;
    public Color SecondaryColor => secondaryColor;
    public Color AccentColor => accentColor;

}

[Serializable]
public struct EquipmentTemplateReference
{
    public EquipmentType equipmentType;
    public string templateId;

    public bool TryResolve(out EquipmentTemplate template)
    {
        template = null;

        return equipmentType != null && equipmentType.TryGetTemplate(templateId, out template);

    }

}

[CreateAssetMenu(fileName = "EquipmentType", menuName = "MutaBorn/Equipment/Equipment Type")]
public sealed class EquipmentType : ScriptableObject
{
    [Header("Stable Type Identity")]
    [SerializeField] private string typeId;
    [SerializeField] private string displayTypeLabel;
    [SerializeField] private Sprite typeIcon;

    [Header("Shared Equipment Rules")]
    [SerializeField] private EquipmentFitType equipmentFit;
    [Tooltip("Default for hand equipment. Individual templates can override it.")]
    [SerializeField] private bool requiresBothHands;
    [InspectorName("Default Armor Weight")]
    [SerializeField] private ArmorCategory armorCategory;
    [SerializeField] private WeaponCategory weaponCategory;
    [SerializeField] private WeaponSubtype weaponSubtype;
    [SerializeField, HideInInspector] private AccessoryCategory accessoryCategory;
    [SerializeField] private DamageType primaryDamageType = DamageType.Unspecified;

    [Header("Default Upgrades")]
    [SerializeField] private EquipmentBonusOption[] upgradeBonuses = Array.Empty<EquipmentBonusOption>();
    [SerializeField] private EquipmentUpgradeResourceCost[] upgradeCosts = Array.Empty<EquipmentUpgradeResourceCost>();

    [Header("Default Equipment Model and Socket Offsets")]
    [SerializeField] private EquipmentAttachmentSocket defaultSocket;
    [SerializeField] private GameObject modelPrefab;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private Vector3 rotationOffset;
    [SerializeField] private Vector3 scale = Vector3.one;

    [Header("Default Color Regions")]
    [SerializeField] private bool applyColors;
    [SerializeField] private Color primaryColor = Color.white;
    [SerializeField] private Color secondaryColor = Color.white;
    [SerializeField] private Color accentColor = Color.white;

    [Header("Shared Starting Grants")]
    [SerializeField] private ActionSet grantedActionSet;
    [SerializeField] private EffectCollection grantedEffectCollection;
    [SerializeField] private ActionDefinition[] startingActions = Array.Empty<ActionDefinition>();
    [SerializeField] private EffectGrant[] startingEffects = Array.Empty<EffectGrant>();

    [Header("Embedded Equipment Templates")]
    [SerializeField] private List<EquipmentTemplate> templates = new List<EquipmentTemplate>();

    public string TypeId => typeId;
    public string DisplayTypeLabel => displayTypeLabel;
    public Sprite TypeIcon => typeIcon;
    public EquipmentFitType EquipmentFit => equipmentFit;
    public bool RequiresBothHands => equipmentFit == EquipmentFitType.Hand && requiresBothHands;
    public ArmorCategory ArmorCategory => armorCategory;
    public WeaponCategory WeaponCategory => weaponCategory;
    public WeaponSubtype WeaponSubtype => weaponSubtype;
    public AccessoryCategory AccessoryCategory => accessoryCategory;
    public DamageType PrimaryDamageType => primaryDamageType;
    public ActionSet GrantedActionSet => grantedActionSet;
    public EffectCollection GrantedEffectCollection => grantedEffectCollection;
    public EffectGrant[] StartingEffects => startingEffects;
    public IReadOnlyList<EquipmentTemplate> Templates => templates;
    public EquipmentBonusOption[] UpgradeBonuses => upgradeBonuses;
    public EquipmentUpgradeResourceCost[] UpgradeCosts => upgradeCosts;
    public EquipmentAttachmentSocket DefaultSocket => defaultSocket;
    public GameObject ModelPrefab => modelPrefab;
    public Vector3 PositionOffset => positionOffset;
    public Vector3 RotationOffset => rotationOffset;
    public Vector3 Scale => scale;
    public bool ApplyColors => applyColors;
    public Color PrimaryColor => primaryColor;
    public Color SecondaryColor => secondaryColor;
    public Color AccentColor => accentColor;

    public bool GetRequiresBothHands(EquipmentTemplate template)
    {
        if (equipmentFit != EquipmentFitType.Hand)
        {
            return false;

        }

        if (template != null)
        {
            if (template.HandednessOverride == EquipmentHandednessOverride.OneHanded)
            {
                return false;

            }

            if (template.HandednessOverride == EquipmentHandednessOverride.TwoHanded)
            {
                return true;

            }

        }

        return RequiresBothHands;

    }

    public ArmorCategory GetArmorWeight(EquipmentTemplate template)
    {
        if (template == null || template.ArmorWeightOverride == EquipmentArmorWeightOverride.Inherit)
        {
            return armorCategory;

        }

        return (ArmorCategory)template.ArmorWeightOverride;

    }

    public bool TryGetTemplate(string id, out EquipmentTemplate template)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            foreach (EquipmentTemplate entry in templates)
            {
                if (entry != null && string.Equals(entry.TemplateId, id, StringComparison.Ordinal))
                {
                    template = entry;

                    return true;

                }

            }

        }

        template = null;

        return false;

    }

    public ActionDefinition[] GetStartingActions(EquipmentTemplate template)
    {
        List<ActionDefinition> actions = new List<ActionDefinition>();
        foreach (ActionDefinition action in startingActions)
        {
            if (action != null && Array.IndexOf(template.ExcludedTypeActions, action) < 0 && !actions.Contains(action))
            {
                actions.Add(action);

            }

        }

        foreach (ActionDefinition action in template.AddedActions)
        {
            if (action != null && !actions.Contains(action))
            {
                actions.Add(action);

            }

        }

        return actions.ToArray();

    }

    private void OnValidate()
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (EquipmentTemplate entry in templates)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.TemplateId) || !ids.Add(entry.TemplateId))
            {
                Debug.LogWarning($"Equipment Type '{name}' contains a missing or duplicate Template ID.", this);

            }

        }

    }

}
