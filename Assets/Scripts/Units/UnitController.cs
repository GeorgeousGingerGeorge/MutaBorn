using System.Text;
using UnityEngine;
using MutaBorn.Gameplay;

public class UnitController : MonoBehaviour
{
    [Header("Unit's Size")]
    [SerializeField] private Vector2Int footPrintSize = Vector2Int.one;
    public Vector2Int footPrint => footPrintSize;
    [Header("Unit's Direction")]
    public UnitFacing unitDirection { get; private set; }
   

    [Header("Units Data- Status, Inventory, Equipment, Mutations")]

    [SerializeField] public string unitName; 
    [SerializeField] public RaceNames raceName;
    [SerializeField] public MutationID currentMutation;
    [Header("Starting Fixed Mutations (No Slot Limit)")]
    [SerializeField] private MutationID[] startingMutations = new MutationID[0];
    [Header("Evolution Testing (Play Mode)")]
    [SerializeField] private MutationID testEvolutionCurrent = MutationID.none;
    [SerializeField] private MutationID testEvolutionNext = MutationID.none;
    [Header("Legacy Scene Starting Equipment (Temporary)")]
    [SerializeField] private EquipmentData[] startingEquipment = new EquipmentData[6];

    [Header("Legacy Scene Starting Consumables (Temporary)")]
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

    [Header("Selection Circle")]
    [SerializeField] private LineRenderer selectionCircle;

    [SerializeField] private Transform weaponSocket;
    private GameObject activeWeapon;


    public void InitializeUnitData() 
    {
        if (data != null)
        {
            data.LoadoutChanged -= RefreshEquipmentAndMutationGrants;

        }

        race = GameDataBaseLoader.Instance.raceData.GetRace(raceName.ToString());
        data = UnitData.CreateNewUnit(unitName, race);

        
        bool hasFixedStartingMutations = false;
        if (startingMutations != null)
        {
            foreach (MutationID id in startingMutations)
            {
                if (id == MutationID.none)
                {
                    continue;

                }

                hasFixedStartingMutations = true;
                InitializeStartingMutation(id);

            }

        }

        // Existing scenes keep their currentMutation without requiring edits.
        if (!hasFixedStartingMutations && currentMutation != MutationID.none)
        {
            InitializeStartingMutation(currentMutation);

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
                if (!data.TryEquipStartingItem(equipment))
                {
                    Debug.LogWarning($"Starting equipment '{equipment.name}' has no compatible free slot on {unitName}.", this);

                    continue;

                }

                if (equipment.weaponModelPrefab != null)
                {
                    AttachWeapon(equipment.weaponModelPrefab);
                }

                Debug.Log($"Equipped starting item: {equipment.name}");

            }

        }

        // Preserve equipment already assigned in existing scenes and prefabs.
        if (!hasStartingEquipment && currentEquipment != null)
        {
            if (currentEquipment is EquipmentData legacyEquipment &&
                data.TryEquipStartingItem(legacyEquipment))
            {
                Debug.Log($"Equipped existing starting item: {currentEquipment.name}");

            }

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
                    Debug.LogWarning($"No unlocked consumable slot available on {unitName}. " +
                        $"Skipping '{consumable.itemName}'.", this);

                }

            }

        }

        data.LoadoutChanged += RefreshEquipmentAndMutationGrants;
        RefreshEquipmentAndMutationGrants();

        // Starting passive stat grants can change maximum resources.
        data.RefillResourcesToMax();

    }
    private void InitializeStartingMutation(MutationID id)
    {
        if (!data.EquipMutation(id))
        {
            Debug.LogWarning($"Could not equip starting mutation '{id}' on {unitName}.", this);

            return;

        }

        // Legacy mutations still need their existing OnEquipped hook.
        foreach (BaseMutation mutation in data.activeMutations.Values)
        {
            if (mutation != null && mutation.ID == id)
            {
                mutation.OnEquipped(this);

                break;

            }

        }

        Debug.Log($"Equipped mutation {id}. Fixed nodes: {data.activeMutationNodes.Count}, " +
            $"legacy mutations: {data.activeMutations.Count}.", this);

    }

    public bool TryEvolveMutation(MutationID current, MutationID next)
    {
        if (!Application.isPlaying || data == null || !data.TryEvolveMutation(current, next))
        {
            return false;

        }

        RefreshEquipmentAndMutationGrants();
        Debug.Log($"Evolved mutation {current} -> {next} on {unitName}.", this);

        return true;

    }

    [ContextMenu("Test / Log Fixed Mutation Evolutions")]
    private void TestLogFixedMutationEvolutions()
    {
        if (data == null || GameDataBaseLoader.Instance == null)
        {
            Debug.LogWarning("No runtime mutation data. Enter Play Mode first.", this);

            return;

        }

        MutationDataBase database = GameDataBaseLoader.Instance.mutationData;
        foreach (ActiveMutationRecord record in data.activeMutationNodes)
        {
            MutationNodeDefinition node = database.GetNode(record.MutationId);
            if (node == null)
            {
                continue;

            }

            string choices = string.Join(", ", node.NextEvolutions);
            Debug.Log($"{unitName}: {node.DisplayName} (Level {node.Level}, family {node.FamilyId}), " +
                $"next: {(string.IsNullOrEmpty(choices) ? "None" : choices)}.", this);

        }

    }

    [ContextMenu("Test / Evolve Selected Mutation")]
    private void TestEvolveSelectedMutation()
    {
        if (!TryEvolveMutation(testEvolutionCurrent, testEvolutionNext))
        {
            Debug.LogWarning("Evolution failed. In Evolution Testing, set Current and Next " +
                "to a valid linked node in the same family.", this);

        }

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
        if (definition == null || definition.UseAction == null ||
            definition.UseAction.IsPassive ||
            definition.UseAction.Category != ActionMenuCategory.Items ||
            (!definition.UsesActionPointsOnly && item.remainingUses <= 0))
        {
            return false;

        }

        UnitStatusController itemStatuses = GetComponent<UnitStatusController>();
        if (itemStatuses != null && !itemStatuses.CanUseAction(definition.UseAction))
        {
            Debug.Log($"[Consumable] {definition.itemName} blocked by a disabling status.", this);
            return false;

        }

        if (definition.UseKind == ConsumableUseKind.TargetedAttack)
        {
            CombatActionExecutor combat = CombatActionExecutor.Instance;
            return combat != null && combat.TryBeginConsumable(this, index);

        }

        bool applied = false;
        switch (definition.UseKind)
        {
            case ConsumableUseKind.RestoreHealth:
                int beforeHealth = data.currentHealth;
                data.ModifyHealth(definition.RestoreHealth);
                applied = data.currentHealth > beforeHealth;
                break;

            case ConsumableUseKind.RestoreActionPoints:
                int beforeAP = data.currentActionPoints;
                data.ModifyActionPoints(definition.RestoreActionPoints);
                applied = data.currentActionPoints > beforeAP;
                break;

            case ConsumableUseKind.ApplyStatus:
                if (definition.AppliedStatus != null)
                {
                    UnitStatusController statuses = GetComponent<UnitStatusController>();
                    if (statuses == null)
                    {
                        statuses = gameObject.AddComponent<UnitStatusController>();

                    }

                    applied = statuses.TryApply(definition.AppliedStatus, this);

                }
                break;

            case ConsumableUseKind.LegacyEffect:
                EffectGrant useEffect = definition.OnUseEffect;
                EffectOperation operation = useEffect != null && useEffect.Definition != null
                    ? useEffect.Definition.Operation : EffectOperation.None;
                bool validOperation = (operation == EffectOperation.ModifyResource &&
                    useEffect.OperationParameters != null &&
                    useEffect.OperationParameters.Resource != ResourceIdentifier.None) ||
                    (operation == EffectOperation.ApplyStatModifier &&
                    useEffect.OperationParameters != null &&
                    useEffect.OperationParameters.Stat != StatIdentifier.None &&
                    useEffect.Activation != null &&
                    useEffect.Activation.Trigger == EffectTrigger.OnSourceActivated);
                if (!validOperation || useEffect.Targeting == null ||
                    useEffect.Targeting.Reference != EffectTargetReference.Owner ||
                    useEffect.Targeting.Selection != EffectTargetSelection.Single ||
                    item.consumableType != ConsumableType.Eat)
                {
                    return false;

                }

                UnitEffectSource effects = GetComponent<UnitEffectSource>();
                if (effects == null)
                {
                    effects = gameObject.AddComponent<UnitEffectSource>();

                }

                applied = effects.ApplyTemporaryEffect(useEffect);
                break;

        }

        if (!applied)
        {
            Debug.Log($"[Consumable] {definition.itemName} had no valid effect or resource to restore.", this);
            return false;

        }

        if (!definition.UsesActionPointsOnly)
        {
            data.TrySpendConsumableUse(index);

        }

        Debug.Log($"[Consumable] {data.characterName} used {definition.itemName}. " +
            $"HP {data.currentHealth}/{data.maxHealth}; AP " +
            $"{data.currentActionPoints}/{data.maxActionPoints}; " +
            $"Uses {data.carriedConsumables[index].remainingUses}/{definition.MaxUses}.", this);
        return true;

    }

    [ContextMenu("Test / Log Current Unit Stats")]
    private void DebugCurrentUnitStats()
    {
        if (data == null)
        {
            Debug.LogWarning("Unit data is initialized during Play Mode.", this);

            return;

        }

        Debug.Log($"{unitName} stats: Strength {data.totalStrength}, Vitality {data.totalVitality}, " +
            $"Health {data.currentHealth}/{data.maxHealth}, AP {data.currentActionPoints}/{data.maxActionPoints}, " +
            $"Melee {data.meleeDamage}, Ranged {data.rangeDamage}, Magic {data.magicDamage}, " +
            $"Armor {data.armor}, Ward {data.ward}, Will {data.will}.", this);

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

    private void OnDestroy()
    {
        if (data != null)
        {
            data.LoadoutChanged -= RefreshEquipmentAndMutationGrants;

        }

    }

    private void RefreshEquipmentAndMutationGrants()
    {
        if (data == null)
        {
            return;

        }

        bool hasEffects = data.activeMutations.Count > 0 || data.activeMutationNodes.Count > 0;
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
        unitDirection = GetComponent<UnitFacing>();
        
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

        if (selectionCircle != null)
        {
            selectionCircle.enabled = selected;
        }
    }

    public bool GetSelected() 
    {
        return isSelected; 
    }

    public float CurrentActionPoints() 
    {
        return data.currentActionPoints;
    }

    private void Awake()
    {
        if (selectionCircle == null)
        {
            return;
        }

        int segments = 48;
        float radius = 0.75f;

        selectionCircle.useWorldSpace = false;
        selectionCircle.loop = true;
        selectionCircle.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;

            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            selectionCircle.SetPosition(i, new Vector3(x, 0f, z));
        }

        selectionCircle.enabled = false;
    }
    public bool AttachWeapon(GameObject weaponPrefab)
    {
        if (weaponPrefab == null || weaponSocket == null)
        {
            return false;
        }

        if (activeWeapon != null)
        {
            Destroy(activeWeapon);
        }

        activeWeapon = Instantiate(weaponPrefab, weaponSocket, false);

        activeWeapon.transform.localPosition = Vector3.zero;
        activeWeapon.transform.localRotation = Quaternion.identity;

        return true;
    }
}


