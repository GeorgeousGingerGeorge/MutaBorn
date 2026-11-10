using UnityEngine;

public sealed class HubResourcesButtonToggle : MonoBehaviour
{
    [SerializeField] private WorkshopResourcesHUD resources;

    public void Configure(WorkshopResourcesHUD hud)
    {
        resources = hud;

    }

    public void Toggle()
    {
        if (resources == null)
        {
            return;

        }

        bool wasOpen = resources.gameObject.activeSelf;
        resources.ToggleDropdown();

        // On first activation, the existing HUD's Awake may close its own panel.
        if (!wasOpen && !resources.gameObject.activeSelf)
        {
            resources.ToggleDropdown();

        }

    }

}
