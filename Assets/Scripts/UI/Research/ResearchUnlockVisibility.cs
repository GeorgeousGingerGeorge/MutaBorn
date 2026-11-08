using System.Collections.Generic;
using UnityEngine;

// Attach this to an always-active parent, never the button being hidden.
// Use it for future Lab injections, surgeries and Mutation Storage tab buttons.
public sealed class ResearchUnlockVisibility : MonoBehaviour
{
    [System.Serializable]
    private sealed class UnlockButton
    {
        public GameObject target;
        public ResearchBenefitType requiredBenefit;

    }

    [SerializeField] private List<UnlockButton> buttons = new List<UnlockButton>();
    private ResearchManager manager;

    private void OnEnable()
    {
        manager = ResearchManager.Instance;
        if (manager != null)
        {
            manager.StateChanged += Refresh;

        }

        Refresh();

    }

    private void Start()
    {
        if (manager == null && ResearchManager.Instance != null)
        {
            manager = ResearchManager.Instance;
            manager.StateChanged += Refresh;

        }

        Refresh();

    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.StateChanged -= Refresh;

        }

    }

    private void Refresh()
    {
        foreach (UnlockButton entry in buttons)
        {
            if (entry != null && entry.target != null)
            {
                entry.target.SetActive(manager == null || manager.HasBenefit(entry.requiredBenefit));

            }

        }

    }

}
