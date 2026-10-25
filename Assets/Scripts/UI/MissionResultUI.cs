using TMPro;
using UnityEngine;

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

    [Header("Appearance")]
    [SerializeField] private Color victoryColor = new Color(0.35f, 0.9f, 0.5f);
    [SerializeField] private Color defeatColor = new Color(1f, 0.4f, 0.4f);

    [Header("Gameplay")]
    [Tooltip("Pauses time-scaled gameplay after the mission finishes. The two buttons remain unassigned for now.")]
    [SerializeField] private bool pauseGameOnResult = true;

    private bool subscribed;
    private bool resultShown;
    private bool pausedTime;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);

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
        Color titleColor = victory ? victoryColor : defeatColor;

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
