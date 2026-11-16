using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Button))]
public class UIAudioFeedback : MonoBehaviour
{
    [SerializeField] private string clickSound = "Button Click";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(PlayClickSound);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(clickSound);
        }
    }
}
