using UnityEngine;

[DisallowMultipleComponent]
public class MissionUnitIdentity : MonoBehaviour
{
    [SerializeField] private string persistentId;

    public string PersistentId => persistentId;

    public void SetPersistentId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogError("Cannot assign an empty persistent ID.", this);
            return;
        }

        persistentId = id;
    }
}