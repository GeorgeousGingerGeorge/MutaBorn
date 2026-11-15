
using UnityEngine;

public static class MissionWorldObjectIdentity
{
    public static string GetId(WorldTrap trap)
    {
        if (trap == null)
        {
            return null;
        }

        MissionUnitIdentity identity = trap.GetComponent<MissionUnitIdentity>();

        if (identity != null && !string.IsNullOrWhiteSpace(identity.PersistentId))
        {
            return identity.PersistentId;
        }

        string path = "";
        Transform current = trap.transform;

        while (current != null)
        {
            path = $"/{current.name}[{current.GetSiblingIndex()}]{path}";
            current = current.parent;
        }

        return $"Trap:{trap.gameObject.scene.name}:{path}";
    }
}
