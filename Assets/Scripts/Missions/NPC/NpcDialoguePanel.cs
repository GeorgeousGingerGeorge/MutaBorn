using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class NpcDialoguePanel : MonoBehaviour
{
    public static NpcDialoguePanel Instance { get; private set; }

    [SerializeField] private GameObject window;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Transform optionsRoot;
    [SerializeField] private Button optionButtonPrefab;
    [SerializeField] private Button closeButton;

    private readonly List<Button> buttons = new List<Button>();
    private MissionNpcCharacter npc;
    private UnitController actor;

    public bool IsOpen => window != null && window.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);

        }

        if (window != null)
        {
            window.SetActive(false);

        }

    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;

        }

    }

    public void Open(MissionNpcCharacter target, UnitController visitor)
    {
        if (target == null || target.Dialogue == null || window == null || optionButtonPrefab == null)
        {

            return;

        }

        npc = target;
        actor = visitor;
        window.SetActive(true);
        ShowNode(target.Dialogue.startNodeId);

    }

    private void ShowNode(string id)
    {
        NpcDialogueNode node = npc.Dialogue.FindNode(id);
        if (node == null)
        {
            Debug.LogWarning($"NPC dialogue node '{id}' was not found.", npc);
            Close();

            return;

        }

        speakerText.text = npc.GetComponent<UnitController>().data.characterName;
        bodyText.text = node.text;
        foreach (Button button in buttons)
        {
            Destroy(button.gameObject);

        }

        buttons.Clear();
        foreach (NpcDialogueChoice choice in node.choices)
        {
            if (choice == null)
            {

                continue;

            }

            Button button = Instantiate(optionButtonPrefab, optionsRoot);
            button.gameObject.SetActive(true);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = choice.label;

            }

            NpcDialogueChoice selected = choice;
            button.onClick.AddListener(() => Choose(selected));
            buttons.Add(button);

        }

    }

    private void Choose(NpcDialogueChoice choice)
    {
        if (!string.IsNullOrWhiteSpace(choice.nextNodeId))
        {
            ShowNode(choice.nextNodeId);

            return;

        }

        npc.Resolve(choice.outcome, actor);
        Close();

    }

    public void Close()
    {
        if (window != null)
        {
            window.SetActive(false);

        }

        npc = null;
        actor = null;

    }

}
