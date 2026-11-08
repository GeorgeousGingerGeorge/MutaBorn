using UnityEngine;

[RequireComponent(typeof(MissionNpcCharacter))]
public sealed class NpcDialogueInteractable : WorldInteractable
{
    private MissionNpcCharacter npc;

    private void Awake()
    {
        npc = GetComponent<MissionNpcCharacter>();

    }

    public override string InteractionMenuLabel => $"Talk to {InteractionTargetName}";

    public override bool CanInteract(UnitController actor)
    {

        return base.CanInteract(actor) && npc != null && !npc.Resolved && npc.Dialogue != null &&
            NpcDialoguePanel.Instance != null && !NpcDialoguePanel.Instance.IsOpen;

    }

    protected override void OnInteractionSucceeded(UnitController actor)
    {
        NpcDialoguePanel.Instance.Open(npc, actor);

    }

}
