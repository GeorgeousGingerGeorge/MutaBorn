using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLocation : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("LayerMask")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask unitLayer;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("RayCasting Variables- HowFar/Ray/Detection ")]
    private float maxDistance = 1000f; //how far the ray goes
    Ray cameraRay;
    RaycastHit targetHit;

    [Header("Targets it needs to hit")]
    public RaycastHit groundTarget { get; private set; }
    public RaycastHit unitTarget { get; private set; }
    public RaycastHit obstacleTarget { get; private set; }
    public bool surfaceHit { get; private set; }
    public bool unitHit { get; private set; }
    public bool obstacleHit { get; private set; }

    [Header("Camera its using")]
    private Camera mainCamera;

    public void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    public void Update()
    {
        surfaceHit = false;
        unitHit = false;
        obstacleHit = false;

        if (mainCamera == null || Mouse.current == null) 
        {
            return;
        }

        //Defines where the mouse is and turns that into a ray 
        Vector3 mousePosition = Mouse.current.position.ReadValue();
        cameraRay = mainCamera.ScreenPointToRay(mousePosition);

        //Detects where the ray has been hit and set the values necessary

        if (Physics.Raycast(cameraRay, out targetHit, maxDistance, unitLayer)) 
        {
           
            unitTarget = targetHit;
            unitHit = true;
        }
        else if (Physics.Raycast(cameraRay, out targetHit, maxDistance, obstacleLayer)) 
        {
           
            obstacleTarget = targetHit;
            obstacleHit = true;
        }
        else if (Physics.Raycast(cameraRay, out targetHit, maxDistance, groundLayer)) 
        {
            
            groundTarget = targetHit;
            surfaceHit = true;
        }


    }
}
