using System;
using System.Text;
using UnityEngine;
using MutaBorn.Gameplay;
using UnityEngine.UI;

// Legacy units stay scene-local. Only explicitly configured player units
// participate in the permanent roster.
public enum UnitInitializationMode
{
    LegacyScene = 0,
    RosterOnly = 1,
    RosterCreateOnceForTesting = 2

}
public enum UnitStance 
{
    Standing,
    Crouching
}
public class UnitController : MonoBehaviour
{
    [Header("Unit's Size")]
    [SerializeField] private Vector2Int footPrintSize = Vector2Int.one;
    public Vector2Int footPrint => footPrintSize;
    [Header("Unit's Direction")]
    public UnitFacing unitDirection { get; private set; }
    public UnitStance currentStance { get; private set; } = UnitStance.Standing;

    [Header("Unit's Vision")]
    [SerializeField] private Transform VisionOrigin;
    public Transform visionOrigin => VisionOrigin;

    public FogState fogstate { get; private set; } = FogState.CLEAR;
   

    [Header("Units Data- Status, Inventory, Equipment, Mutations")]

    [SerializeField] public string unitName; 
    [SerializeField] public RaceNames raceName;
    [SerializeField] public MutationID currentMutation;
    [Header("Starting Fixed Mutations (No Slot Limit)")]
    [SerializeField] private MutationID[] startingMutations = new MutationID[0];
    [Header("Evolution Testing (Play Mode)")]
    [SerializeField] private MutationID testEvolutionCurrent = MutationID.none;
    [SerializeField] private MutationID testEvolutionNext = MutationID.none;
    [Header("Scene Starting Equipment")]
    [SerializeField] private EquipmentTemplateReference[] startingEquipment = new EquipmentTemplateReference[0];

    [Header("Legacy Scene Starting Consumables (Temporary)")]
    [SerializeField] private ConsumableData[] startingConsumables = new ConsumableData[2];

    // Retained for pre-existing scenes/prefabs. Used only if the new array is empty.
    [SerializeField] private EquipmentTemplateReference singleStartingEquipment;
    [Header("Optional Permanent Player Roster")]
    [SerializeField] private UnitInitializationMode initializationMode = UnitInitializationMode.LegacyScene;
    [SerializeField, Min(0)] private int rosterDeploymentSlot;
    private string boundRosterUnitId;

    // For an opt-in mission spawn area. Sets the wrapper's deployment slot
    // before GridManager initializes it; it never creates a new UnitData.
    public void ConfigureCrewDeployment(int slotIndex)
    {
        initializationMode = UnitInitializationMode.RosterOnly;
        rosterDeploymentSlot = slotIndex;

    }

    public UnitInitializationMode InitializationMode => initializationMode;
    public int RosterDeploymentSlot => rosterDeploymentSlot;

    DynamicStats race = null;

    public UnitData data { get; private set; } 

    [Header("Reference Node to the Grid ")]
    public GridNode currentNode { get; private set; }
    public static event Action<UnitController, GridNode, GridNode> GridNodeChanged;
    public Occupancy occupancy;


    [Header("Controls Outline Visuals")]

  //  private OutlineController characterOutline;
    private bool isHovered = false;
    private bool isSelected = false;

    [Header("Selection Circle")]
    [SerializeField] private LineRenderer selectionCircle;

    [SerializeField] private Transform weaponSocket;
    private readonly Transform[] playerSockets = new Transform[10];
    private readonly GameObject[] activeEquipmentModels = new GameObject[10];
    private GameObject activeWeapon;
    private GameObject activeRaceModel;


    public void InitializeUnitData()
    {
        TryInitializeUnitData();

    }

    // GridManager calls this before registering the scene wrapper on the grid.
    // Failing closed prevents an unbound player placeholder from rolling new gear.
    public bool TryInitializeUnitData()
    {
        if (initializationMode == UnitInitializationMode.LegacyScene)
        {
            InitializeLegacyUnitData();

            return data != null;

        }

        UnitRoster roster = UnitRoster.Instance;
        if (!string.IsNullOrEmpty(boundRosterUnitId))
        {
            return roster != null &&
                roster.TryGetDeploymentUnit(rosterDeploymentSlot, out UnitData assigned) &&
                ReferenceEquals(data, assigned);

        }

        if (roster == null || rosterDeploymentSlot < 0)
        {
            Debug.LogError($"Roster mode requires a UnitRoster and valid deployment slot on '{name}'.", this);

            return false;

        }

        if (roster.TryGetDeploymentUnit(rosterDeploymentSlot, out UnitData existing))
        {
            if (!roster.TryBindDeploymentUnit(rosterDeploymentSlot, this, out UnitData bound))
            {
                Debug.LogError($"Roster unit in slot {rosterDeploymentSlot} is already bound to another scene object.", this);

                return false;

            }

            BindExistingUnitData(existing);
            boundRosterUnitId = bound.UnitId;
            Debug.Log($"[UnitRoster] Rebound '{data.characterName}' [{data.UnitId}] " +
                $"to deployment slot {rosterDeploymentSlot} without generating new gear.", this);

            return true;

        }

        if (initializationMode != UnitInitializationMode.RosterCreateOnceForTesting)
        {
            Debug.LogError($"No roster unit assigned to deployment slot {rosterDeploymentSlot} for '{name}'.", this);

            return false;

        }

        // TEMPORARY opt-in bootstrap: retain the exact existing starter logic,
        // but run it only the first time this deployment slot is empty.
        InitializeLegacyUnitData();
        if (data == null || !roster.TryRegisterAndBindNewUnit(data, rosterDeploymentSlot, this))
        {
            Debug.LogError($"Could not enroll generated test unit '{name}' in roster slot {rosterDeploymentSlot}.", this);

            return false;

        }

        boundRosterUnitId = data.UnitId;
        Debug.Log($"[UnitRoster] Created test recruit '{data.characterName}' [{data.UnitId}] " +
            $"in deployment slot {rosterDeploymentSlot}.", this);

        return true;

    }

    // No CreateNewUnit, starter rolls, mutation OnEquipped or refill here.
    private void BindExistingUnitData(UnitData persistentData)
    {
        if (data != null)
        {
            data.LoadoutChanged -= RefreshEquipmentAndMutationGrants;

        }

        data = persistentData;
        race = data.RaceDefinition;
        unitName = data.characterName;
        gameObject.name = unitName;
        if (Enum.TryParse(data.raceName, true, out RaceNames boundRace))
        {
            raceName = boundRace;

        }

        InitializeRaceModel();
        data.LoadoutChanged += RefreshEquipmentAndMutationGrants;
        RefreshEquipmentAndMutationGrants();


    }

    private void InitializeLegacyUnitData()
    {
        if (data != null)
        {
            data.LoadoutChanged -= RefreshEquipmentAndMutationGrants;

        }

        race = GameDataBaseLoader.Instance.raceData.GetRace(raceName.ToString());
        data = UnitData.CreateNewUnit(unitName, race, CalculateUnitWidth());
        gameObject.name = unitName; 
        InitializeRaceModel();

        
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
            foreach (EquipmentTemplateReference equipment in startingEquipment)
            {
                if (!equipment.TryResolve(out EquipmentTemplate definition))
                {
                    continue;

                }

                hasStartingEquipment = true;
                if (!data.TryEquipStartingItem(equipment, out UnitSlotId equippedSlot))
                {
                    Debug.LogWarning($"Starting equipment '{definition.DisplayName}' has no compatible free slot on {unitName}.", this);

                    continue;

                }

                Debug.Log($"Equipped starting item: {definition.DisplayName}");

            }

        }

        // Preserve equipment already assigned in existing scenes and prefabs.
        if (!hasStartingEquipment && singleStartingEquipment.TryResolve(out EquipmentTemplate fallback) &&
            data.TryEquipStartingItem(singleStartingEquipment))
        {
            Debug.Log($"Equipped starting item: {fallback.DisplayName}");

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

        if (definition.UseKind == ConsumableUseKind.TargetedAttack ||
            definition.UseKind == ConsumableUseKind.CaptureUnit)
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

    private void OnEnable()
    {
        if (currentNode != null)
        {
            GridNodeChanged?.Invoke(this, null, currentNode);

        }

    }

    private void OnDisable()
    {
        if (currentNode != null)
        {
            GridNodeChanged?.Invoke(this, currentNode, null);

        }

    }

    private void OnDestroy()
    {
        if (data != null)
        {
            data.LoadoutChanged -= RefreshEquipmentAndMutationGrants;

            if (!string.IsNullOrEmpty(boundRosterUnitId))
            {
                // Remove scene-owned status grants, without refilling HP/AP/charges
                // or modifying any actual equipment or consumable instances.
                data.ClearSceneRuntimeStatModifiers();
                if (UnitRoster.Instance != null)
                {
                    UnitRoster.Instance.ReleaseSceneBinding(boundRosterUnitId, this);

                }

            }

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

        RefreshEquipmentModels();

        UnitActionLoadout loadout = GetComponent<UnitActionLoadout>();
        if (loadout != null)
        {
            loadout.NotifyActionsChanged();

        }

    }

    //Sets the Node of the unit,
    public void InitializeNode() 
    {
        GridNode previousNode = currentNode;
        currentNode = FindAnyObjectByType<GridManager>().GetNodeFromClosestWorldPosition(transform.position);
        occupancy = FindAnyObjectByType<Occupancy>();
        occupancy.RegisterUnit(currentNode, this);
        unitDirection = GetComponent<UnitFacing>();

        if (previousNode != currentNode)
        {
            GridNodeChanged?.Invoke(this, previousNode, currentNode);

        }

    }
    public void SetNode(GridNode node) 
    {
        GridNode previousNode = currentNode;
        occupancy.UnregisterUnit(currentNode, this);
        currentNode = node;
        occupancy.RegisterUnit(currentNode, this);

        if (previousNode != currentNode)
        {
            GridNodeChanged?.Invoke(this, previousNode, currentNode);

        }

    }
    public void SetNodeWithoutOccupancy(GridNode node)
    {
        if (occupancy != null && currentNode != null)
        {
            occupancy.UnregisterUnit(currentNode, this);
        }

        currentNode = node;
    }
    public void SetHover(bool hover) 
    {
        isHovered = hover;

    }
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UnitFaction factionController = GetComponent<UnitFaction>();

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

    // The model is a VISUAL child of this combat wrapper, not another UnitData.
    // Safe to call again when binding an existing roster unit: it keeps the same
    // race model if present and only replaces a mismatched playermodel child.
    public bool InitializeRaceModel()
    {
        if (data == null || data.RaceDefinition == null)
        {
            return false;

        }

        GameObject prefab = data.RaceDefinition.RaceModelPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[UnitVisual] No model prefab assigned for {data.raceName}.", this);

            return false;

        }

        GameObject oldModel = activeRaceModel;
        if (oldModel == null)
        {
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("playermodel_", StringComparison.OrdinalIgnoreCase))
                {
                    oldModel = child.gameObject;

                    break;

                }

            }

        }

        if (oldModel != null && string.Equals(
            oldModel.name.Replace("(Clone)", "").Trim(), prefab.name, StringComparison.OrdinalIgnoreCase))
        {
            activeRaceModel = oldModel;
            ReconnectRaceVisualComponents(null, oldModel);

            return true;

        }

        GameObject model = Instantiate(prefab, transform, false);
        model.name = prefab.name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        // Leave the prefab's authored scale intact (Orcs and other races differ).
        activeRaceModel = model;
        ReconnectRaceVisualComponents(oldModel, model);

        if (oldModel != null)
        {
            oldModel.SetActive(false);
            Destroy(oldModel);

        }

        return true;

    }

    private void ReconnectRaceVisualComponents(GameObject oldModel, GameObject newModel)
    {
        Transform oldRoot = oldModel != null ? oldModel.transform : null;
        Animator newAnimator = newModel.GetComponentInChildren<Animator>(true);
        UnitMovements movement = GetComponent<UnitMovements>();
        if (movement != null)
        {
            movement.RefreshRaceAnimator(newAnimator);

        }

        UnitFacing facing = GetComponent<UnitFacing>();
        if (facing != null)
        {
            facing.RebindRaceModel(oldRoot, newModel.transform);

        }

        for (int i = 0; i < activeEquipmentModels.Length; i++)
        {
            if (activeEquipmentModels[i] != null)
            {
                Destroy(activeEquipmentModels[i]);
                activeEquipmentModels[i] = null;
            }
        }

        playerSockets[0] = FindVisualChild(newModel.transform, "wpnSkt_R");
        playerSockets[1] = FindVisualChild(newModel.transform, "wpnSkt_L");
        playerSockets[2] = FindVisualChild(newModel.transform, "armorSkt_Head");
        playerSockets[3] = FindVisualChild(newModel.transform, "armorSkt_Body");
        playerSockets[4] = FindVisualChild(newModel.transform, "armorSkt_Arms");
        playerSockets[5] = FindVisualChild(newModel.transform, "armorSkt_Legs");
        playerSockets[6] = FindVisualChild(newModel.transform, "armorSkt_Boots");
        playerSockets[7] = FindVisualChild(newModel.transform, "accessorySkt_1");
        playerSockets[8] = FindVisualChild(newModel.transform, "accessorySkt_2");
        playerSockets[9] = FindVisualChild(newModel.transform, "accessorySkt_3");

        // Preserve compatibility with the existing AttachWeapon().
        weaponSocket = playerSockets[0] != null ? playerSockets[0] : playerSockets[1];

        if (weaponSocket == null)
        {
            Debug.LogWarning($"[UnitVisual] {data.characterName}: missing weapon socket on {newModel.name}.", this);

        }

        if (selectionCircle == null || (oldRoot != null && selectionCircle.transform.IsChildOf(oldRoot)))
        {
            selectionCircle = newModel.GetComponentInChildren<LineRenderer>(true);
            InitializeSelectionCircle();

        }

        OutlineController outline = GetComponent<OutlineController>();
        if (outline != null)
        {
            outline.RebindRaceModel(oldRoot, newModel.transform);

        }

    }

    private static Transform FindVisualChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
            {
                return child;

            }

        }

        return null;

    }

    private void Awake()
    {
        InitializeSelectionCircle();

    }

    private void InitializeSelectionCircle()
    {
        UnitFaction factionController = GetComponent<UnitFaction>();
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

        if (factionController.GetFactionID() != "playerFaction") 
        {
            if(factionController.GetFactionID() == "raiderFaction") 
            {
                selectionCircle.material = GameDataBaseLoader.Instance.selectionCircle[2];
            }
            else if(factionController.GetFactionID() == "civilians") 
            {
                selectionCircle.material = GameDataBaseLoader.Instance.selectionCircle[1];

            }
        }
        

        selectionCircle.enabled = false;
    }

    private int GetSocketIndex(EquipmentSlots slot)
    {
        switch (slot)
        {
            case EquipmentSlots.MainHand:
            case EquipmentSlots.TwoHanded:
                return 0;

            case EquipmentSlots.OffHand:
                return 1;

            case EquipmentSlots.ArmorHelmet:
                return 2;

            case EquipmentSlots.ArmorChest:
                return 3;

            case EquipmentSlots.ArmorLeftArm:
            case EquipmentSlots.ArmorRightArm:
            case EquipmentSlots.ArmorArms:
                return 4;

            case EquipmentSlots.ArmorLeftLeg:
            case EquipmentSlots.ArmorRightLeg:
            case EquipmentSlots.ArmorLegs:
                return 5;

            default:
                return -1;
        }
    }
    private int GetSocketIndex(UnitSlotId slot)
    {
        int index = (int)slot - 1;

        return index >= 0 && index < playerSockets.Length
            ? index
            : -1;
    }
    private void RefreshEquipmentModels()
    {
        for (int i = 0; i < activeEquipmentModels.Length; i++)
        {
            if (activeEquipmentModels[i] != null)
            {
                Destroy(activeEquipmentModels[i]);
                activeEquipmentModels[i] = null;

            }

        }

        if (data == null)
        {
            return;

        }

        foreach (UnitSlotId slot in UnitLoadoutSlots.All)
        {
            if (!data.TryGetEquipmentAtSlot(slot, out EquipmentInstance item) || item.ModelPrefab == null)
            {
                continue;

            }

            EquipmentTemplate definition = null;
            if (item.typeDefinition != null)
            {
                item.typeDefinition.TryGetTemplate(item.templateId, out definition);

            }

            EquipmentAttachmentSocket socket = definition != null &&
                definition.SocketOverride != EquipmentAttachmentSocket.Auto
                ? definition.SocketOverride : item.typeDefinition != null
                    ? item.typeDefinition.DefaultSocket : EquipmentAttachmentSocket.Auto;
            int index = socket == EquipmentAttachmentSocket.Auto
                ? GetSocketIndex(slot) : (int)socket - 1;
            if (index < 0 || index >= playerSockets.Length || playerSockets[index] == null ||
                !AttachWeapon(item.ModelPrefab, index))
            {
                continue;

            }

            if (definition == null)
            {
                continue;

            }

            Transform attached = activeEquipmentModels[index].transform;
            bool overridden = definition.OverrideAttachment;
            attached.localPosition = overridden ? definition.PositionOffset : item.typeDefinition.PositionOffset;
            attached.localRotation = Quaternion.Euler(overridden ? definition.RotationOffset : item.typeDefinition.RotationOffset);
            attached.localScale = Vector3.Scale(attached.localScale,
                overridden ? definition.Scale : item.typeDefinition.Scale);

            bool applyColors = definition.OverrideColors || item.typeDefinition.ApplyColors;
            if (applyColors)
            {
                Color primary = definition.OverrideColors ? definition.PrimaryColor : item.typeDefinition.PrimaryColor;
                Color secondary = definition.OverrideColors ? definition.SecondaryColor : item.typeDefinition.SecondaryColor;
                Color accent = definition.OverrideColors ? definition.AccentColor : item.typeDefinition.AccentColor;
                ApplyEquipmentColors(activeEquipmentModels[index], primary, secondary, accent);

            }

        }

    }

    private static void ApplyEquipmentColors(GameObject model, Color primary, Color secondary, Color accent)
    {
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    continue;

                }

                properties.Clear();
                renderer.GetPropertyBlock(properties, i);
                if (material.HasProperty("_BaseColor")) properties.SetColor("_BaseColor", primary);
                if (material.HasProperty("_Color")) properties.SetColor("_Color", primary);
                if (material.HasProperty("_PrimaryColor")) properties.SetColor("_PrimaryColor", primary);
                if (material.HasProperty("_SecondaryColor")) properties.SetColor("_SecondaryColor", secondary);
                if (material.HasProperty("_AccentColor")) properties.SetColor("_AccentColor", accent);
                renderer.SetPropertyBlock(properties, i);

            }

        }

    }

    public bool AttachWeapon(GameObject weaponPrefab, int socketIndex = 0)
    {
        if (weaponPrefab == null || socketIndex < 0 || socketIndex >= playerSockets.Length)
        {
            return false;
        }

        Transform socket = playerSockets[socketIndex];

        if (socket == null)
        {
            return false;
        }

        if (activeEquipmentModels[socketIndex] != null)
        {
            Destroy(activeEquipmentModels[socketIndex]);
        }

        activeEquipmentModels[socketIndex] = Instantiate(weaponPrefab, socket, false);

        activeEquipmentModels[socketIndex].transform.localPosition = Vector3.zero;
        activeEquipmentModels[socketIndex].transform.localRotation = Quaternion.identity;

        return true;
    }

    public void SetFogState(FogState state) 
    {
        
        
        Transform child = transform.Find("Floating Health Bar/Background");
        GameObject canvas = child.gameObject;
        Renderer[] character = activeRaceModel.GetComponentsInChildren<Renderer>();


      
        switch (state)
        {
            case FogState.CLEAR:
                {

                    foreach (Renderer bodypart in character) 
                    {
                        bodypart.enabled = true;
                    }

                    canvas.SetActive(true);   

                }
                break;
            case FogState.DARK:
                {

                    foreach (Renderer bodypart in character)
                    {
                        bodypart.enabled = false;
                    }

                    canvas.SetActive(false);

                }
                break;
        }
    }
    private float CalculateUnitWidth() 
    {
        float xStep = footPrintSize.x;
        float yStep = footPrintSize.y;
        float widthModifier = 0.8f;

        return (xStep + yStep) * widthModifier;
    }
    public void SetStance(UnitStance state) 
    {

        switch (state) 
        {
            case UnitStance.Standing:
                currentStance = UnitStance.Standing;
                break;
            case UnitStance.Crouching:
                currentStance = UnitStance.Crouching;
                break;
        }
    }
}


