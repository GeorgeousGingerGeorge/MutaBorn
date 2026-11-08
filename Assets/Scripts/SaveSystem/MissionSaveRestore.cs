using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public static class MissionSaveRestore
{
    public static bool TryRestorePosition(UnitController unit, UnitSaveData savedUnit, GridManager gridManager)
    {
        if (unit == null || savedUnit == null || gridManager == null)
        {
            return false;
        }

        if (unit.currentNode == null || unit.occupancy == null)
        {
            return false;
        }

        GridNode targetNode = gridManager.GetNodeFromGridPosition(savedUnit.floorId, savedUnit.nodeCoords);

        if (targetNode == null)
        {
            Debug.LogWarning($"Saved node not found for {savedUnit.unitName}.");
            return false;
        }

        if (!unit.occupancy.IsFootPrintMovable(targetNode, unit))
        {
            Debug.LogWarning($"Saved position is currently blocked for {savedUnit.unitName}.");
            return false;
        }

        unit.SetNode(targetNode);
        
        int apDifference = savedUnit.currentActionPoints - unit.data.currentActionPoints;
        int healthDifference = savedUnit.currentHealth - unit.data.currentHealth;
        int manaDifference = savedUnit.currentMana - unit.data.currentMana;

        unit.data.ModifyActionPoints(apDifference);
        unit.data.ModifyHealth(healthDifference);
        unit.data.ModifyMana(manaDifference);
        unit.transform.SetPositionAndRotation(savedUnit.position, savedUnit.rotation);

        Debug.Log($"Restored position for {savedUnit.unitName}.");

        return true;
    }
    public static Dictionary<string, UnitController> GetCurrentUnits()
    {
        Dictionary<string, UnitController> unitLookup = new Dictionary<string, UnitController>();

        UnitController[] units = Object.FindObjectsByType<UnitController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (UnitController unit in units)
        {
            if (unit.data == null)
                continue;

            MissionUnitIdentity identity = unit.GetComponent<MissionUnitIdentity>();

            string persistentId = identity != null ? identity.PersistentId : "";

            if (string.IsNullOrEmpty(persistentId) &&
                unit.InitializationMode != UnitInitializationMode.LegacyScene)
            {
                persistentId = $"CrewSlot_{unit.RosterDeploymentSlot}";
            }

            if (string.IsNullOrEmpty(persistentId))
                continue;

            if (unitLookup.ContainsKey(persistentId))
            {
                Debug.LogError($"Duplicate unit identity detected: {persistentId}");
                return null;
            }

            unitLookup.Add(persistentId, unit);
        }

        return unitLookup;
    }

    public static bool ValidateSavedPositions(MissionSaveData saved, GridManager gridManager)
    {
        if (saved == null || saved.units == null || gridManager == null)
        {
            return false;
        }

        Dictionary<string, UnitController> currentUnits = GetCurrentUnits();

        if (currentUnits == null)
        {
            return false;
        }

        HashSet<string> processedIds = new HashSet<string>();
        HashSet<GridNode> savedNodes = new HashSet<GridNode>();

        foreach (UnitSaveData savedUnit in saved.units)
        {
            if (savedUnit == null || string.IsNullOrEmpty(savedUnit.persistentId) || !processedIds.Add(savedUnit.persistentId))
            {
                Debug.LogError("Missing or duplicate saved unit identity!");
                return false;
            }

            if (!currentUnits.ContainsKey(savedUnit.persistentId))
            {
                Debug.LogError($"Unit not found: {savedUnit.persistentId}");
                return false;
            }

            GridNode targetNode = gridManager.GetNodeFromGridPosition(
                savedUnit.floorId,
                savedUnit.nodeCoords
            );

            if (targetNode == null)
            {
                Debug.LogError($"Invalid saved position: {savedUnit.unitName}");
                return false;
            }

            UnitController currentUnit = currentUnits[savedUnit.persistentId];

            for (int x = 0; x < currentUnit.footPrint.x; x++)
            {
                for (int z = 0; z < currentUnit.footPrint.y; z++)
                {
                    Vector2Int coords = savedUnit.nodeCoords + new Vector2Int(x, z);

                    GridNode footprintNode = gridManager.GetNodeFromGridPosition(
                        savedUnit.floorId,
                        coords
                    );

                    if (footprintNode == null)
                    {
                        Debug.LogError($"Invalid saved footprint: {savedUnit.unitName}");
                        return false;
                    }

                    if (currentUnit.occupancy != null &&
                        currentUnit.occupancy.IsBlockedBySolidObstacle(footprintNode))
                    {
                        Debug.LogError($"Saved position contains an obstacle: {savedUnit.unitName}");
                        return false;
                    }

                    if (!savedNodes.Add(footprintNode))
                    {
                        Debug.LogError($"Overlapping saved unit footprints: {savedUnit.unitName}");
                        return false;
                    }
                }
            }
        }

        Debug.Log($"Validated {saved.units.Count} saved unit positions!");
        return true;
    }
    private class PositionRestoreEntry
    {
        public UnitController unit;
        public GridNode originalNode;
        public GridNode savedNode;
        public Vector3 originalPosition;
        public Quaternion originalRotation;
        public Vector3 savedPosition;
        public Quaternion savedRotation;
    }

    public static bool TryRestoreAllPositions(
        MissionSaveData saved,
        GridManager gridManager)
    {
        if (saved == null || gridManager == null ||
            saved.sceneName != SceneManager.GetActiveScene().name)
        {
            Debug.LogError("Cannot restore positions: invalid save or scene.");
            return false;
        }

        if (!ValidateSavedPositions(saved, gridManager))
            return false;

        Dictionary<string, UnitController> currentUnits = GetCurrentUnits();

        if (currentUnits == null || currentUnits.Count != saved.units.Count)
        {
            Debug.LogError("Current units do not match the saved unit count.");
            return false;
        }

        List<PositionRestoreEntry> restoreEntries =
            new List<PositionRestoreEntry>();

        foreach (UnitSaveData savedUnit in saved.units)
        {
            UnitController unit = currentUnits[savedUnit.persistentId];

            if (unit.currentNode == null || unit.occupancy == null ||
                !unit.isActiveAndEnabled || !savedUnit.isActive ||
                savedUnit.currentHealth <= 0)
            {
                Debug.LogError($"Unit is not ready for position restoration: {savedUnit.unitName}");
                return false;
            }

            restoreEntries.Add(new PositionRestoreEntry
            {
                unit = unit,
                originalNode = unit.currentNode,
                savedNode = gridManager.GetNodeFromGridPosition(
                    savedUnit.floorId, savedUnit.nodeCoords),
                originalPosition = unit.transform.position,
                originalRotation = unit.transform.rotation,
                savedPosition = savedUnit.position,
                savedRotation = savedUnit.rotation
            });
        }

        // Clear everyone's existing occupancy before moving anyone.
        foreach (PositionRestoreEntry entry in restoreEntries)
        {
            entry.unit.occupancy.UnregisterUnit(
                entry.originalNode, entry.unit);
        }

        // Restore saved positions and register the new grid occupancy.
        foreach (PositionRestoreEntry entry in restoreEntries)
        {
            entry.unit.SetNode(entry.savedNode);

            entry.unit.transform.SetPositionAndRotation(
                entry.savedPosition, entry.savedRotation);
        }

        // Verify that every unit registered successfully.
        foreach (PositionRestoreEntry entry in restoreEntries)
        {
            if (entry.unit.occupancy.FindUnit(entry.savedNode) != entry.unit)
            {
                Debug.LogError(
                    $"Occupancy registration failed for {entry.unit.name}. Rolling back.");

                // Remove the attempted registrations.
                foreach (PositionRestoreEntry rollback in restoreEntries)
                {
                    rollback.unit.occupancy.UnregisterUnit(rollback.unit.currentNode, rollback.unit);
                }

                // Restore the original positions and occupancy.
                foreach (PositionRestoreEntry rollback in restoreEntries)
                {
                    rollback.unit.SetNode(rollback.originalNode);

                    rollback.unit.transform.SetPositionAndRotation(rollback.originalPosition, rollback.originalRotation);
                }

                return false;
            }
        }

        // Restore unit resources after successful position restoration.
        foreach (UnitSaveData savedUnit in saved.units)
        {
            UnitController unit = currentUnits[savedUnit.persistentId];

            int apDifference = savedUnit.currentActionPoints - unit.data.currentActionPoints;
            int healthDifference = savedUnit.currentHealth - unit.data.currentHealth;
            int manaDifference = savedUnit.currentMana - unit.data.currentMana;

            unit.data.ModifyActionPoints(apDifference);
            unit.data.ModifyHealth(healthDifference);
            unit.data.ModifyMana(manaDifference);
        }

        Debug.Log($"Successfully restored {restoreEntries.Count} unit positions!");
        return true;
    }
}
