using System;
using System.Collections;
using System.Collections.Generic;
using MutaBorn.Gameplay;
using UnityEngine;
using UnityEngine.Events;

public enum TrapAreaShape
{
    Rectangle,
    Radius,
    ForwardStrip,
    ForwardRay

}

[Serializable]
public sealed class TrapStatusChance
{
    public StatusDefinition status;
    [Range(0f, 100f)] public float chancePercent = 100f;

}

// The shared receiver for all traps. Sensors and other objects decide when to fire it.
public class WorldTrap : MonoBehaviour
{
    [Header("Affected Grid Area")]
    [SerializeField] private int floorId = -1;
    [SerializeField] private TrapAreaShape shape = TrapAreaShape.Rectangle;
    [SerializeField] private Vector2Int areaSizeInTiles = Vector2Int.one;
    [SerializeField, Min(0)] private int radiusInTiles = 1;
    [SerializeField, Min(1)] private int rangeInTiles = 4;
    [SerializeField, Min(1)] private int stripWidthInTiles = 1;
    [SerializeField] private bool stopRayAtFirstUnit = true;

    [Header("Damage and Statuses")]
    [SerializeField] private DamageType damageType = DamageType.Pierce;
    [SerializeField, Min(0)] private int minimumDamage = 4;
    [SerializeField, Min(0)] private int maximumDamage = 8;
    [SerializeField] private List<TrapStatusChance> statusChances = new List<TrapStatusChance>();
    [Tooltip("Empty means all factions, including allies and civilians.")]
    [SerializeField] private List<FactionData> affectedFactions = new List<FactionData>();

    [Header("Activation")]
    [SerializeField] private bool oneShot = true;
    [SerializeField] private bool startsArmed = true;
    [SerializeField, Min(0f)] private float impactDelaySeconds;
    [SerializeField] private UnityEvent onFired = new UnityEvent();
    [SerializeField] private UnityEvent onHit = new UnityEvent();
    [SerializeField] private UnityEvent onDisarmed = new UnityEvent();

    [Header("Hidden Trap Detection")]
    [SerializeField] private bool startsHidden;
    [SerializeField, Range(0f, 100f)] private float detectionChancePercent = 50f;
    [SerializeField, Min(0f)] private float bonusChancePerPerception = 1f;
    [SerializeField, Min(0)] private int detectionRangeInTiles = 2;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Color armedColor = new Color(0.95f, 0.13f, 0.13f);
    [SerializeField] private Color disarmedColor = new Color(0.28f, 0.18f, 0.2f);

    private readonly HashSet<UnitController> detectionAttempts = new HashSet<UnitController>();
    private GridNode anchor;
    private GridManager grid;
    private bool armed;
    private bool fired;
    private bool revealed;
    private bool firing;
    private bool pendingImpact;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public bool IsArmed => armed && (!oneShot || !fired);
    public bool IsRevealed => revealed;

    private void Awake()
    {
        armed = startsArmed;
        revealed = !startsHidden;
        RefreshVisual();

    }

    private void OnEnable()
    {
        UnitController.GridNodeChanged += OnUnitNodeChanged;

    }

    private void Start()
    {
        ResolveAnchor();
        foreach (UnitController unit in FindObjectsByType<UnitController>())
        {
            TryDetect(unit);

        }

    }

    private void OnDisable()
    {
        UnitController.GridNodeChanged -= OnUnitNodeChanged;

    }

    private void OnUnitNodeChanged(UnitController unit, GridNode previous, GridNode current)
    {
        TryDetect(unit);

    }

    private void TryDetect(UnitController unit)
    {
        if (revealed || unit == null || !unit.isActiveAndEnabled || unit.data == null ||
            unit.currentNode == null || !ResolveAnchor())
        {

            return;

        }

        TurnManager turns = TurnManager.Instance;
        UnitFaction faction = unit.GetComponent<UnitFaction>();
        if (turns == null || faction == null || faction.Faction != turns.PlayerFaction ||
            unit.currentNode.floorID != anchor.floorID ||
            Mathf.Max(Mathf.Abs(unit.currentNode.coords.x - anchor.coords.x),
                Mathf.Abs(unit.currentNode.coords.y - anchor.coords.y)) > detectionRangeInTiles ||
            !detectionAttempts.Add(unit))
        {

            return;

        }

        float chance = Mathf.Clamp(detectionChancePercent +
            unit.data.totalPerception * bonusChancePerPerception, 0f, 100f);
        if (chance >= 100f || chance > 0f && UnityEngine.Random.value * 100f < chance)
        {
            Reveal();

        }

    }

    public void Reveal()
    {
        revealed = true;
        RefreshVisual();

    }

    private void RefreshVisual()
    {
        if (visualRoot == null)
        {

            return;

        }

        visualRoot.SetActive(revealed);
        Color tint = IsArmed ? armedColor : disarmedColor;
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        foreach (Renderer renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            renderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, tint);
            properties.SetColor(ColorId, tint);
            renderer.SetPropertyBlock(properties);
            properties.Clear();

        }

    }

    public void Arm()
    {
        armed = true;
        fired = false;
        RefreshVisual();

    }

    public void Disarm()
    {
        if (!IsArmed)
        {

            return;

        }

        armed = false;
        RefreshVisual();
        onDisarmed.Invoke();

    }

    // Direct UnityEvent receiver. Works from levers, chests, plates or mission events.
    public void Fire()
    {
        TryFire(null, false);

    }

    // Called only on the ending faction's turn for persistent hazards.
    public void FireForFaction(FactionData faction)
    {
        if (faction != null)
        {
            TryFire(faction, true);

        }

    }

    private bool TryFire(FactionData onlyFaction, bool requireOccupant)
    {
        if (!IsArmed || firing || pendingImpact || !ResolveAnchor())
        {

            return false;

        }

        List<UnitController> targets = CollectTargets(onlyFaction);
        if (requireOccupant && targets.Count == 0)
        {

            return false;

        }

        firing = true;
        try
        {
            fired = true;
            Reveal();
            if (impactDelaySeconds > 0f)
            {
                pendingImpact = true;
                StartCoroutine(ResolveDelayedImpact(onlyFaction));

            }
            else
            {
                ApplyTargets(targets);

            }

            onFired.Invoke();

        }
        finally
        {
            firing = false;

        }

        return true;

    }

    private IEnumerator ResolveDelayedImpact(FactionData onlyFaction)
    {

        yield return new WaitForSeconds(impactDelaySeconds);
        pendingImpact = false;
        if (ResolveAnchor())
        {
            ApplyTargets(CollectTargets(onlyFaction));

        }

    }

    private void ApplyTargets(List<UnitController> targets)
    {
        foreach (UnitController target in targets)
        {
            ApplyToUnit(target);

        }

    }

    private void ApplyToUnit(UnitController unit)
    {
        if (unit == null || !unit.isActiveAndEnabled || unit.data == null ||
            unit.data.currentHealth <= 0)
        {

            return;

        }

        int damage = UnityEngine.Random.Range(Mathf.Max(0, minimumDamage),
            Mathf.Max(minimumDamage, maximumDamage) + 1);
        bool hit = false;
        if (damage > 0)
        {
            DamageRequest request = new DamageRequest(null, unit, damage, 1f, 0, 0, null,
                damageType, DamageDeliveryType.Environmental);
            if (DamageResolver.TryResolve(request, out DamageResult result))
            {
                CombatActionExecutor.PublishResolvedEffectDamage(result);
                hit = true;

            }

        }

        if (unit.isActiveAndEnabled && unit.data.currentHealth > 0)
        {
            UnitStatusController statuses = unit.GetComponent<UnitStatusController>();
            if (statuses != null)
            {
                foreach (TrapStatusChance entry in statusChances)
                {
                    if (entry != null && entry.status != null &&
                        (entry.chancePercent >= 100f ||
                        entry.chancePercent > 0f &&
                        UnityEngine.Random.value * 100f < entry.chancePercent))
                    {
                        hit |= statuses.TryApply(entry.status, null);

                    }

                }

            }

        }

        if (hit)
        {
            onHit.Invoke();

        }

    }

    public bool ContainsUnit(UnitController unit)
    {

        return unit != null && ContainsUnitAt(unit, unit.currentNode);

    }

    public bool ContainsUnitAt(UnitController unit, GridNode node)
    {
        if (unit == null || node == null || !ResolveAnchor() ||
            node.floorID != anchor.floorID)
        {

            return false;

        }

        if (shape == TrapAreaShape.ForwardRay)
        {

            return IsOnForwardRay(node.coords);

        }

        Vector2Int footprint = unit.footPrint;
        for (int x = 0; x < Mathf.Max(1, footprint.x); x++)
        {
            for (int z = 0; z < Mathf.Max(1, footprint.y); z++)
            {
                if (ContainsTile(node.coords + new Vector2Int(x, z)))
                {

                    return true;

                }

            }

        }

        return false;

    }

    private bool IsOnForwardRay(Vector2Int tile)
    {
        Vector2Int delta = tile - anchor.coords;
        Vector2Int forward = ForwardDirection();

        return Dot(delta, forward) >= 1 &&
            Dot(delta, forward) <= rangeInTiles &&
            delta == forward * Dot(delta, forward);

    }

    private bool ContainsTile(Vector2Int tile)
    {
        Vector2Int delta = tile - anchor.coords;
        if (shape == TrapAreaShape.Rectangle)
        {

            return delta.x >= 0 && delta.y >= 0 &&
                delta.x < Mathf.Max(1, areaSizeInTiles.x) &&
                delta.y < Mathf.Max(1, areaSizeInTiles.y);

        }

        if (shape == TrapAreaShape.Radius)
        {

            return delta.sqrMagnitude <= radiusInTiles * radiusInTiles;

        }

        Vector2Int forward = ForwardDirection();
        Vector2Int right = new Vector2Int(forward.y, -forward.x);
        int along = Dot(delta, forward);
        int side = Dot(delta, right);

        return along >= 1 && along <= rangeInTiles &&
            side >= -(stripWidthInTiles - 1) / 2 &&
            side <= stripWidthInTiles / 2;

    }

    private static int Dot(Vector2Int a, Vector2Int b)
    {

        return a.x * b.x + a.y * b.y;

    }

    private Vector2Int ForwardDirection()
    {
        Vector3 direction = transform.forward;

        return Mathf.Abs(direction.x) > Mathf.Abs(direction.z)
            ? new Vector2Int(direction.x >= 0f ? 1 : -1, 0)
            : new Vector2Int(0, direction.z >= 0f ? 1 : -1);

    }

    // Uses the same grid shapes and shooter blockers as trap damage targeting.
    public void GetHitAreaNodes(List<GridNode> result)
    {
        result.Clear();
        if (!ResolveAnchor())
        {

            return;

        }

        if (shape != TrapAreaShape.ForwardRay)
        {
            foreach (GridNode node in grid.GetGrid(anchor.floorID).Values)
            {
                if (ContainsTile(node.coords))
                {
                    result.Add(node);

                }

            }

            return;

        }

        Vector2Int forward = ForwardDirection();
        Occupancy occupancy = FindAnyObjectByType<Occupancy>();
        UnitController[] units = stopRayAtFirstUnit
            ? FindObjectsByType<UnitController>() : null;
        GridNode previous = anchor;
        for (int step = 1; step <= rangeInTiles; step++)
        {
            GridNode node = grid.GetNodeFromGridPosition(anchor.floorID,
                anchor.coords + forward * step);
            if (node == null || occupancy != null &&
                (occupancy.IsBlockedByEdgeObstacle(previous, node) ||
                 occupancy.IsBlockedBySolidObstacle(node)))
            {

                break;

            }

            result.Add(node);
            if (stopRayAtFirstUnit)
            {
                bool foundTarget = false;
                foreach (UnitController unit in units)
                {
                    if (IsValidTarget(unit, null) && IsOnTile(unit, node.coords))
                    {
                        foundTarget = true;

                        break;

                    }

                }

                if (foundTarget)
                {

                    break;

                }

            }

            previous = node;

        }

    }

    private List<UnitController> CollectTargets(FactionData onlyFaction)
    {
        UnitController[] units = FindObjectsByType<UnitController>();
        List<UnitController> targets = new List<UnitController>();
        if (shape == TrapAreaShape.ForwardRay)
        {
            Vector2Int forward = ForwardDirection();
            Occupancy occupancy = FindAnyObjectByType<Occupancy>();
            GridNode previous = anchor;
            for (int step = 1; step <= rangeInTiles; step++)
            {
                GridNode node = grid.GetNodeFromGridPosition(anchor.floorID,
                    anchor.coords + forward * step);
                if (node == null || occupancy != null &&
                    (occupancy.IsBlockedByEdgeObstacle(previous, node) ||
                     occupancy.IsBlockedBySolidObstacle(node)))
                {

                    break;

                }

                foreach (UnitController unit in units)
                {
                    if (IsValidTarget(unit, onlyFaction) && IsOnTile(unit, node.coords) &&
                        !targets.Contains(unit))
                    {
                        targets.Add(unit);
                        if (stopRayAtFirstUnit)
                        {

                            return targets;

                        }

                    }

                }

                previous = node;

            }

        }
        else
        {
            foreach (UnitController unit in units)
            {
                if (IsValidTarget(unit, onlyFaction) && ContainsUnit(unit))
                {
                    targets.Add(unit);

                }

            }

        }

        return targets;

    }

    private bool IsValidTarget(UnitController unit, FactionData onlyFaction)
    {
        if (unit == null || !unit.isActiveAndEnabled || unit.data == null ||
            unit.data.currentHealth <= 0 || unit.currentNode == null ||
            unit.currentNode.floorID != anchor.floorID)
        {

            return false;

        }

        UnitFaction faction = unit.GetComponent<UnitFaction>();
        if (onlyFaction != null && (faction == null || faction.Faction != onlyFaction))
        {

            return false;

        }

        return affectedFactions.Count == 0 || faction != null &&
            affectedFactions.Contains(faction.Faction);

    }

    private static bool IsOnTile(UnitController unit, Vector2Int tile)
    {
        Vector2Int delta = tile - unit.currentNode.coords;

        return delta.x >= 0 && delta.y >= 0 &&
            delta.x < Mathf.Max(1, unit.footPrint.x) &&
            delta.y < Mathf.Max(1, unit.footPrint.y);

    }

    private bool ResolveAnchor()
    {
        if (anchor != null)
        {

            return true;

        }

        grid = FindAnyObjectByType<GridManager>();
        if (grid == null)
        {

            return false;

        }

        int floor = floorId;
        if (floor < 0)
        {
            GridNode nearest = grid.GetNodeFromClosestWorldPosition(transform.position);
            if (nearest == null)
            {

                return false;

            }

            floor = nearest.floorID;

        }

        Dictionary<Vector2Int, GridNode> nodes = grid.GetGrid(floor);
        float distance = float.MaxValue;
        if (nodes == null)
        {

            return false;

        }

        foreach (GridNode node in nodes.Values)
        {
            float score = new Vector2(node.cellWorldPosition.x - transform.position.x,
                node.cellWorldPosition.z - transform.position.z).sqrMagnitude;
            if (score < distance)
            {
                distance = score;
                anchor = node;

            }

        }

        return anchor != null;

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.8f);
        float cell = 2f;
        float size = shape == TrapAreaShape.Radius ? radiusInTiles * cell * 2f : cell;
        Gizmos.DrawWireCube(transform.position, new Vector3(size, 0.1f, size));
        if (shape == TrapAreaShape.ForwardRay || shape == TrapAreaShape.ForwardStrip)
        {
            Gizmos.DrawRay(transform.position, transform.forward * rangeInTiles * cell);

        }

    }

}
