using System;
using System.Collections.Generic;
using UnityEngine;

public enum NpcDialogueOutcome
{
    None,
    JoinCrew,
    Escape,
    Hostile,
    Neutral

}

[Serializable]
public sealed class NpcDialogueChoice
{
    public string label;
    public string nextNodeId;
    public NpcDialogueOutcome outcome;

}

[Serializable]
public sealed class NpcDialogueNode
{
    public string id;
    [TextArea(2, 6)] public string text;
    public List<NpcDialogueChoice> choices = new List<NpcDialogueChoice>();

}

[CreateAssetMenu(menuName = "MutaBorn/Missions/NPC Dialogue", fileName = "New NPC Dialogue")]
public sealed class NpcDialogueData : ScriptableObject
{
    public string startNodeId = "start";
    public List<NpcDialogueNode> nodes = new List<NpcDialogueNode>();

    public NpcDialogueNode FindNode(string id)
    {

        return nodes.Find(node => node != null && node.id == id);

    }

}
