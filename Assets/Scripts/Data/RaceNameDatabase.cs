using UnityEngine;

[CreateAssetMenu(fileName = "RaceNames", menuName = "MutaBorn/Race Names")]
public class RaceNameDatabase : ScriptableObject
{
    public string[] firstNames;
    public string[] lastNames;
    [Range(0f, 1f)] public float singleNameChance;

    public string GenerateName()
    {
        if (firstNames == null || firstNames.Length == 0)
        {

            return string.Empty;

        }

        string name = firstNames[Random.Range(0, firstNames.Length)];
        if (lastNames != null && lastNames.Length > 0 && Random.value >= singleNameChance)
        {
            name += " " + lastNames[Random.Range(0, lastNames.Length)];

        }

        return name;

    }

}
