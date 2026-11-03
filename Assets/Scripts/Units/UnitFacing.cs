using UnityEngine;

public class UnitFacing : MonoBehaviour
{
    [Header("Direction A UnitFaces")]
    [SerializeField]
    private FacingDirection currentFacing = FacingDirection.North;

    [SerializeField]
    private Transform targetToRotate;

    public Vector3 facingVector => targetToRotate.forward;

    private void Awake() 
    {
        ApplyFacing();    
    
    }

    public void SetFacing(FacingDirection direction) 
    {
        currentFacing = direction;
        ApplyFacing();
    }
    public void SetFacing(Vector3 direction) 
    {
        direction.y = 0f;
        
        if(direction.sqrMagnitude < 0.001f) 
        {
            return;

        }

        direction.Normalize();
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        angle = (Mathf.Round(angle / 45f) * 45f + 360f) % 360f;
        currentFacing = (FacingDirection)angle;

        ApplyFacing();
    }

    private void ApplyFacing() 
    {
        if (targetToRotate == null) 
        {
            targetToRotate = transform;
        }

        targetToRotate.rotation = Quaternion.Euler(0f, (float)currentFacing, 0f);
    }
}
