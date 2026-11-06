using UnityEngine;

public class UnitFacing : MonoBehaviour
{
    [Header("Direction A UnitFaces")]
    [SerializeField]
    private FacingDirection currentFacing = FacingDirection.North;

    [SerializeField]
    private Transform targetToRotate;
    [SerializeField]
    private float turnSpeed = 360f;

  
    private Quaternion targetRotation;
    public Vector3 facingVector => targetToRotate.forward;
    public FacingDirection CurrentFacing => currentFacing;

    private void Awake() 
    {
        if (targetToRotate == null)
        {
            targetToRotate = transform;
        }
        ApplyFacing(true);    
    
    }

    // Only redirect facing if its previous target belonged to the old model.
    public void RebindRaceModel(Transform oldVisualRoot, Transform newVisualRoot)
    {
        if (targetToRotate == null ||
            (oldVisualRoot != null && targetToRotate.IsChildOf(oldVisualRoot)))
        {
            targetToRotate = newVisualRoot;
            ApplyFacing(true);

        }

    }

    public void SetFacing(FacingDirection direction) 
    {
        currentFacing = direction;
        ApplyFacing();
    }
    public void SetFacing(Vector3 direction) 
    {
        direction.y = 0f;
      
        if (direction.sqrMagnitude < 0.001f) 
        {
            return;

        }

        direction.Normalize();
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        angle = (Mathf.Round(angle / 45f) * 45f + 360f) % 360f;
        currentFacing = (FacingDirection)angle;
       
        ApplyFacing();
    }

    private void ApplyFacing(bool instance = false) 
    {
        targetRotation = Quaternion.Euler(0f, (float)currentFacing, 0f);

        if (instance) 
        {
            targetToRotate.rotation = targetRotation;
        }
       
    }

    private void Update() 
    {
        if (targetToRotate == null) 
        {
          
            return;
        }
        targetToRotate.rotation = Quaternion.RotateTowards(targetToRotate.rotation, targetRotation, turnSpeed * Time.deltaTime);

       
    }
}
