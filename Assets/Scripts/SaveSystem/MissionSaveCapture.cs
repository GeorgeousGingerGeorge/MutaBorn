using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissionSaveCapture
{
    public static MissionSaveData Capture()
    { 
        MissionSaveData snapshot = new MissionSaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            saveTimeUTC = DateTime.UtcNow.ToString("O")
        };

        UnitController[] units = UnityEngine.Object.FindObjectsByType<UnitController>(FindObjectsInactive.Include);

        UnitRoster roster = UnitRoster.Instance;

        if (roster != null)
        {
            snapshot.rosterState.unlockedCrewSlots = roster.UnlockedCrewSlots;

            foreach (UnitRosterDeployment deployment in roster.Deployments)
            {
                snapshot.rosterState.deployments.Add(new UnitRosterDeploymentSaveData
                {
                    slotIndex = deployment.SlotIndex,
                    unitId = deployment.UnitId
                });
            }

            foreach (UnitData unit in roster.Units)
            {
                UnitRosterMemberSaveData savedMember = new UnitRosterMemberSaveData
                {
                    unitId = unit.UnitId,
                    characterName = unit.characterName,
                    raceName = unit.raceName,

                    level = unit.Level,
                    experience = unit.Experience,
                    unspentAttributePoints = unit.UnspentAttributePoints,
                    mutationPoints = unit.MutationPoints,

                    leveledStrength = unit.leveledStrength,
                    leveledDexterity = unit.leveledDexterity,
                    leveledVitality = unit.leveledVitality,
                    leveledConstitution = unit.leveledConstitution,
                    leveledPerception = unit.leveledPerception,
                    leveledIntelligence = unit.leveledIntelligence,

                    currentHealth = unit.currentHealth,
                    currentMana = unit.currentMana,
                    currentActionPoints = unit.currentActionPoints
                };

                // Capture fixed mutation nodes
                foreach (ActiveMutationRecord mutation in unit.activeMutationNodes)
                {
                    if (mutation != null && mutation.MutationId != MutationID.none)
                    {
                        savedMember.fixedMutationIds.Add(mutation.MutationId);
                    }
                }

                // Capture legacy equipped mutations
                foreach (BaseMutation mutation in unit.activeMutations.Values)
                {
                    if (mutation != null && mutation.ID != MutationID.none)
                    {
                        savedMember.legacyMutationIds.Add(mutation.ID);
                    }
                }

                // Capture equipped item instances without generating replacements
                foreach (EquipmentInstance item in unit.equippedItems)
                {
                    if (item.typeDefinition == null || string.IsNullOrWhiteSpace(item.instanceId) || string.IsNullOrWhiteSpace(item.typeDefinition.TypeId) || string.IsNullOrWhiteSpace(item.templateId))
                    {
                        Debug.LogError($"[Save/Load] Invalid equipment identity for {unit.characterName}: {item.itemName}.");
                        return null;
                    }

                    savedMember.equipment.Add(new EquipmentSaveData
                    {
                        instanceId = item.instanceId,
                        typeId = item.typeDefinition.TypeId,
                        templateId = item.templateId,

                        rarity = item.rarity,
                        baseStats = item.baseStats,
                        rolledStats = item.rolledStats,
                        attributeScaling = item.attributeScaling,

                        lastStatRoll1 = item.lastStatRoll1,
                        lastStatRoll2 = item.lastStatRoll2,
                        bonusStacks = item.bonusStacks,
                        lastUpgradeBonus = item.lastUpgradeBonus
                    });
                }

                snapshot.rosterState.members.Add(savedMember);

                Debug.Log($"[Save/Load] {unit.characterName}: {savedMember.fixedMutationIds.Count} fixed mutations, {savedMember.legacyMutationIds.Count} legacy mutations.");
            }

            Debug.Log($"[Save/Load] Captured {snapshot.rosterState.deployments.Count} crew deployments and {snapshot.rosterState.members.Count} roster members.");
        }
        else
        {
            Debug.LogWarning("[Save/Load] UnitRoster not found. Roster data was not captured.");
        }

        foreach (UnitController unit in units)
        {
            if (unit.data == null)
                continue;

            MissionUnitIdentity identity = unit.GetComponent<MissionUnitIdentity>();

            string persistentId = identity != null ? identity.PersistentId : "";

            if (string.IsNullOrEmpty(persistentId) && unit.InitializationMode != UnitInitializationMode.LegacyScene)
            {
                persistentId = $"CrewSlot_{unit.RosterDeploymentSlot}"; // 	EnemySpawn_01:group0:unit0 for enemies (& civ) - CrewSlot_0 for our players chars
            }

            UnitSaveData savedUnit = new UnitSaveData
            {
                unitId = unit.data.UnitId,
                persistentId = persistentId,
                unitName = unit.data.characterName,
                raceName = unit.data.raceName,

                position = unit.transform.position,
                rotation = unit.transform.rotation,

                floorId = unit.currentNode != null ? unit.currentNode.floorID : -1,
                nodeCoords = unit.currentNode != null ? unit.currentNode.coords : Vector2Int.zero,

                currentHealth = unit.data.currentHealth,
                currentMana = unit.data.currentMana,
                currentActionPoints = unit.data.currentActionPoints,

                isActive = unit.gameObject.activeSelf
            };

            snapshot.units.Add(savedUnit);
        } // END of the unit foreach loop

        // Capture complete turn state
        TurnManager turnManager = TurnManager.Instance;

        if (turnManager == null)
        {
            Debug.LogError("Cannot save mission: Turn Manager not found.");
            return null;
        }

        snapshot.turnState = turnManager.CaptureTurnState();

        if (snapshot.turnState == null)
        {
            Debug.LogError("Cannot save mission: Turn capture failed.");
            return null;
        }

        // Capture complete objective state
        MissionObjectiveManager objectiveManager = MissionObjectiveManager.Instance;

        if (objectiveManager == null)
        {
            Debug.LogError("Cannot save mission: Objective Manager not found.");
            return null;
        }

        snapshot.objectiveState = objectiveManager.CaptureObjectiveState();

        if (snapshot.objectiveState == null)
        {
            Debug.LogError("Cannot save mission: Objective capture failed.");
            return null;
        }

        return snapshot;
    } // END of Capture()
} // END of MissionSaveCapture class
