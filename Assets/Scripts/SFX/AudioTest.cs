using UnityEngine;
using UnityEngine.InputSystem;

public class AudioTest : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            AudioManager.Instance.PlayLoop("Wind Sound");
        }

        if (Keyboard.current.yKey.wasPressedThisFrame)
        {
            AudioManager.Instance.StopLoop();
        }
    }
}

/*
public class AudioTest : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            AudioManager.Instance.PlaySound("Wind Noise");
        }
    }
}
*/