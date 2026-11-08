using MutaBorn.Gameplay;
using UnityEngine;

[RequireComponent(typeof(WorldTrap))]
public class WorldTrapSensor : MonoBehaviour
{
    public enum SensorMode
    {
        OnUnitEnter,
        EndOfFactionTurn

    }

    [SerializeField] private SensorMode mode;
    private WorldTrap trap;
    private TurnManager turns;

    private void Awake()
    {
        trap = GetComponent<WorldTrap>();

    }

    private void OnEnable()
    {
        UnitController.GridNodeChanged += OnUnitNodeChanged;
        BindTurns();

    }

    private void Start()
    {
        BindTurns();

    }

    private void Update()
    {
        if (turns == null)
        {
            BindTurns();

        }

    }

    private void OnDisable()
    {
        UnitController.GridNodeChanged -= OnUnitNodeChanged;
        if (turns != null)
        {
            turns.TurnEnded -= OnTurnEnded;
            turns = null;

        }

    }

    private void BindTurns()
    {
        if (TurnManager.Instance == null || turns == TurnManager.Instance)
        {

            return;

        }

        if (turns != null)
        {
            turns.TurnEnded -= OnTurnEnded;

        }

        turns = TurnManager.Instance;
        turns.TurnEnded += OnTurnEnded;

    }

    private void OnUnitNodeChanged(UnitController unit, GridNode previous, GridNode current)
    {
        if (mode == SensorMode.OnUnitEnter && trap.IsArmed &&
            !trap.ContainsUnitAt(unit, previous) && trap.ContainsUnit(unit))
        {
            trap.Fire();

        }

    }

    private void OnTurnEnded(FactionData faction)
    {
        if (mode == SensorMode.EndOfFactionTurn)
        {
            trap.FireForFaction(faction);

        }

    }

}
