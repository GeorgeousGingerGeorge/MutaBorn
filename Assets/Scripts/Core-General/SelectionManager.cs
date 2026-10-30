using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UIButton = UnityEngine.UI.Button;

public class SelectionManager : MonoBehaviour
{
    [Header("Unit Cycling (Optional UI Buttons)")]
    [SerializeField] private UIButton previousUnitButton;
    [SerializeField] private UIButton nextUnitButton;

    public TileController selectedTile { get; private set; }
    public UnitController selectedUnit { get; private set; }

    private TileController selectedUnitTile;
    private Occupancy occupancy;
    private MouseLocation mouseInWorld;
    private InputManager input;

    private readonly List<UnitController> playerUnits = new List<UnitController>();
    private readonly List<UnitController> newUnits = new List<UnitController>();
    private float nextRosterRefreshTime;
    private bool? lastPlayerTurn;

    private void Awake()
    {
        if (previousUnitButton != null)
        {
            previousUnitButton.onClick.AddListener(SelectPreviousUnit);

        }

        if (nextUnitButton != null)
        {
            nextUnitButton.onClick.AddListener(SelectNextUnit);

        }

    }

    private void Start()
    {
        mouseInWorld = FindAnyObjectByType<MouseLocation>();
        input = FindAnyObjectByType<InputManager>();
        occupancy = FindAnyObjectByType<Occupancy>();
        RefreshPlayerRoster();
        EnsureUnitSelected();

    }

    private void Update()
    {
        if (Time.unscaledTime >= nextRosterRefreshTime)
        {
            RefreshPlayerRoster();
            nextRosterRefreshTime = Time.unscaledTime + 0.5f;

        }

        EnsureUnitSelected();
        RefreshTurnSelectionVisuals();
        RefreshCycleButtons();

        if (input == null)
        {
            return;

        }

        int cycleDirection = input.GetUnitCycleDirection();
        if (cycleDirection != 0)
        {
            CycleUnit(cycleDirection);

        }

        if (mouseInWorld == null || !input.OnLeftClick() ||
            !CanSelectManually() || TargetingModeController.BlocksSelection)
        {
            return;

        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;

        }

        HandleClickSelection();

    }

    private bool CanSelectManually()
    {
        TurnManager turns = TurnManager.Instance;
        if (turns == null || !turns.IsPlayerTurn)
        {
            return false;

        }

        MissionObjectiveManager mission = MissionObjectiveManager.Instance;
        return mission == null ||
            (mission.Status != MissionStatus.Victory && mission.Status != MissionStatus.Defeat);

    }

    private bool IsLivingPlayerUnit(UnitController unit)
    {
        TurnManager turns = TurnManager.Instance;
        if (unit == null || !unit.isActiveAndEnabled || unit.data == null ||
            unit.data.currentHealth <= 0 || turns == null || turns.PlayerFaction == null)
        {
            return false;

        }

        UnitFaction membership = unit.GetComponent<UnitFaction>();
        return membership != null && membership.Faction == turns.PlayerFaction;

    }

    // Shared command validation for selection, movement, and the action menu.
    public bool CanCommandUnit(UnitController unit)
    {
        TurnManager turns = TurnManager.Instance;
        return IsLivingPlayerUnit(unit) && turns != null && turns.IsPlayerTurn;

    }

    private void RefreshPlayerRoster()
    {
        playerUnits.RemoveAll(unit => !IsLivingPlayerUnit(unit));
        newUnits.Clear();

        foreach (UnitFaction membership in FindObjectsByType<UnitFaction>())
        {
            if (membership == null)
            {
                continue;

            }

            UnitController unit = membership.GetComponent<UnitController>();
            if (IsLivingPlayerUnit(unit) && !playerUnits.Contains(unit))
            {
                newUnits.Add(unit);

            }

        }

        // Keep the existing roster order stable throughout the mission.
        // Newly discovered units are appended without relying on deprecated object IDs.
        playerUnits.AddRange(newUnits);

    }

    private void EnsureUnitSelected()
    {
        if (IsLivingPlayerUnit(selectedUnit))
        {
            return;

        }

        if (selectedUnit != null || selectedUnitTile != null)
        {
            ClearUnitSelection();
            ClearDestination();

        }

        if (playerUnits.Count > 0)
        {
            SelectUnit(playerUnits[0]);

        }

    }

    private void RefreshTurnSelectionVisuals()
    {
        TurnManager turns = TurnManager.Instance;
        bool playerTurn = turns != null && turns.IsPlayerTurn;
        if (lastPlayerTurn.HasValue && lastPlayerTurn.Value == playerTurn)
        {
            return;

        }

        lastPlayerTurn = playerTurn;
        if (selectedUnit == null)
        {
            return;

        }

        UnitMovements movement = selectedUnit.GetComponent<UnitMovements>();
        if (movement == null)
        {
            return;

        }

        movement.ClearMovementRange();
        if (playerTurn && !TargetingModeController.IsTargeting)
        {
            movement.ShowMovementRange();

        }

    }

    private void RefreshCycleButtons()
    {
        bool canCycle = CanSelectManually() && playerUnits.Count > 1;

        if (previousUnitButton != null && previousUnitButton.interactable != canCycle)
        {
            previousUnitButton.interactable = canCycle;

        }

        if (nextUnitButton != null && nextUnitButton.interactable != canCycle)
        {
            nextUnitButton.interactable = canCycle;

        }

    }

    public void SelectNextUnit()
    {
        CycleUnit(1);

    }

    public void SelectPreviousUnit()
    {
        CycleUnit(-1);

    }

    private void CycleUnit(int direction)
    {
        if (direction == 0 || !CanSelectManually())
        {
            return;

        }

        RefreshPlayerRoster();
        if (playerUnits.Count == 0)
        {
            return;

        }

        int currentIndex = playerUnits.IndexOf(selectedUnit);
        int nextIndex = currentIndex < 0
            ? (direction > 0 ? 0 : playerUnits.Count - 1)
            : (currentIndex + (direction > 0 ? 1 : -1) + playerUnits.Count) % playerUnits.Count;

        SelectUnit(playerUnits[nextIndex]);

    }

    private void SelectUnit(UnitController unit)
    {
        if (!IsLivingPlayerUnit(unit) || selectedUnit == unit)
        {
            return;

        }

        // An action begun by the previous unit must not survive a unit switch.
        CombatActionExecutor.Instance?.CancelTargeting();
        TargetingModeController.Instance?.CancelTargeting();

        ClearDestination();
        ClearUnitSelection();

        selectedUnit = unit;
        selectedUnit.SetSelected(true);
        RefreshSelectedUnitTile();

        if (CanCommandUnit(unit) && !TargetingModeController.IsTargeting)
        {
            unit.GetComponent<UnitMovements>()?.ShowMovementRange();

        }

    }

    private void RefreshSelectedUnitTile()
    {
        ClearUnitSelectedTile();
        if (selectedUnit == null || selectedUnit.currentNode == null || occupancy == null)
        {
            return;

        }

        GridManager gridManager = occupancy.ReturnGrid();
        if (gridManager == null)
        {
            return;

        }

        selectedUnitTile = gridManager.FindTile(selectedUnit.currentNode);
        if (selectedUnitTile != null)
        {
            selectedUnitTile.SetUnitSelected(true);

        }

    }

    private void SelectTile(TileController tile)
    {
        if (tile == null || tile == selectedTile)
        {
            return;

        }

        ClearDestination();
        selectedTile = tile;
        selectedTile.SetSelected(true);

    }

    public void ClearDestination()
    {
        if (selectedTile != null)
        {
            selectedTile.SetSelected(false);

        }

        selectedTile = null;

    }

    // Internal reset for disabled/defeated units; regular clicks never deselect.
    public void ClearUnitSelection()
    {
        if (selectedUnit != null)
        {
            selectedUnit.GetComponent<UnitMovements>()?.ClearMovementRange();
            selectedUnit.SetSelected(false);

        }

        ClearUnitSelectedTile();
        selectedUnit = null;

    }

    public void ClearUnitSelectedTile()
    {
        if (selectedUnitTile == null)
        {
            return;

        }

        selectedUnitTile.SetUnitSelected(false);
        selectedUnitTile = null;

    }

    public void RefreshSelectedUnitAfterMovement(UnitController movedUnit)
    {
        if (movedUnit == null || selectedUnit != movedUnit)
        {
            return;

        }

        UnitMovements movement = movedUnit.GetComponent<UnitMovements>();
        movement?.ClearMovementRange();
        RefreshSelectedUnitTile();

        if (movement != null && CanCommandUnit(movedUnit) && !TargetingModeController.IsTargeting)
        {
            movement.ShowMovementRange();

        }

    }

    private void HandleClickSelection()
    {
        if (mouseInWorld.unitHit && mouseInWorld.unitTarget.collider != null)
        {
            UnitController clickedUnit = mouseInWorld.unitTarget.collider.GetComponentInParent<UnitController>();
            if (clickedUnit != null)
            {
                if (IsLivingPlayerUnit(clickedUnit))
                {
                    SelectUnit(clickedUnit);

                }

                // Clicking enemies never changes the selected player unit.
                return;

            }

        }

        if (!mouseInWorld.surfaceHit || mouseInWorld.groundTarget.collider == null)
        {
            return;

        }

        TileController tile = mouseInWorld.groundTarget.collider.GetComponentInParent<TileController>();
        if (tile == null || tile.node == null || occupancy == null)
        {
            return;

        }

        UnitController occupant = occupancy.FindUnit(tile.node);
        if (occupant != null)
        {
            if (IsLivingPlayerUnit(occupant))
            {
                SelectUnit(occupant);

            }

            return;

        }

        if (CanCommandUnit(selectedUnit))
        {
            SelectTile(tile);

        }

    }

    private void OnDestroy()
    {
        if (previousUnitButton != null)
        {
            previousUnitButton.onClick.RemoveListener(SelectPreviousUnit);

        }

        if (nextUnitButton != null)
        {
            nextUnitButton.onClick.RemoveListener(SelectNextUnit);

        }

    }

}
