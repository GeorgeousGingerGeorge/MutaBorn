using UnityEngine;
using MutaBorn.Gameplay;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitController))]
public sealed class UnitDownedState : MonoBehaviour
{
    [SerializeField, Min(1)] private int reviveWindowRounds = 2;
    [SerializeField, Range(1, 100)] private int reviveHealthPercent = 25;
    [SerializeField, Min(0.01f)] private float rescueIconScale = 0.033f;
    [SerializeField, Min(0f)] private float rescueIconHeight = 2.25f;

    private UnitController unit;
    private TurnManager turns;
    private Transform visual;
    private Animator animator;
    private bool animatorWasEnabled;
    private Vector3 standingPosition;
    private Quaternion standingRotation;
    private SpriteRenderer icon;
    private UnitController lastAttacker;
    private int downedRound;
    private int remainingRounds;

    public bool IsDowned { get; private set; }
    public bool IsPermanentlyDead { get; private set; }
    public int RoundsRemaining => remainingRounds;

    private void Awake()
    {
        unit = GetComponent<UnitController>();

    }

    private void Update()
    {
        if (turns == null)
        {
            turns = TurnManager.Instance;
            if (turns != null)
            {
                turns.RoundStarted += OnRoundStarted;

            }

        }

        if (!IsDowned && !IsPermanentlyDead && unit != null && unit.data != null &&
            unit.data.currentHealth <= 0)
        {
            if (CanBeDowned(unit))
            {
                EnterDowned(null);

            }
            else
            {
                CombatActionExecutor.FinalizePermanentDeath(null, unit);

            }

        }

    }

    private void LateUpdate()
    {
        if (icon != null && icon.gameObject.activeSelf && Camera.main != null)
        {
            icon.transform.rotation = Camera.main.transform.rotation;

        }

    }

    public static bool CanBeDowned(UnitController target)
    {
        if (target == null || target.data == null)
        {
            return false;

        }

        UnitFaction member = target.GetComponent<UnitFaction>();
        FactionData faction = member != null ? member.Faction : null;
        if (faction == null)
        {
            return false;

        }

        return (TurnManager.Instance != null && faction == TurnManager.Instance.PlayerFaction) ||
            faction.Id == "npc" || faction.Id == "civilians" ||
            target.GetComponent<MissionNpcCharacter>() != null;

    }

    public void EnterDowned(UnitController attacker)
    {
        if (IsDowned || IsPermanentlyDead || unit == null || unit.data == null ||
            unit.data.currentHealth > 0)
        {
            return;

        }

        if (!CanBeDowned(unit))
        {
            CombatActionExecutor.FinalizePermanentDeath(attacker, unit);

            return;

        }

        IsDowned = true;
        lastAttacker = attacker;
        downedRound = TurnManager.Instance != null ? TurnManager.Instance.Round : 0;
        remainingRounds = reviveWindowRounds;
        unit.data.ModifyActionPoints(-unit.data.currentActionPoints);
        unit.GetComponent<UnitMovements>()?.ClearMovementRange();
        ApplyDownedPose();
        ShowRescueIcon(true);
        CombatFloatingFeedback.ReportRescueState(unit, "DOWNED");
        Debug.Log($"[Rescue] {unit.data.characterName} is downed. {remainingRounds} rounds to revive.", unit);

    }

    private void OnRoundStarted(int round)
    {
        if (!IsDowned || round <= downedRound)
        {
            return;

        }

        // Each new round uses one of the two allowed rescue turns.
        int elapsed = round - downedRound;
        remainingRounds = Mathf.Max(0, reviveWindowRounds - elapsed);
        if (remainingRounds == 0)
        {
            BecomePermanentlyDead();

        }

    }

    public bool TryRevive(UnitController rescuer)
    {
        if (!IsDowned || IsPermanentlyDead || unit == null || unit.data == null ||
            unit.data.currentHealth != 0)
        {
            return false;

        }

        int restored = Mathf.Max(1, Mathf.CeilToInt(unit.data.maxHealth * reviveHealthPercent / 100f));
        unit.data.ModifyHealth(restored);
        IsDowned = false;
        remainingRounds = 0;
        lastAttacker = null;
        ShowRescueIcon(false);
        RestoreStandingPose();
        CombatFloatingFeedback.ReportRescueState(unit, "REVIVED");
        Debug.Log($"[Rescue] {unit.data.characterName} revived with {unit.data.currentHealth} HP.", unit);

        return true;

    }

    public void BecomePermanentlyDead()
    {
        if (!IsDowned || IsPermanentlyDead)
        {
            return;

        }

        IsDowned = false;
        IsPermanentlyDead = true;
        remainingRounds = 0;
        ShowRescueIcon(false);
        CombatFloatingFeedback.ReportRescueState(unit, "DIED");
        Debug.Log($"[Rescue] {unit.data.characterName} died after the revival window expired.", unit);
        CombatActionExecutor.FinalizePermanentDeath(lastAttacker, unit);

    }

    private void ApplyDownedPose()
    {
        visual = unit.VisualModelTransform;
        if (visual == null)
        {
            return;

        }

        standingPosition = visual.localPosition;
        standingRotation = visual.localRotation;
        animator = visual.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animatorWasEnabled = animator.enabled;
            animator.enabled = false;

        }

        visual.localRotation = standingRotation * Quaternion.Euler(80f, 0f, 0f);
        visual.localPosition = standingPosition + Vector3.up * 0.08f;

    }

    private void RestoreStandingPose()
    {
        if (visual != null)
        {
            visual.localPosition = standingPosition;
            visual.localRotation = standingRotation;

        }

        if (animator != null)
        {
            animator.enabled = animatorWasEnabled;

        }

    }

    private void ShowRescueIcon(bool visible)
    {
        if (visible && icon == null)
        {
            Sprite sprite = Resources.Load<Sprite>("MutaBorn/RescueHover");
            if (sprite == null)
            {
                return;

            }

            GameObject holder = new GameObject("Rescue Hover Icon");
            holder.transform.SetParent(transform, false);
            holder.transform.localPosition = new Vector3(0f, rescueIconHeight, 0f);
            holder.transform.localScale = Vector3.one * rescueIconScale;
            icon = holder.AddComponent<SpriteRenderer>();
            icon.sprite = sprite;
            icon.sortingOrder = 200;

        }

        if (icon != null)
        {
            icon.gameObject.SetActive(visible);

        }

    }

    private void OnDisable()
    {
        if (turns != null)
        {
            turns.RoundStarted -= OnRoundStarted;
            turns = null;

        }

    }

}
