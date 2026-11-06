using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Presentation only. MissionObjectiveManager remains responsible for objective progress.
[DisallowMultipleComponent]
public sealed class MissionObjectiveWorldMarkers : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MissionObjectiveManager missionManager;
    [SerializeField] private MissionObjectiveWorldMarkerUI markerPrefab;

    [Header("Existing Mission Icon Sprites")]
    [SerializeField] private Sprite eliminateIcon;
    [SerializeField] private Sprite priorityTargetIcon;
    [SerializeField] private Sprite interactIcon;
    [SerializeField] private Sprite rescueIcon;
    [SerializeField] private Sprite extractionIcon;

    [Header("Visibility")]
    [SerializeField] private bool showOptionalObjectives = true;
    [Tooltip("Your existing InputSystem_Actions asset. The action UI/ShowOptionalObjectives is set up by the one-time Editor command.")]
    [SerializeField] private InputActionAsset optionalRevealInputActions;
    [SerializeField, Min(0.1f)] private float rescanInterval = 0.35f;

    [Header("Position / Animation")]
    [Tooltip("Clearance between the top of a unit's model and the bottom of its banner.")]
    [SerializeField, Min(0f)] private float unitHeight = 1.65f;
    [Tooltip("Clearance over an interactable without a unit model.")]
    [SerializeField, Min(0f)] private float interactableHeight = 1.9f;
    [SerializeField, Min(0f)] private float extractionHeight = 2.1f;
    [Tooltip("Move banners toward the camera so they render in front of floating health bars. The tether still points to the target.")]
    [SerializeField, Min(0f)] private float cameraForwardOffset = 1.0f;
    [SerializeField] private bool showTetherLines = true;
    [SerializeField, Min(0.005f)] private float tetherWidth = 0.035f;
    [SerializeField, Min(0.0001f)] private float canvasScale = 0.0065f;
    [SerializeField, Min(0f)] private float bobAmplitude = 0.08f;
    [SerializeField, Min(0f)] private float bobSpeed = 1.8f;

    private readonly Dictionary<MarkerKey, MissionObjectiveWorldMarkerUI> activeMarkers = new();
    private readonly Dictionary<EntityId, MarkerCandidate> winners = new();
    private readonly HashSet<MarkerKey> desiredMarkers = new();
    private readonly List<MarkerKey> staleKeys = new();
    private float nextRescan;
    private bool dirty = true;
    private bool warnedAboutPrefab;
    private bool optionalKeyHeld;
    private InputAction optionalRevealAction;
    private bool enabledRevealActionHere;
    private bool warnedAboutRevealAction;

    private readonly struct MarkerKey : IEquatable<MarkerKey>
    {
        public readonly string ObjectiveId;
        public readonly EntityId TargetId;

        public MarkerKey(string objectiveId, Component host)
        {
            ObjectiveId = objectiveId;
            TargetId = host.GetEntityId();

        }

        public bool Equals(MarkerKey other) =>
            TargetId == other.TargetId && StringComparer.OrdinalIgnoreCase.Equals(ObjectiveId, other.ObjectiveId);

        public override bool Equals(object obj) => obj is MarkerKey other && Equals(other);
        public override int GetHashCode() =>
            (StringComparer.OrdinalIgnoreCase.GetHashCode(ObjectiveId ?? string.Empty) * 397) ^ TargetId.GetHashCode();
    }

    private readonly struct MarkerCandidate
    {
        public readonly MissionObjectiveEntry Objective;
        public readonly Component Host;
        public readonly float Height;

        public MarkerCandidate(MissionObjectiveEntry objective, Component host, float height)
        {
            Objective = objective;
            Host = host;
            Height = height;

        }
    }

    private void OnEnable()
    {
        TryBindManager();
        BindOptionalRevealAction();
        dirty = true;

    }

    private void BindOptionalRevealAction()
    {
        optionalRevealAction = optionalRevealInputActions != null
            ? optionalRevealInputActions.FindAction("UI/ShowOptionalObjectives", false)
            : null;

        if (optionalRevealAction == null)
        {
            if (!warnedAboutRevealAction)
            {
                Debug.LogWarning("[ObjectiveMarkers] Optional reveal input is not wired. Run " +
                    "Tools > MutaBorn > Objective Markers > Connect Optional Reveal Input. " +
                    "Primary banners still work.", this);
                warnedAboutRevealAction = true;

            }

            return;

        }

        // Only disable actions this component personally enabled. Another system
        // may already be using the same UI action map.
        enabledRevealActionHere = !optionalRevealAction.enabled;
        if (enabledRevealActionHere)
        {
            optionalRevealAction.Enable();

        }

    }

    private void Update()
    {
        // Read the configured Input Action every frame, even when rescans are throttled.
        // Releasing the control immediately restores primary markers on shared targets.
        bool isHeld = showOptionalObjectives && optionalRevealAction != null &&
                      optionalRevealAction.enabled && optionalRevealAction.IsPressed();
        if (optionalKeyHeld != isHeld)
        {
            optionalKeyHeld = isHeld;
            dirty = true;

        }

        if (missionManager == null)
        {
            TryBindManager();

        }

        if (!dirty && Time.unscaledTime < nextRescan)
        {
            return;

        }

        dirty = false;
        nextRescan = Time.unscaledTime + rescanInterval;
        RefreshMarkers();

    }

    private void TryBindManager()
    {
        MissionObjectiveManager found = missionManager != null ? missionManager : MissionObjectiveManager.Instance;
        if (found == null)
        {
            found = FindAnyObjectByType<MissionObjectiveManager>();

        }

        if (found == missionManager && found != null)
        {
            found.ObjectivesChanged -= MarkDirty;
            found.ObjectivesChanged += MarkDirty;
            return;

        }

        if (missionManager != null)
        {
            missionManager.ObjectivesChanged -= MarkDirty;

        }

        missionManager = found;
        if (missionManager != null)
        {
            missionManager.ObjectivesChanged += MarkDirty;

        }

        dirty = true;

    }

    public void BindManager(MissionObjectiveManager manager)
    {
        if (missionManager != null)
        {
            missionManager.ObjectivesChanged -= MarkDirty;

        }

        missionManager = manager;
        if (missionManager != null)
        {
            missionManager.ObjectivesChanged += MarkDirty;

        }

        dirty = true;

    }

    private void MarkDirty()
    {
        dirty = true;

    }

    public void RefreshMarkers()
    {
        winners.Clear();
        desiredMarkers.Clear();

        if (markerPrefab == null)
        {
            if (!warnedAboutPrefab)
            {
                Debug.LogWarning("[ObjectiveMarkers] Missing marker prefab. Build/assign the editable ObjectiveWorldMarker prefab.", this);
                warnedAboutPrefab = true;

            }

            RemoveStaleMarkers();
            return;

        }

        if (missionManager != null && missionManager.Status == MissionStatus.InProgress)
        {
            foreach (MissionObjectiveEntry objective in missionManager.Objectives)
            {
                if (objective == null || !objective.Visible || objective.Status != MissionObjectiveStatus.Active ||
                    (objective.Optional && !optionalKeyHeld))
                {
                    continue;

                }

                CollectCandidates(objective);

            }

            foreach (MarkerCandidate winner in winners.Values)
            {
                ShowMarker(winner);

            }

        }

        RemoveStaleMarkers();

    }

    private void CollectCandidates(MissionObjectiveEntry objective)
    {
        if (objective.Type == MissionObjectiveType.ExtractPlayerUnits)
        {
            foreach (MissionExtractionZone zone in FindObjectsByType<MissionExtractionZone>())
            {
                if (zone != null && zone.isActiveAndEnabled && zone.MatchesZoneId(objective.Template.TargetKey) &&
                    zone.TryGetWorldMarkerPosition(out _))
                {
                    ConsiderCandidate(new MarkerCandidate(objective, zone, extractionHeight));

                }

            }

            return;

        }

        foreach (Component target in objective.PendingTargets)
        {
            if (target == null || !target.gameObject.activeInHierarchy ||
                target is Behaviour behaviour && !behaviour.isActiveAndEnabled)
            {
                continue;

            }

            UnitController unit = target.GetComponentInParent<UnitController>();
            if (unit != null && (unit.data == null || unit.data.currentHealth <= 0))
            {
                continue;

            }

            // Mark the same visual host only once: an interactable ON a unit
            // and an elimination of that unit should not stack two banners.
            Component host = unit != null ? unit : target;
            ConsiderCandidate(new MarkerCandidate(objective, host,
                unit != null ? unitHeight : interactableHeight));

        }

    }

    private void ConsiderCandidate(MarkerCandidate candidate)
    {
        EntityId id = candidate.Host.GetEntityId();
        if (!winners.TryGetValue(id, out MarkerCandidate existing) ||
            IsBetterCandidate(candidate.Objective, existing.Objective))
        {
            winners[id] = candidate;

        }

    }

    private static bool IsBetterCandidate(MissionObjectiveEntry candidate, MissionObjectiveEntry existing)
    {
        // Level-specific explicit priority always wins (including negative values).
        if (candidate.WorldMarkerPriority != existing.WorldMarkerPriority)
        {
            return candidate.WorldMarkerPriority > existing.WorldMarkerPriority;

        }

        // With equal priority, an exact target is more informative than a broad
        // "Eliminate Hostiles" match, regardless of iteration order.
        int candidateSpecificity = GetTargetSpecificity(candidate);
        int existingSpecificity = GetTargetSpecificity(existing);
        if (candidateSpecificity != existingSpecificity)
        {
            return candidateSpecificity > existingSpecificity;

        }

        if (candidate.Optional != existing.Optional)
        {
            return !candidate.Optional;

        }

        return StringComparer.OrdinalIgnoreCase.Compare(candidate.Id, existing.Id) < 0;

    }

    private static int GetTargetSpecificity(MissionObjectiveEntry objective)
    {
        switch (objective.Template.TargetFilter)
        {
            case MissionTargetFilter.TargetId: return 60;
            case MissionTargetFilter.PrefabTypeId: return 50;
            case MissionTargetFilter.CategoryId: return 40;
            case MissionTargetFilter.SpecificFaction: return 30;
            case MissionTargetFilter.HostileToPlayer: return 20;
            default: return 10;

        }

    }

    private void ShowMarker(MarkerCandidate candidate)
    {
        MissionObjectiveEntry objective = candidate.Objective;
        MarkerKey key = new MarkerKey(objective.Id, candidate.Host);
        desiredMarkers.Add(key);

        if (!activeMarkers.TryGetValue(key, out MissionObjectiveWorldMarkerUI marker) || marker == null)
        {
            marker = Instantiate(markerPrefab, transform);
            marker.name = "Objective Marker - " + objective.Id + " - " + candidate.Host.name;
            activeMarkers[key] = marker;

        }

        marker.Configure(candidate.Host, objective.Title, objective.Optional, GetIcon(objective),
            candidate.Height, canvasScale, bobAmplitude, bobSpeed,
            cameraForwardOffset, showTetherLines, tetherWidth);

    }

    private Sprite GetIcon(MissionObjectiveEntry objective)
    {
        if (objective.Template.WorldMarkerIcon != null)
        {
            return objective.Template.WorldMarkerIcon;

        }

        switch (objective.Type)
        {
            case MissionObjectiveType.EliminateUnits:
                return objective.Template.TargetFilter == MissionTargetFilter.TargetId && priorityTargetIcon != null
                    ? priorityTargetIcon : eliminateIcon;

            case MissionObjectiveType.InteractWithTargets:
                return string.Equals(objective.Template.InteractionId, "help", StringComparison.OrdinalIgnoreCase) && rescueIcon != null
                    ? rescueIcon : interactIcon;

            case MissionObjectiveType.ExtractPlayerUnits:
                return extractionIcon;

            default:
                return null;

        }

    }

    private void RemoveStaleMarkers()
    {
        staleKeys.Clear();
        foreach (KeyValuePair<MarkerKey, MissionObjectiveWorldMarkerUI> item in activeMarkers)
        {
            if (desiredMarkers.Contains(item.Key) && item.Value != null)
            {
                continue;

            }

            if (item.Value != null)
            {
                Destroy(item.Value.gameObject);

            }

            staleKeys.Add(item.Key);

        }

        foreach (MarkerKey key in staleKeys)
        {
            activeMarkers.Remove(key);

        }

    }

    private void OnDisable()
    {
        if (enabledRevealActionHere && optionalRevealAction != null)
        {
            optionalRevealAction.Disable();

        }

        enabledRevealActionHere = false;
        optionalRevealAction = null;
        optionalKeyHeld = false;

        if (missionManager != null)
        {
            missionManager.ObjectivesChanged -= MarkDirty;

        }

        foreach (MissionObjectiveWorldMarkerUI marker in activeMarkers.Values)
        {
            if (marker != null)
            {
                Destroy(marker.gameObject);

            }

        }

        activeMarkers.Clear();
        winners.Clear();
        dirty = true;

    }
}
