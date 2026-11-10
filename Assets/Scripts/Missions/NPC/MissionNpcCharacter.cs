using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(UnitController), typeof(UnitFaction))]
public sealed class MissionNpcCharacter : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private NpcDialogueData dialogue;

    [Header("Faction outcomes")]
    [SerializeField] private FactionData playerFaction;
    [SerializeField] private FactionData hostileFaction;
    [SerializeField] private FactionData neutralFaction;

    [Header("Teach abilities on neutral outcome (optional)")]
    [SerializeField] private ActionDefinition[] taughtActions = new ActionDefinition[0];

    [Header("Supplies given when asked (optional)")]
    [SerializeField] private List<WorldLootEntry> offeredLoot = new();

    [Header("Stationary defensive turn")]
    [SerializeField, Min(0)] private int healPerTurn = 4;
    [SerializeField, Range(0, 100)] private float guardPercent = 15;
    [SerializeField] private bool retaliateWhenProvoked = true;

    [Header("Outcome events")]
    [SerializeField] private UnityEvent onJoined;
    [SerializeField] private UnityEvent onEscaped;
    [SerializeField] private UnityEvent onHostile;
    [SerializeField] private UnityEvent onNeutral;
    [SerializeField] private UnityEvent onSuppliesGiven;

    private UnitController unit;
    private UnitFaction member;
    private bool provoked;
    private bool resolved;

    public NpcDialogueData Dialogue => dialogue;
    public bool Resolved => resolved;

    private void Awake()
    {
        unit = GetComponent<UnitController>();
        member = GetComponent<UnitFaction>();

    }

    private void OnEnable()
    {
        CombatActionExecutor.DamageApplied += OnDamageApplied;

    }

    private void OnDisable()
    {
        CombatActionExecutor.DamageApplied -= OnDamageApplied;

    }

    private void OnDamageApplied(CombatDamageEvent damage)
    {
        if (damage.Target != unit || damage.Attacker == null || damage.Attacker == unit ||
            damage.ActualDamage <= 0 || resolved || !retaliateWhenProvoked)
        {

            return;

        }

        provoked = true;
        if (hostileFaction != null)
        {
            member.SetFaction(hostileFaction);

        }

    }

    public void TakeDefensiveTurn(CombatActionExecutor executor)
    {
        if (unit == null || unit.data == null || unit.data.currentHealth <= 0)
        {

            return;

        }

        if (unit.data.currentHealth < unit.data.maxHealth)
        {
            unit.data.ModifyHealth(healPerTurn);

        }

        if (provoked && executor != null && TryRetaliate(executor))
        {

            return;

        }

        if (CombatStatusHandler.Instance != null)
        {
            CombatStatusHandler.Instance.ApplyGuard(unit, guardPercent);

        }

    }

    private bool TryRetaliate(CombatActionExecutor executor)
    {
        UnitActionLoadout loadout = unit.GetComponent<UnitActionLoadout>();
        FactionManager factions = FindAnyObjectByType<FactionManager>();
        if (loadout == null || factions == null || member.Faction == null || unit.currentNode == null)
        {

            return false;

        }

        foreach (UnitController target in FindObjectsByType<UnitController>())
        {
            UnitFaction other = target.GetComponent<UnitFaction>();
            if (target == unit || target.data == null || target.data.currentHealth <= 0 ||
                target.currentNode == null || target.currentNode.floorID != unit.currentNode.floorID ||
                other == null || !factions.AreHostile(member.Faction, other.Faction))
            {

                continue;

            }

            foreach (ActionDefinition action in loadout.Actions)
            {
                if (action != null && action.CombatBehavior == CombatActionBehavior.DamageUnit &&
                    !action.IsPassive && unit.data.currentActionPoints >= Mathf.CeilToInt(action.ApCost) &&
                    Vector3.Distance(unit.transform.position, target.transform.position) <= action.CombatRange &&
                    executor.TryExecuteAiAttack(unit, target, action))
                {

                    return true;

                }

            }

        }

        return false;

    }

    public void Resolve(NpcDialogueOutcome outcome, UnitController actor)
    {
        if (resolved || outcome == NpcDialogueOutcome.None)
        {

            return;

        }

        switch (outcome)
        {
            case NpcDialogueOutcome.JoinCrew:
                if (playerFaction == null || UnitRoster.Instance == null || unit.data == null ||
                    !UnitRoster.Instance.TryRegisterUnit(unit.data))
                {
                    Debug.LogWarning("NPC could not join the player roster. Verify UnitRoster and player faction.", this);

                    return;

                }

                member.SetFaction(playerFaction);
                resolved = true;
                onJoined?.Invoke();

                break;

            case NpcDialogueOutcome.Escape:
                resolved = true;
                onEscaped?.Invoke();
                if (unit.occupancy != null && unit.currentNode != null)
                {
                    unit.occupancy.UnregisterUnit(unit.currentNode, unit);

                }

                gameObject.SetActive(false);

                break;

            case NpcDialogueOutcome.Hostile:
                if (hostileFaction == null)
                {

                    return;

                }

                member.SetFaction(hostileFaction);
                provoked = true;
                resolved = true;
                onHostile?.Invoke();

                break;

            case NpcDialogueOutcome.GiveItems:
                MissionSpoils spoils = MissionSpoils.Instance;
                if (spoils == null || spoils.IsPrepared)
                {
                    Debug.LogWarning("NPC supplies require an active, unclaimed MissionSpoils.", this);

                    return;

                }

                List<WorldLootStack> supplies = new List<WorldLootStack>();
                foreach (WorldLootEntry entry in offeredLoot)
                {
                    if (entry != null && entry.IsValid)
                    {
                        supplies.Add(new WorldLootStack(entry));

                    }

                }

                spoils.Collect(supplies);
                resolved = true;
                onSuppliesGiven?.Invoke();
                MissionLootPopup.Show($"Supplies from {unit.data.characterName}", supplies,
                    "You got nothing.");

                break;

            case NpcDialogueOutcome.Neutral:
                if (neutralFaction != null)
                {
                    member.SetFaction(neutralFaction);

                }

                if (actor != null && actor.data != null)
                {
                    foreach (ActionDefinition taught in taughtActions)
                    {
                        actor.data.TryLearnAction(taught);

                    }

                    actor.GetComponent<UnitActionLoadout>()?.NotifyActionsChanged();

                }

                resolved = true;
                onNeutral?.Invoke();

                break;

        }

        Debug.Log($"NPC {name}: {outcome} chosen by {actor?.name}.", this);

    }

}
