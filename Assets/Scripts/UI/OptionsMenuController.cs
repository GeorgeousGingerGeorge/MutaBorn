using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OptionsMenuController : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown displayDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private List<Resolution> availableResolutions;
    private void Start()
    {
        displayDropdown.ClearOptions();

        displayDropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            "Windowed",
            "Borderless",
            "Fullscreen"
        });

        displayDropdown.value = GetCurrentDisplayMode();
        displayDropdown.RefreshShownValue();

        availableResolutions = new List<Resolution>();

        resolutionDropdown.ClearOptions();

        List<string> resolutionOptions = new List<string>();

        for (int i = 0; i < Screen.resolutions.Length; i++)
        {
            Resolution resolution = Screen.resolutions[i];

            if (resolution.height >= 512 && resolution.height <= 2160)
            {
                bool alreadyAdded = false;

                for (int j = 0; j < availableResolutions.Count; j++)
                {
                    Resolution existing = availableResolutions[j];

                    if (existing.width == resolution.width &&
                        existing.height == resolution.height)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (!alreadyAdded)
                {
                    availableResolutions.Add(resolution);

                    resolutionOptions.Add(
                        resolution.width + " x " + resolution.height
                    );
                }
            }
        }

        resolutionDropdown.AddOptions(resolutionOptions);

        int currentResolution = 0;

        for (int i = 0; i < availableResolutions.Count; i++)
        {
            if (availableResolutions[i].width == Screen.width &&
                availableResolutions[i].height == Screen.height)
            {
                currentResolution = i;
                break;
            }
        }

        resolutionDropdown.SetValueWithoutNotify(currentResolution);
        resolutionDropdown.RefreshShownValue();
    }

    public void SetResolution(int index)
    {
        if (index < 0 || index >= availableResolutions.Count)
            return;

        Resolution resolution = availableResolutions[index];

        Screen.SetResolution(
            resolution.width,
            resolution.height,
            Screen.fullScreenMode
        );
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume / 100f;
    }

    public void SetDisplayMode(int mode)
    {
        switch (mode)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;

            case 1:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;

            case 2:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
        }
    }

    private int GetCurrentDisplayMode()
    {
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.Windowed:
                return 0;

            case FullScreenMode.FullScreenWindow:
                return 1;

            case FullScreenMode.ExclusiveFullScreen:
                return 2;

            default:
                return 0;
        }
    }
}
