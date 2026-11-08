using UnityEngine;

public class AudioTest : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            AudioManager.Instance.PlaySound("Goblin Noise");
        }
    }
}
