using UnityEngine;

public sealed class FaceCameraBillboard : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            return;

        }

        transform.forward = targetCamera.transform.forward;

    }

}
