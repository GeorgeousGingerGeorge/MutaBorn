using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MutationLookupEntry
{
    public BodySlots slot;
    public MutationStrain strain;
    public List<MutationID> mutations = new();
}
