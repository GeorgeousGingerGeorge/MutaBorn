using System.Text;
using UnityEngine;
using MutaBorn.Gameplay;

public class UnitController : MonoBehaviour
{
    [Header("Unit's Size")]
    [SerializeField] private Vector2Int footPrintSize = Vector2Int.one;
    public Vector2Int footPrint => footPrintSize;

    [Header("Units Data- Status, Inventory, Equipment, Mutations")]

    [SerializeField] public string unitName; 
    [SerializeField] public string raceName;
    [SerializeField] public MutationID currentMutation;
    [Header("Starting Equipment - 6 Test Slots (Duplicates Allowed)")]
    [SerializeField] private EquipmentData[] startingEquipment = new EquipmentData[6];

    [Header("Starting Consumables - 2 Mission Slots")]
    [SerializeField] private ConsumableData[] startingConsumables = new ConsumableData[2];

    // Retained for pre-existing scenes/prefabs. Used only if the new array is empty.
    [SerializeField, HideInInspector] public ItemData currentEquipment;
    DynamicStats race = null;

    public UnitData data { get; private set; } 

    [Header("Reference Node to the Grid ")]
    public GridNode currentNode { get; private set; }
    public Occupancy occupancy;
    [Header("Controls Outline Visuals")]
  //  private OutlineController characterOutline;
    private bool isHovered = false;
    private bool isSelected = false;
    public void Start() 
    {
        InitializeUnitData();
        InitializeNode();
        Debug.Log(data.characterName);
        Debug.Log(data.raceName);
        Debug.Log(data.maxHealth);
        Debug.Log(data.maxMana);
        Debug.Log(data.maxActionPoints);
        Debug.Log(data.meleeDamage);
       
    }

  


    private void InitializeUnitData() 
    {
        race = GameDataBaseLoader.Instance.raceData.GetRace(raceName);
        data = UnitData.CreateNewUnit(unitName, race);

        
        if(currentMutation != MutationID.none)
        {

            bool equipped = data.EquipMutation(currentMutation);

            Debug.Log($"Mutation equipped: {equipped}");
            Debug.Log($"Active mutations: {data.activeMutations.Count}");
            
           

            foreach(BaseMutation available in data.activeMutations.Values)
            {
                Debug.Log($"{available.name} , {available.ID}, {available.currentStage}");
                    available.OnEquipped(this);

                
            }
        }


       
        bool hasStartingEquipment = false;

        if (startingEquipment != null)
        {
            foreach (EquipmentData equipment in startingEquipment)
            {
                if (equipment == null)
                {
                    continue;

                }

                hasStartingEquipment = true;
                data.EquipItem(equipment);
                Debug.Log($"Equipped starting item: {equipment.name}");

            }

        }

        // Preserve equipment already assigned in existing scenes and prefabs.
        if (!hasStartingEquipment && currentEquipment != null)
        {
            data.EquipItem(currentEquipment);
            Debug.Log($"Equipped existing starting item: {currentEquipment.name}");

        }

        Debug.Log($"Active Items: {data.equippedItems.Count}");

        if (startingConsumables != null)
        {
            foreach (ConsumableData consumable in startingConsumables)
            {
                if (consumable == null)
                {
                    continue;

                }

                if (!data.TryAddConsumable(consumable))
                {
                    Debug.LogWarning($"Consumable limit ({UnitData.ConsumableCapacity}) reached for {unitName}. " +
                        $"Skipping '{consumable.itemName}'.", this);

                }

            }

        }

        data.RefillResourcesToMax();

        RefreshEquipmentAndMutationGrants();

    }
    public bool TryUpgradeEquipment(int index)
    {
        if (data == null || !data.TryUpgradeEquippedItem(index))
        {
            return false;

        }

        RefreshEquipmentAndMutationGrants();
        Debug.Log($"Upgraded {data.equippedItems[index].itemName} to " +
            $"{data.equippedItems[index].rarity}.", this);

        return true;

    }

    // Consumables can be activated in combat, but equipment remains fixed for the mission.
    public bool TryUseConsumable(int index)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || data == null ||
            data.currentHealth <= 0 || index < 0 || index >= data.carriedConsumables.Count)
        {
            return false;

        }

        TurnManager turns = TurnManager.Instance;
        UnitFaction member = GetComponent<UnitFaction>();
        if (turns == null || !turns.IsPlayerTurn || member == null || member.Faction == null ||
            !turns.IsFactionTurn(member.Faction) || TargetingModeController.IsTargeting)
        {
            return false;

        }

        ConsumableInstance item = data.carriedConsumables[index];
        ConsumableData definition = item.definition;
        if (item.remainingUses <= 0 || definition == null || definition.UseAction == null ||
            definition.UseAction.IsPassive ||
            definition.UseAction.Category != ActionMenuCategory.Items ||
            definition.OnUseEffect == null ||
            definition.OnUseEffect.Definition == null ||
            definition.OnUseEffect.Definition.Operation != EffectOperation.ModifyResource ||
            definition.OnUseEffect.Targeting == null ||
            definition.OnUseEffect.Targeting.Reference != EffectTargetReference.Owner ||
            definition.OnUseEffect.Targeting.Selection != EffectTargetSelection.Single ||
            definition.OnUseEffect.OperationParameters == null ||
            definition.OnUseEffect.OperationParameters.Resource == ResourceIdentifier.None ||
            item.consumableType != ConsumableType.Eat)
        {
            return false;

        }

        UnitEffectSource effects = GetComponent<UnitEffectSource>();
        if (effects == null)
        {
            effects = gameObject.AddComponent<UnitEffectSource>();

        }

        // A rejected grant must never spend a use.
        if (!effects.ApplyTemporaryEffect(definition.OnUseEffect))
        {
            Debug.LogWarning($"'{definition.itemName}' needs a valid Turn Count effect " +
                "with Source Match disabled.", this);

            return false;

        }

        // The use was validated before the effect was applied.
        data.TrySpendConsumableUse(index);
        Debug.Log($"{data.characterName} used {definition.itemName}. " +
            $"AP: {data.currentActionPoints}/{data.maxActionPoints}; " +
            $"Uses remaining: {data.carriedConsumables[index].remainingUses}/{definition.MaxUses}; " +
            $"Consumables: {data.carriedConsumables.Count}/{UnitData.ConsumableCapacity}.", this);

        return true;

    }

    [ContextMenu("Test / Log Carried Consumables")]
    private void DebugConsumables()
    {
        if (data == null)
        {
            Debug.LogWarning("Consumables are initialized during Play Mode.", this);

            return;

        }

        Debug.Log($"{unitName}: {data.carriedConsumables.Count}/{UnitData.ConsumableCapacity} consumables.", this);
        for (int i = 0; i < data.carriedConsumables.Count; i++)
        {
            ConsumableInstance item = data.carriedConsumables[i];
            ConsumableData definition = item.definition;
            Debug.Log($"[Consumable {i}] " +
                $"{(definition != null ? definition.itemName : "Missing Template")} " +
                $"- Uses {item.remainingUses}/{(definition != null ? definition.MaxUses : 0)}", this);

        }

    }

    [ContextMenu("Test / Upgrade First Equipped Item")]
    private void TestUpgradeFirstItem()
    {
        if (data == null || !TryUpgradeEquipment(0))
        {
            Debug.LogWarning("Cannot upgrade first equipment: no item, max rarity, or no eligible star reward.", this);

        }

    }

    [ContextMenu("Test / Upgrade Second Equipped Item")]
    private void TestUpgradeSecondItem()
    {
        if (data == null || !TryUpgradeEquipment(1))
        {
            Debug.LogWarning("Cannot upgrade second equipment: no item, max rarity, or no eligible star reward.", this);

        }

    }

    [ContextMenu("Test / Log Generated Equipment")]
    private void DebugGeneratedEquipment()
    {
        if (data == null)
        {
            Debug.LogWarning("Equipment is generated only in Play Mode after unit initialization.", this);

            return;

        }

        for (int i = 0; i < data.equippedItems.Count; i++)
        {
            EquipmentInstance item = data.equippedItems[i];
            StringBuilder message = new StringBuilder();
            message.Append($"[Equipment {i}] {item.itemName} ({item.rarity}) | " +
                $"Armor {item.armorRating}, Ward {item.wardRating}, " +
                $"Melee {item.meleeDamage}, Ranged {item.rangeDamage}, Magic {item.magicDamage}");

            if (item.grantedActions != null)
            {
                foreach (ActionDefinition action in item.grantedActions)
                {
                    if (action != null)
                    {
                        message.Append($"\nAction: {action.DisplayName}");

                    }

                }

            }

            if (item.grantedEffects != null)
            {
                foreach (EffectGrant grant in item.grantedEffects)
                {
                    if (grant != null && grant.Definition != null)
                    {
                        message.Append($"\nEffect: {grant.Definition.DisplayName}");

                    }

                }

            }

            if (item.bonusDamage != null)
            {
                foreach (DamageComponent bonus in item.bonusDamage)
                {
                    message.Append($"\nExtra damage: {bonus.Amount} {bonus.Type}");

                }

            }

            if (item.resistances != null)
            {
                foreach (DamageResistance resistance in item.resistances)
                {
                    message.Append($"\nResistance: {resistance.Type} {resistance.Percent:+0.##;-0.##;0}%");

                }

            }

            Debug.Log(message.ToString(), this);

        }

    }

    private void RefreshEquipmentAndMutationGrants()
    {
        if (data == null)
        {
            return;

        }

        bool hasEffects = data.activeMutations.Count > 0;
        foreach (EquipmentInstance item in data.equippedItems)
        {
            if (item.grantedEffectCollection != null ||
                (item.grantedEffects != null && item.grantedEffects.Length > 0))
            {
                hasEffects = true;

                break;

            }

        }

        UnitEffectSource effectSource = GetComponent<UnitEffectSource>();
        if (hasEffects && effectSource == null)
        {
            effectSource = gameObject.AddComponent<UnitEffectSource>();

        }

        if (effectSource != null)
        {
            effectSource.RefreshEffects();

        }

        UnitActionLoadout loadout = GetComponent<UnitActionLoadout>();
        if (loadout != null)
        {
            loadout.NotifyActionsChanged();

        }

    }

    //Sets the Node of the unit,
    public void InitializeNode() 
    {
        currentNode = FindAnyObjectByType<GridManager>().GetNodeFromClosestWorldPosition(transform.position);
        occupancy = FindAnyObjectByType<Occupancy>();
        occupancy.RegisterUnit(currentNode, this);
        
    }
    public void SetNode(GridNode node) 
    {
        occupancy.UnregisterUnit(currentNode, this);
        currentNode = node;
        occupancy.RegisterUnit(currentNode, this);
    }
    public void SetHover(bool hover) 
    {
        isHovered = hover;

    }
    public void SetSelected(bool selected) 
    {
        isSelected = selected;
      

    }

    public bool GetSelected() 
    {
        return isSelected; 
    }

    public float CurrentActionPoints() 
    {
        return data.currentActionPoints;
    }
}
