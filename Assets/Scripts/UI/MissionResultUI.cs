using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MissionResultUI : MonoBehaviour
{
    [Header("Mission Reference")]
    [Tooltip("Optional. The mission manager will be found automatically if left empty.")]
    [SerializeField] private MissionObjectiveManager missionManager;

    [Header("Result Menu")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text resultOutcome;

    [Header("Spoils result sections (author in Unity)")]
    [SerializeField] private TMP_Text resourcesText;
    [SerializeField] private TMP_Text equipmentText;
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private TMP_Text casualtiesText;
    [SerializeField] private TMP_Text researchText;
    [SerializeField] private Button claimAndReturnButton;
    [SerializeField] private string hubSceneName = "TEST Hub";

    [Header("Unit Progression - On Claimed Victory")]
    [SerializeField, Min(0)] private int victoryExperience = 100;
    [SerializeField, Min(0)] private int victoryMutationPoints = 25;

    [Header("Appearance")]
    [SerializeField] private Color victoryColor = new Color(0.35f, 0.9f, 0.5f);
    [SerializeField] private Color defeatColor = new Color(1f, 0.4f, 0.4f);

    [Header("Gameplay")]
    [Tooltip("Pauses gameplay after the mission finishes. Result buttons remain clickable.")]
    [SerializeField] private bool pauseGameOnResult = true;

    private bool subscribed;
    private bool resultShown;
    private bool victoryProgressRecorded;
    private bool pausedTime;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);

        }

        if (claimAndReturnButton != null)
        {
            claimAndReturnButton.onClick.AddListener(ClaimAndReturnToHub);

        }

    }

    private void OnEnable()
    {
        TrySubscribe();
        CheckExistingResult();

    }

    private void Start()
    {
        // Handles a mission manager that initializes after this component.
        TrySubscribe();
        CheckExistingResult();

        if (resultPanel == null)
        {
            Debug.LogWarning("MissionResultUI needs its Result Panel assigned in the Inspector.", this);

        }

    }

    private void TrySubscribe()
    {
        if (subscribed)
        {

            return;

        }

        if (missionManager == null)
        {
            missionManager = MissionObjectiveManager.Instance;

        }

        if (missionManager == null)
        {
            missionManager = FindAnyObjectByType<MissionObjectiveManager>();

        }

        if (missionManager == null)
        {

            return;

        }

        missionManager.MissionFinished += HandleMissionFinished;
        subscribed = true;

    }

    private void CheckExistingResult()
    {
        if (missionManager == null || resultShown)
        {

            return;

        }

        HandleMissionFinished(missionManager.Status);

    }

    private void HandleMissionFinished(MissionStatus result)
    {
        if (resultShown || (result != MissionStatus.Victory && result != MissionStatus.Defeat))
        {

            return;

        }

        if (resultPanel == null)
        {
            Debug.LogError("MissionResultUI cannot display the mission result: Result Panel is missing.", this);

            return;

        }

        bool victory = result == MissionStatus.Victory;
        Color titleColor = resultPanel.name == "Unified Mission Results"
            ? Color.white : (victory ? victoryColor : defeatColor);

        if (resultTitle != null)
        {
            resultTitle.text = victory ? "MISSION COMPLETE" : "MISSION FAILED";
            resultTitle.color = titleColor;

        }

        if (resultOutcome != null)
        {
            resultOutcome.text = victory ? "VICTORY" : "DEFEAT";
            resultOutcome.color = titleColor;

        }

        MissionSpoils spoils = MissionSpoils.Instance;
        if (spoils != null)
        {
            spoils.PrepareResult();

        }

        if (resourcesText != null)
        {
            resourcesText.text = "RESOURCES GAINED\n" +
                (spoils != null ? spoils.ResourceSummary() : "Spoils system missing.");

        }

        if (equipmentText != null)
        {
            equipmentText.text = "EQUIPMENT GAINED / LOST\n" +
                (spoils != null ? spoils.EquipmentSummary() : "Spoils system missing.");

        }

        if (experienceText != null)
        {
            experienceText.text = victory
                ? $"UNIT EXPERIENCE\nSurviving mission participants: +{victoryExperience} XP and +{EffectiveMutationPoints()} Mutation Points on claim."
                : "UNIT EXPERIENCE\nNo progression awarded on defeat.";

        }

        if (casualtiesText != null)
        {
            casualtiesText.text = "FALLEN CREW\n" +
                (spoils != null ? spoils.CasualtySummary() : "Spoils system missing.");

        }

        if (researchText != null)
        {
            researchText.text = BuildResearchSummary(victory);

        }

        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling();
        resultShown = true;

        if (pauseGameOnResult)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            pausedTime = true;

        }

    }

    private string BuildResearchSummary(bool victory)
    {
        ResearchManager research = ResearchManager.Instance;
        if (research == null)
        {

            return "RESEARCH PROGRESS\nResearch is unavailable. Start the mission from the Hub.";

        }

        if (string.IsNullOrEmpty(research.ActiveResearchId))
        {

            return "RESEARCH PROGRESS\nNo active research project.";

        }

        ResearchNode node = research.Database != null
            ? research.Database.Find(research.ActiveResearchId) : null;
        string name = node != null ? node.displayName : research.ActiveResearchId;
        int remaining = research.RemainingBattles;
        if (!victory)
        {

            return $"RESEARCH PROGRESS\n{name}\n{remaining} battle(s) remaining. No progress on defeat.";

        }

        int afterClaim = Mathf.Max(0, remaining - 1);
        string result = afterClaim == 0 ? "Completes when rewards are claimed."
            : $"{afterClaim} battle(s) remaining after claim.";

        return $"RESEARCH PROGRESS\n{name}\n{remaining} → {afterClaim} battles remaining\n{result}";

    }

    private int EffectiveMutationPoints()
    {
        ResearchManager research = ResearchManager.Instance;
        int bonusPercent = research != null
            ? research.BenefitTotal(ResearchBenefitType.MutationExperience) : 0;

        return victoryMutationPoints + Mathf.CeilToInt(victoryMutationPoints * bonusPercent / 100f);

    }

    public void ClaimAndReturnToHub()
    {
        if (string.IsNullOrWhiteSpace(hubSceneName) ||
            !Application.CanStreamedLevelBeLoaded(hubSceneName))
        {
            Debug.LogError($"[MissionResultUI] Add '{hubSceneName}' to Build Settings or correct Hub Scene Name.", this);

            return;

        }

        MissionSpoils spoils = MissionSpoils.Instance;
        if (spoils == null || !spoils.IsClaimed && !spoils.TryClaim())
        {
            Debug.LogError("[MissionResultUI] Cannot return: spoils could not be committed to the Hub.", this);

            return;

        }

        if (!victoryProgressRecorded && missionManager != null &&
            missionManager.Status == MissionStatus.Victory)
        {
            victoryProgressRecorded = true;
            UnitRoster roster = UnitRoster.Instance;
            if (roster != null)
            {
                int count = roster.GrantVictoryProgress(victoryExperience, EffectiveMutationPoints());
                Debug.Log($"[Progression] Rewarded {count} surviving deployed unit(s).", this);

            }

            if (ResearchManager.Instance != null)
            {
                ResearchManager.Instance.RecordClaimedVictory();

            }

        }

        RestoreTimeScale();
        SceneManager.LoadScene(hubSceneName);

    }

    private void OnDisable()
    {
        if (subscribed && missionManager != null)
        {
            missionManager.MissionFinished -= HandleMissionFinished;
            subscribed = false;

        }

        RestoreTimeScale();

    }

    private void OnDestroy()
    {
        RestoreTimeScale();

    }

    private void RestoreTimeScale()
    {
        if (!pausedTime)
        {

            return;

        }

        Time.timeScale = previousTimeScale;
        pausedTime = false;

    }

}
