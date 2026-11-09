using UnityEngine;

// Temporary cover and traps expire at the beginning of the creator's faction turn.
public sealed class MutationDeployable : MonoBehaviour
{
    public GridNode Node { get; private set; }

    private FactionData creatorFaction;
    private int remainingTurns;
    private TurnManager turns;

    public void Initialize(UnitController creator, GridNode node, int durationOwnerTurns)
    {
        Node = node;
        creatorFaction = creator.GetComponent<UnitFaction>()?.Faction;
        remainingTurns = Mathf.Max(1, durationOwnerTurns);
        BindTurns();

    }

    private void Start()
    {
        BindTurns();

    }

    private void BindTurns()
    {
        if (turns != null || TurnManager.Instance == null)
        {

            return;

        }

        turns = TurnManager.Instance;
        turns.TurnStarted += OnTurnStarted;

    }

    private void OnTurnStarted(FactionData faction)
    {
        if (faction != creatorFaction)
        {

            return;

        }

        remainingTurns--;
        if (remainingTurns <= 0)
        {
            Destroy(gameObject);

        }

    }

    private void OnDestroy()
    {
        if (turns != null)
        {
            turns.TurnStarted -= OnTurnStarted;

        }

        ObstacleController cover = GetComponent<ObstacleController>();
        if (cover != null && cover.IsBlocking)
        {
            cover.TrySetBlocking(false);

        }

    }

}
