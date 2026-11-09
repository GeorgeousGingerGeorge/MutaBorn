using UnityEngine;
using UnityEngine.InputSystem;

public class AudioTest : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            AudioManager.Instance.PlaySound("Goblin Noise");
        }
    }
}
