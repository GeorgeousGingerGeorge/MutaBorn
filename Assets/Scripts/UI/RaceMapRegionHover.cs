using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RaceMapRegionHover : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler
{
    [Header("Faction lore")]
    [SerializeField] private string raceName;
    [SerializeField] private string factionName;
    [SerializeField] private string territoryName;
    [SerializeField] private string culturalIdentity;
    [TextArea(4, 10)] [SerializeField] private string territoryDescription;
    [TextArea(4, 12)] [SerializeField] private string factionHistory;
    [SerializeField] private bool available;
    [SerializeField] private bool concealed;

    [Header("Shared information panel")]
    [SerializeField] private TMP_Text raceTitle;
    [SerializeField] private TMP_Text factionTitle;
    [SerializeField] private TMP_Text territoryTitle;
    [SerializeField] private TMP_Text identityTitle;
    [SerializeField] private TMP_Text statusTitle;
    [SerializeField] private TMP_Text loreBody;
    [SerializeField] private ScrollRect loreScroll;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Display();

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Display();

    }

    public void OnSelect(BaseEventData eventData)
    {
        Display();

    }

    public void Display()
    {
        if (raceTitle == null || factionTitle == null || territoryTitle == null ||
            identityTitle == null || statusTitle == null || loreBody == null)
        {

            return;

        }

        bool hidden = concealed && !available;
        raceTitle.text = hidden ? "???" : raceName.ToUpperInvariant();
        factionTitle.text = hidden ? "Unknown Faction" : factionName;
        territoryTitle.text = hidden ? "Territory Unknown" : territoryName;
        identityTitle.text = hidden ? "Unknown" : culturalIdentity;
        statusTitle.text = available ? "AVAILABLE — SELECT TO VIEW MISSIONS" : "LOCKED — PREVIEW ONLY";
        loreBody.text = hidden ? "This region remains shrouded in mystery." :
            "<b>TERRITORY DESCRIPTION</b>\n" + territoryDescription +
            "\n\n<b>FACTION BACKSTORY</b>\n" + factionHistory;

        if (loreScroll != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(loreScroll.content);
            loreScroll.verticalNormalizedPosition = 1f;

        }

    }

#if UNITY_EDITOR
    public void Configure(string race, string faction, string territory, string identity,
        string description, string history, bool unlocked, bool secret,
        TMP_Text raceText, TMP_Text factionText, TMP_Text territoryText, TMP_Text identityText,
        TMP_Text statusText, TMP_Text loreText, ScrollRect scroll)
    {
        raceName = race;
        factionName = faction;
        territoryName = territory;
        culturalIdentity = identity;
        territoryDescription = description;
        factionHistory = history;
        available = unlocked;
        concealed = secret;
        raceTitle = raceText;
        factionTitle = factionText;
        territoryTitle = territoryText;
        identityTitle = identityText;
        statusTitle = statusText;
        loreBody = loreText;
        loreScroll = scroll;

    }
#endif

}
