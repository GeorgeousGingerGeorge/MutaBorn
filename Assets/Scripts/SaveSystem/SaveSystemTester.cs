using MutaBorn.Gameplay;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSystemTester : MonoBehaviour
{
    [ContextMenu("Test / Save Mission")]
    private void TestSaveMission()
    {
        bool success = SaveManager.SaveCurrentMission();

        Debug.Log(success ? "Mission save test successful!!! :D" : "Mission save test failed!! :(");
    }

    [ContextMenu("Test / Load Mission")]
    private void TestLoadMission()
    {
        if (SaveManager.TryLoadMission(out MissionSaveData data))
        {
            Debug.Log($"Mission loaded successfully!! LET'S RUMBLE!!!!"); // debugger message to console
            Debug.Log($"Scene: {data.sceneName}"); // loaded scene
            Debug.Log($"Saved Units: {data.units?.Count ?? 0}"); // total units counted when attempting to LOAD the save file
            Debug.Log($"Save Time: {data.saveTimeUTC}"); // time stamp of the SAVED FILE
        }
        else
        {
            Debug.LogError("Mission load test failed! Maybe... Try that again.");
        }
    }

    [ContextMenu("Test / Match Saved Units")]
    private void TestMatchSavedUnits()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved))
        {
            Debug.LogError("No mission save found!");
            return;
        }

        MissionSaveData current = MissionSaveCapture.Capture();

        if (saved.sceneName != current.sceneName)
        {
            Debug.LogError("Saved scene does not match current scene!");
            return;
        }

        HashSet<string> currentIds = new HashSet<string>();

        foreach (UnitSaveData unit in current.units)
        {
            if (!string.IsNullOrEmpty(unit.persistentId))
                currentIds.Add(unit.persistentId);
        }

        int matched = 0;

        foreach (UnitSaveData unit in saved.units)
        {
            if (currentIds.Contains(unit.persistentId))
            {
                matched++;
            }
            else
            {
                Debug.LogWarning($"Missing unit: {unit.unitName} ({unit.persistentId})");
            }
        }

        Debug.Log($"Identity Match Test: {matched}/{saved.units.Count} units matched!");
    }

    [ContextMenu("Test / Restore Goblino Position")]
    private void TestRestoreGoblino()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved))
        {
            Debug.LogError("No mission save found!");
            return;
        }

        if (saved.sceneName != SceneManager.GetActiveScene().name)
        {
            Debug.LogError("Save belongs to a different scene!");
            return;
        }

        UnitSaveData savedUnit = saved.units.Find(
            unit => unit.persistentId == "CrewSlot_0"
        );

        if (savedUnit == null)
        {
            Debug.LogError("Goblino not found in save data!");
            return;
        }

        UnitController[] units = FindObjectsByType<UnitController>(FindObjectsInactive.Include);

        foreach (UnitController unit in units)
        {
            if (unit.data != null &&
                unit.InitializationMode != UnitInitializationMode.LegacyScene &&
                unit.RosterDeploymentSlot == 0)
            {
                GridManager gridManager = FindAnyObjectByType<GridManager>();

                bool restored = MissionSaveRestore.TryRestorePosition(unit, savedUnit, gridManager);

                //if (restored)
                //{
                //    int apDifference = savedUnit.currentActionPoints - unit.data.currentActionPoints;

                //    unit.data.ModifyActionPoints(apDifference);

                //    Debug.Log($"Restored AP: {unit.data.currentActionPoints}");
                //}

                Debug.Log($"Goblino restoration successful: {restored}");
                return;
            }
        }

        Debug.LogError("Could not find Goblino in the current mission!");
    }

    [ContextMenu("Test / Current Unit Lookup")]
    private void TestCurrentUnitLookup()
    {
        Dictionary<string, UnitController> units =
            MissionSaveRestore.GetCurrentUnits();

        if (units == null)
        {
            Debug.LogError("Unit lookup failed! Duplicate IDs detected.");
            return;
        }

        Debug.Log($"Unit Lookup Successful! Found {units.Count} units.");
    }

    [ContextMenu("Test / Validate Saved Positions")]
    private void TestValidateSavedPositions()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved))
        {
            Debug.LogError("No save file found!");
            return;
        }

        GridManager gridManager = FindAnyObjectByType<GridManager>();

        bool valid = MissionSaveRestore.ValidateSavedPositions(
            saved,
            gridManager
        );

        Debug.Log($"Saved Position Validation: {valid}");
    }

    [ContextMenu("Test / Restore All Unit Positions")]
    private void TestRestoreAllUnitPositions()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved))
        {
            Debug.LogError("No mission save found!");
            return;
        }

        GridManager gridManager = FindAnyObjectByType<GridManager>();

        bool restored = MissionSaveRestore.TryRestoreAllPositions(
            saved,
            gridManager
        );

        Debug.Log($"Full Mission Position Restoration: {restored}");
    }

    [ContextMenu("Test / Restore Turn State")]
    private void TestRestoreTurnState()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved))
        {
            Debug.LogError("No mission save found!");
            return;
        }

        if (saved.sceneName != UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
        {
            Debug.LogError("Save belongs to a different scene!");
            return;
        }

        TurnManager turnManager = TurnManager.Instance;

        if (turnManager == null)
        {
            Debug.LogError("TurnManager not found!");
            return;
        }

        bool restored = turnManager.TryRestoreTurnState(saved.turnState);

        Debug.Log($"Turn Restoration Successful: {restored}");

        if (restored)
        {
            Debug.Log($"Current Round: {turnManager.Round}");
            Debug.Log($"Active Faction: {turnManager.ActiveFaction.Id}");
        }
    }

    [ContextMenu("Test / Restore Mission Snapshot")]
    private void TestRestoreMissionSnapshot()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved))
        {
            Debug.LogError("No valid mission save found!");
            return;
        }

        TurnManager turnManager = TurnManager.Instance;
        GridManager gridManager = FindAnyObjectByType<GridManager>();

        MissionObjectiveManager objectiveManager = MissionObjectiveManager.Instance;

        if (objectiveManager == null || saved.objectiveState == null)
        {
            Debug.LogError("Cannot restore mission: Objective state or manager missing.");
            return;
        }

        if (turnManager == null || gridManager == null || saved.turnState == null || turnManager.PlayerFaction == null)
        {
            Debug.LogError("Missing required mission restoration components!");
            return;
        }

        if (!turnManager.IsPlayerTurn || !string.Equals(saved.turnState.activeFactionId, turnManager.PlayerFaction.Id, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("Mission restoration currently supports player turns only.");
            return;
        }

        // Restore all saved unit states.
        if (!MissionSaveRestore.TryRestoreAllPositions(saved, gridManager))
        {
            Debug.LogError("Mission restoration failed during unit restoration!");
            return;
        }

        // Restore saved turn progression.
        if (!turnManager.TryRestoreTurnState(saved.turnState))
        {
            Debug.LogError("Turn restoration failed! Unit states were restored, but the mission is only partially restored.");
            return;
        }
        // Restore objective progression
        if (!objectiveManager.TryRestoreObjectiveState(saved.objectiveState))
        {
            Debug.LogError("Objective restoration failed! Unit and turn states were restored, but the mission is only partially restored.");
            return;
        }

        Debug.Log($"Mission Snapshot Restored Successfully! Round: {turnManager.Round} | Active Faction: {turnManager.ActiveFaction.Id}");
    }

    [ContextMenu("Test / Verify Saved Roster")]
    private void TestVerifySavedRoster()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved) || saved.rosterState == null)
        {
            Debug.LogError("[Save/Load] No valid roster data found!");
            return;
        }

        int members = saved.rosterState.members?.Count ?? 0;
        int deployments = saved.rosterState.deployments?.Count ?? 0;

        Debug.Log($"[Save/Load] JSON verified: {members} roster members and {deployments} deployments.");
    }

    [ContextMenu("Test / Verify Saved Race Lookups")]
    private void TestVerifySavedRaceLookups()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved) || saved.rosterState == null || saved.rosterState.members == null)
        {
            Debug.LogError("[Save/Load] Saved roster data not found.");
            return;
        }

        GameDataBase database = GameDataBaseLoader.Instance;
        RaceDataBase raceDatabase = database != null ? database.raceData : null;

        if (raceDatabase == null)
        {
            Debug.LogError("[Save/Load] Race database could not be loaded.");
            return;
        }

        int resolved = 0;

        foreach (UnitRosterMemberSaveData member in saved.rosterState.members)
        {
            DynamicStats race = raceDatabase.GetRace(member.raceName);

            if (race == null)
            {
                Debug.LogError($"[Save/Load] Failed to resolve race '{member.raceName}' for {member.characterName}.");
                continue;
            }

            resolved++;
        }

        Debug.Log($"[Save/Load] Race lookup verified: {resolved}/{saved.rosterState.members.Count} roster members.");
    }

    [ContextMenu("Test / Verify Unit Reconstruction")]
    private void TestVerifyUnitReconstruction()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved) || saved.rosterState?.members == null)
        {
            Debug.LogError("[Save/Load] Saved roster members not found.");
            return;
        }

        RaceDataBase raceDatabase = GameDataBaseLoader.Instance?.raceData;

        if (raceDatabase == null)
        {
            Debug.LogError("[Save/Load] Race database not found.");
            return;
        }

        int reconstructed = 0;

        foreach (UnitRosterMemberSaveData member in saved.rosterState.members)
        {
            DynamicStats race = raceDatabase.GetRace(member.raceName);
            UnitData unit = UnitData.CreateFromSavedIdentity(member, race);

            if (unit == null)
            {
                Debug.LogError($"[Save/Load] Failed to reconstruct {member.characterName}.");
                continue;
            }

            if (unit.UnitId != member.unitId || unit.Level != member.level || unit.Experience != member.experience || unit.UnspentAttributePoints != member.unspentAttributePoints || unit.MutationPoints != member.mutationPoints)
            {
                Debug.LogError($"[Save/Load] Identity or progression mismatch for {member.characterName}.");
                continue;
            }

            reconstructed++;
        }

        Debug.Log($"[Save/Load] Unit reconstruction verified: {reconstructed}/{saved.rosterState.members.Count} characters.");
    }

    [ContextMenu("Test / Verify Saved Mutations")]
    private void TestVerifySavedMutations()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved) || saved.rosterState?.members == null || UnitRoster.Instance == null)
        {
            Debug.LogError("[Save/Load] Saved mutation data or live roster not found.");
            return;
        }

        int verified = 0;

        foreach (UnitRosterMemberSaveData member in saved.rosterState.members)
        {
            if (!UnitRoster.Instance.TryGetUnit(member.unitId, out UnitData unit))
            {
                Debug.LogError($"[Save/Load] Roster member not found: {member.characterName}");
                continue;
            }

            int fixedCount = 0;
            int legacyCount = 0;
            bool matches = true;

            foreach (ActiveMutationRecord mutation in unit.activeMutationNodes)
            {
                if (mutation == null || mutation.MutationId == MutationID.none) continue;

                fixedCount++;

                if (!member.fixedMutationIds.Contains(mutation.MutationId))
                {
                    matches = false;
                }
            }

            foreach (BaseMutation mutation in unit.activeMutations.Values)
            {
                if (mutation == null || mutation.ID == MutationID.none) continue;

                legacyCount++;

                if (!member.legacyMutationIds.Contains(mutation.ID))
                {
                    matches = false;
                }
            }

            if (!matches || fixedCount != member.fixedMutationIds.Count || legacyCount != member.legacyMutationIds.Count)
            {
                Debug.LogError($"[Save/Load] Mutation mismatch for {member.characterName}.");
                continue;
            }

            verified++;
        }

        Debug.Log($"[Save/Load] Mutation verification: {verified}/{saved.rosterState.members.Count} roster members.");
    }

    [ContextMenu("Test / Verify Reconstructed Fixed Mutations")]
    private void TestVerifyReconstructedFixedMutations()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved) || saved.rosterState?.members == null)
        {
            Debug.LogError("[Save/Load] Saved roster members not found.");
            return;
        }

        GameDataBase database = GameDataBaseLoader.Instance;
        RaceDataBase raceDatabase = database != null ? database.raceData : null;

        if (raceDatabase == null)
        {
            Debug.LogError("[Save/Load] Race database not found.");
            return;
        }

        int verified = 0;

        foreach (UnitRosterMemberSaveData member in saved.rosterState.members)
        {
            DynamicStats race = raceDatabase.GetRace(member.raceName);
            UnitData unit = UnitData.CreateFromSavedIdentity(member, race);

            if (unit == null)
            {
                Debug.LogError($"[Save/Load] Reconstruction failed for {member.characterName}.");
                continue;
            }

            int expected = member.fixedMutationIds?.Count ?? 0;

            if (unit.activeMutationNodes.Count != expected)
            {
                Debug.LogError($"[Save/Load] Mutation count mismatch for {member.characterName}.");
                continue;
            }

            bool matches = true;

            foreach (MutationID mutationId in member.fixedMutationIds)
            {
                if (!unit.activeMutationNodes.Exists(mutation => mutation != null && mutation.MutationId == mutationId))
                {
                    matches = false;
                    break;
                }
            }

            if (!matches)
            {
                Debug.LogError($"[Save/Load] Mutation ID mismatch for {member.characterName}.");
                continue;
            }

            verified++;
        }

        Debug.Log($"[Save/Load] Fixed mutation reconstruction verified: {verified}/{saved.rosterState.members.Count} characters.");
    }

    [ContextMenu("Test / Verify Equipment Registry")]
    private void TestVerifyEquipmentRegistry()
    {
        GameDataBase database = GameDataBaseLoader.Instance;
        UnitRoster roster = UnitRoster.Instance;

        if (database == null || roster == null)
        {
            Debug.LogError("[Save/Load] Game database or roster unavailable.");
            return;
        }

        int checkedItems = 0;
        int verified = 0;

        foreach (UnitData unit in roster.Units)
        {
            foreach (EquipmentInstance item in unit.equippedItems)
            {
                checkedItems++;

                string typeId = item.typeDefinition != null ? item.typeDefinition.TypeId : null;
                EquipmentType equipmentType = database.GetEquipmentType(typeId);

                if (equipmentType == null || equipmentType != item.typeDefinition || !equipmentType.TryGetTemplate(item.templateId, out EquipmentTemplate template))
                {
                    Debug.LogError($"[Save/Load] Equipment lookup failed: {unit.characterName} / {item.itemName}.");
                    continue;
                }

                verified++;
            }
        }

        if (checkedItems == 0)
        {
            Debug.LogWarning("[Save/Load] No equipped items found to verify.");
            return;
        }

        Debug.Log($"[Save/Load] Equipment registry verified: {verified}/{checkedItems} equipped items.");
    }

    [ContextMenu("Test / Verify Saved Equipment")]
    private void TestVerifySavedEquipment()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved) || saved.rosterState?.members == null)
        {
            Debug.LogError("[Save/Load] Saved equipment data not found.");
            return;
        }

        GameDataBase database = GameDataBaseLoader.Instance;

        if (database == null)
        {
            Debug.LogError("[Save/Load] Game database unavailable.");
            return;
        }

        int total = 0;
        int verified = 0;
        HashSet<string> instanceIds = new HashSet<string>();

        foreach (UnitRosterMemberSaveData member in saved.rosterState.members)
        {
            if (member.equipment == null)
            {
                Debug.LogError($"[Save/Load] Equipment list missing for {member.characterName}.");
                continue;
            }

            foreach (EquipmentSaveData item in member.equipment)
            {
                total++;

                if (string.IsNullOrWhiteSpace(item.instanceId) || !instanceIds.Add(item.instanceId))
                {
                    Debug.LogError($"[Save/Load] Missing or duplicate equipment ID for {member.characterName}.");
                    continue;
                }

                EquipmentType type = database.GetEquipmentType(item.typeId);

                if (type == null || !type.TryGetTemplate(item.templateId, out EquipmentTemplate template))
                {
                    Debug.LogError($"[Save/Load] Saved equipment definition not found for {member.characterName}.");
                    continue;
                }

                verified++;
            }
        }

        Debug.Log($"[Save/Load] Saved equipment verified: {verified}/{total} items.");
    }

    [ContextMenu("Test / Verify Saved Equipment Slots")]
    private void TestVerifySavedEquipmentSlots()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved) || saved.rosterState?.members == null)
        {
            Debug.LogError("[Save/Load] Saved roster data not found.");
            return;
        }

        int total = 0;
        int verified = 0;

        foreach (UnitRosterMemberSaveData member in saved.rosterState.members)
        {
            if (member.equipment == null)
            {
                continue;
            }

            foreach (EquipmentSaveData item in member.equipment)
            {
                total++;

                if (item.equippedSlot == UnitSlotId.None)
                {
                    Debug.LogError($"[Save/Load] Missing equipment slot for {member.characterName}: {item.instanceId}.");
                    continue;
                }

                verified++;
            }
        }

        Debug.Log($"[Save/Load] Equipment slots verified: {verified}/{total} items.");
    }

    [ContextMenu("Test / Verify Equipment Slot Mapping")]
    private void TestVerifyEquipmentSlotMapping()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved) || saved.rosterState?.members == null || UnitRoster.Instance == null)
        {
            Debug.LogError("[Save/Load] Saved roster or live roster unavailable.");
            return;
        }

        int total = 0;
        int verified = 0;

        foreach (UnitRosterMemberSaveData member in saved.rosterState.members)
        {
            if (!UnitRoster.Instance.TryGetUnit(member.unitId, out UnitData unit))
            {
                Debug.LogError($"[Save/Load] Roster member not found: {member.characterName}.");
                continue;
            }

            foreach (EquipmentSaveData item in member.equipment)
            {
                total++;

                UnitLoadoutSlotState slot = unit.GetSlot(item.equippedSlot);

                if (slot == null || slot.ItemInstanceId != item.instanceId)
                {
                    Debug.LogError($"[Save/Load] Incorrect slot mapping for {member.characterName}: {item.instanceId}.");
                    continue;
                }

                verified++;
            }
        }

        Debug.Log($"[Save/Load] Equipment slot mapping verified: {verified}/{total} items.");
    }

    [ContextMenu("Test / Verify Saved Equipment Stats")]
    private void TestVerifySavedEquipmentStats()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved) || saved.rosterState?.members == null || UnitRoster.Instance == null)
        {
            Debug.LogError("[Save/Load] Saved roster or live roster unavailable.");
            return;
        }

        int total = 0;
        int verified = 0;

        foreach (UnitRosterMemberSaveData member in saved.rosterState.members)
        {
            if (!UnitRoster.Instance.TryGetUnit(member.unitId, out UnitData unit))
            {
                Debug.LogError($"[Save/Load] Roster member not found: {member.characterName}.");
                continue;
            }

            foreach (EquipmentSaveData savedItem in member.equipment)
            {
                total++;

                int index = unit.equippedItems.FindIndex(item => item.instanceId == savedItem.instanceId);

                if (index < 0)
                {
                    Debug.LogError($"[Save/Load] Equipment missing: {member.characterName} / {savedItem.instanceId}.");
                    continue;
                }

                EquipmentInstance item = unit.equippedItems[index];

                bool matches = item.rarity == savedItem.rarity &&
                               item.baseStats.Equals(savedItem.baseStats) &&
                               item.rolledStats.Equals(savedItem.rolledStats);

                for (int i = 0; i < 6; i++)
                {
                    if (item.attributeScaling.Get(i) != savedItem.attributeScaling.Get(i))
                    {
                        matches = false;
                        break;
                    }
                }

                if (!matches)
                {
                    Debug.LogError($"[Save/Load] Equipment stat mismatch: {member.characterName} / {item.itemName}.");
                    continue;
                }

                verified++;
            }
        }

        Debug.Log($"[Save/Load] Equipment stats verified: {verified}/{total} items.");
    }

    [ContextMenu("Test / Verify Equipment Combat Properties")]
    private void TestVerifyEquipmentCombatProperties()
    {
        if (!SaveManager.TryLoadMission(out MissionSaveData saved) || saved.rosterState?.members == null || UnitRoster.Instance == null)
        {
            Debug.LogError("[Save/Load] Saved roster or live roster unavailable.");
            return;
        }

        int total = 0;
        int verified = 0;
        int resistanceEntries = 0;
        int damageEntries = 0;

        foreach (UnitRosterMemberSaveData member in saved.rosterState.members)
        {
            if (!UnitRoster.Instance.TryGetUnit(member.unitId, out UnitData unit) || member.equipment == null)
            {
                Debug.LogError($"[Save/Load] Roster member unavailable: {member.characterName}.");
                continue;
            }

            foreach (EquipmentSaveData savedItem in member.equipment)
            {
                total++;

                int index = unit.equippedItems.FindIndex(item => item.instanceId == savedItem.instanceId);

                if (index < 0)
                {
                    Debug.LogError($"[Save/Load] Equipment not found: {savedItem.instanceId}.");
                    continue;
                }

                EquipmentInstance item = unit.equippedItems[index];

                DamageResistance[] originalResistances = item.resistances ?? Array.Empty<DamageResistance>();
                DamageResistance[] savedResistances = savedItem.resistances ?? Array.Empty<DamageResistance>();

                DamageComponent[] originalDamage = item.bonusDamage ?? Array.Empty<DamageComponent>();
                DamageComponent[] savedDamage = savedItem.bonusDamage ?? Array.Empty<DamageComponent>();

                bool resistancesMatch = System.Collections.StructuralComparisons.StructuralEqualityComparer.Equals(originalResistances, savedResistances);
                bool damageMatches = System.Collections.StructuralComparisons.StructuralEqualityComparer.Equals(originalDamage, savedDamage);

                if (!resistancesMatch || !damageMatches)
                {
                    Debug.LogError($"[Save/Load] Combat property mismatch: {member.characterName} / {item.itemName}.");
                    continue;
                }

                resistanceEntries += savedResistances.Length;
                damageEntries += savedDamage.Length;
                verified++;
            }
        }

        Debug.Log($"[Save/Load] Equipment combat properties verified: {verified}/{total} items.");
        Debug.Log($"[Save/Load] Verified {resistanceEntries} resistance entries and {damageEntries} bonus damage entries.");
    }

}