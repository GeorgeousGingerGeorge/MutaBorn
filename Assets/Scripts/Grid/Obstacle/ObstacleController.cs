
using UnityEngine;
using System.Collections; 
using System.Collections.Generic;
using System;


public class ObstacleController : MonoBehaviour
{
    [Header("Node References - FirstNode, SecondNode (Solid || Edge)")]
    public GridNode firstnode { get; private set; } = null;
    public GridNode secondnode { get; private set; } = null;
    public GridNode boundaryNode { get; private set; } = null;
    public Vector2Int outwardDirection { get; private set; } = Vector2Int.zero;
    private List<GridNode> occupiedNodes = new List<GridNode>();
    public IReadOnlyList<GridNode> OccupiedNodes => occupiedNodes;

    [Header("Determines the number of nodes the obstacle occupies")]
    [SerializeField] public NodeCoverage nodeCoverage;
    [SerializeField] public Vector2Int obstacleFootprint = Vector2Int.one;
    [SerializeField] public Transform anchor;
    private Occupancy occupancy;
    private GridManager gridManager;
    private FloorConnector connector;

    [Header("Obstacle Information")]
    [SerializeField] private ObstacleData data;

    [Header("Obstacle Information- Accessor")]
    public ObstacleData currentData { get; private set; }

    [Header("Obstacle Information- Name of Obstacle/TypeOfObstacle")]
    public string obstacleName { get; private set; }
    public ObstacleType typeOfObstacle{ get; private set;  }

    [Header("ObstacleInformation- MaxDurability/CurrentDurability")]
    public int maxHealth { get; private set; }
    public int currentHealth { get; private set; }

    [Header("Obstacle Information- States/Height/Directions")]
    public CoverState stateOfCover { get; private set; }
    public float coverHeight { get; private set; }

    [Header("Obstacle Information- GamePlay Checks (Movements/targeting/shooting")]
    public bool blocksMovement { get; private set; }
    public bool blockLineOfSight { get; private set; }
    public bool blockProjectile { get; private set; }
    public bool blockSound { get; private set; }

    [Header("Obstacle Information- Traversal Check")]
    public bool canBeVaulted { get; private set; }
    public bool canBeClimbed { get; private set; }

    [Header("Obstacle Infomration- Visual Changes to objects")]
    public ObstacleDestructionStages currentStage { get; private set; }
    private ObstacleDestructionStages[] destructionStages;
    public int currentStageIndex { get; private set; } = -1;

    public bool isDestructible { get; private set; }
    public bool IsDestroyed { get; private set; } = false;

    [Header("Particlee System")]
   
    [SerializeField] private ParticleSystem destructionEffectPrefab;
    
    private bool occupancyRegistered;

    public bool IsBlocking => occupancyRegistered;
    public Occupancy CurrentOccupancy => occupancy;

    [SerializeField] private int ObstaclesCurrentHealth;
    public void Awake()
    {
        destructionStages = GetComponentsInChildren<ObstacleDestructionStages>(true);
        
        InitializeObstacleData();
       
    }


    //Initialization
    public bool InitializeObstacleData() 
    {
        if (data == null)
        {
            Debug.LogWarning("An obstacle is missing its ObstacleData");
            return false;
        }

        currentData = data;

        obstacleName = currentData.obstacleName;
        typeOfObstacle = currentData.typeOfObstacle;
        maxHealth = currentData.maxHealth;
        currentHealth = maxHealth;
        ObstaclesCurrentHealth = currentHealth; //For Testing Purposes
        isDestructible = currentData.isDestructible;



        //Gives Current Default Values of the obstacle
        ApplyDefaultObstacleInformation();

        destructionStages = GetComponentsInChildren<ObstacleDestructionStages>(true);

        //Determine the Initial Destruction Stage
        UpdateDestructionStage();

        return true;
    }

    //ApplyDefault
    private void ApplyDefaultObstacleInformation() 
    {
        blocksMovement = currentData.blocksMovement;
        blockLineOfSight = currentData.blocksLineOfSight;
        blockProjectile = currentData.blocksProjectile;
        blockSound = currentData.blocksSound;

        stateOfCover = currentData.coverState;
        coverHeight = currentData.coverHeight;

        canBeVaulted = currentData.canBeVaulted;
        canBeClimbed = currentData.canBeClimbed;



    }
    public void InitializeNodes() 
    {
        if (!InitializeObstacleData())
        {
            return;

        }

        gridManager = FindAnyObjectByType<GridManager>();
        occupancy = FindAnyObjectByType<Occupancy>();

        if (gridManager == null || occupancy == null)
        {
            Debug.LogError($"Obstacle '{name}' requires GridManager and Occupancy.", this);

            return;

        }

        switch (currentData.typeOfObstacle)
        {
            case ObstacleType.Solid:
                {   if(nodeCoverage == NodeCoverage.Single && obstacleFootprint == Vector2Int.one)
                    { 
                        firstnode = gridManager.GetNodeFromClosestWorldPosition(transform.position); 
                    }
                    else if(nodeCoverage == NodeCoverage.Multiple && obstacleFootprint != Vector2Int.one) 
                    {
                        TryGetOccupiedNodes();
                    }

                }
                break;

            case ObstacleType.Edges:
                { 
                    Vector3 perpendicular = transform.right;
                    perpendicular.y = 0f;
                    perpendicular.Normalize();

                    float offset = gridManager.GridCellSize / 2f;

                    firstnode = gridManager.GetNodeFromClosestWorldPosition(
                        transform.position + perpendicular * offset);

                        secondnode = gridManager.GetNodeFromClosestWorldPosition(
                            transform.position - perpendicular * offset);
                }
                break;

            case ObstacleType.FloorConnectors:
                connector = GetComponent<FloorConnector>();

                if (connector != null && connector.connectorType == ConnectorType.Stairs)
                {
                    foreach (GridNode node in connector.nodes)
                    {
                        Debug.Log($"FloorID ->{node.floorID}, GridPosition -> {node.coords}");

                        occupancy.RegisterSolidObstacles(node, this);

                    }

                    occupancyRegistered = true;

                }

                return;
               
            case ObstacleType.WorldSpace: 
                {
                    Vector3 perpendicular = transform.right;
                    perpendicular.y = 0f;
                    perpendicular.Normalize();

                    float offset = gridManager.GridCellSize / 2f;


                    Vector3 positionA = transform.position + perpendicular * offset;
                    Vector3 positionB = transform.position - perpendicular * offset;

                    bool hasNodeA = TryGetNodeAtPosition(positionA, out GridNode nodeA);
                    bool hasNodeB = TryGetNodeAtPosition(positionB, out GridNode nodeB);

                    if (hasNodeA && hasNodeB)
                    {
                        if (nodeA == nodeB)
                        {
                            return;
                        }

                        firstnode = nodeA;
                        secondnode = nodeB;
                    }
                    else if (hasNodeA != hasNodeB)
                    {
                        boundaryNode = hasNodeA ? nodeA : nodeB;

                        Vector3 outward = hasNodeA ? -perpendicular : perpendicular;

                        if (Mathf.Abs(outward.x) > Mathf.Abs(outward.y))
                        {
                            outwardDirection = new Vector2Int(outward.x > 0 ? 1 : -1, 0);


                        }
                        else
                        {
                            outwardDirection = new Vector2Int(0, outward.z > 0 ? 1 : -1);

                        }
                    }
                    else
                    {
                        return;
                    }
                }
                return;


        }

        if (!TrySetBlocking(true))
        {
            Debug.LogWarning(
                $"Obstacle '{name}' could not register its grid nodes. Check placement or overlaps.",
                this);

        }

    }

    // Allows doors and gates to change blocking without destroying the obstacle.
    public bool TrySetBlocking(bool blocked)
    {
        if (!InitializeObstacleData() || occupancy == null || firstnode == null)
        {
            return false;

        }

        if (blocked == occupancyRegistered)
        {
            return true;

        }

        switch (currentData.typeOfObstacle)
        {
            case ObstacleType.Solid:

                if (blocked)
                {
                    if (occupancy.IsUnitInNode(firstnode) ||
                        occupancy.FindSolidObstacle(firstnode) != null)
                    {
                        return false;

                    }

                  

                }
                else
                {
                    occupancy.UnregisterSolidObstacles(firstnode, this);

                }

                break;

            case ObstacleType.Edges:
                {

                    //Gets the perpendicular Direction of the current Transform
                    Vector3 perpendicular = transform.right;
                    perpendicular.y = 0f;
                    perpendicular.Normalize();

                    //Calculates the center of the object
                    float cellsizeCenter = gridManager.GridCellSize / 2f;

                    //Acquires the position of the potential nodes on either side of the object
                    Vector3 positionA = transform.position + perpendicular * cellsizeCenter;
                    Vector3 positionB = transform.position - perpendicular * cellsizeCenter;

                    //Gets The node by their world position
                    firstnode = gridManager.GetNodeFromClosestWorldPosition(positionA);
                    secondnode = gridManager.GetNodeFromClosestWorldPosition(positionB);

                    if (firstnode == null || secondnode == null || firstnode == secondnode)
                    {
                        Debug.LogWarning("Invalid Edge Obstacle node Reference.", this);
                        return false;
                    }

                    
                }
                break;
            case ObstacleType.FloorConnectors:
                {
                    connector = GetComponent<FloorConnector>();

                    if (connector == null) 
                    {
                        Debug.LogWarning("Floor Connector Obstacle is missing FloorConnector Component");
                        return false;
                    }
                }
                break;

            default:
                return false;

        }
        UpdateOccupancyRegistration(!blocked);

        return occupancyRegistered == blocked;
        
    }

    //Destruction Stages

    private void UpdateDestructionStage() 
    {
        if (currentData == null) 
        {
            Debug.LogWarning("This was called before CurrentData was set");
            return;
        }

        if (destructionStages == null || destructionStages.Length == 0) 
        {
            currentStage = null;
            currentStageIndex = -1;
            ApplyDefaultObstacleInformation();
            return;

        }

        ObstacleDestructionStages selectedStage = null;

        //No Configured Stages: Use Default

        

      
     

        if (currentHealth <= 0) 
        {
            selectedStage = Array.Find(destructionStages, stage => stage != null && stage.stageName == ObstacleCondition.Destroyed);
        }
        else 
        {
            float healthPercentage = (float)currentHealth / maxHealth;

            //Find threshold that the current health meets

            foreach (ObstacleDestructionStages stage in destructionStages) 
            {
                if(stage == null || stage.stageName == ObstacleCondition.Destroyed) 
                {
                    continue;
                }

                if(healthPercentage < stage.healthThreshold) 
                {
                    continue;
                }

                if (selectedStage == null || stage.healthThreshold > selectedStage.healthThreshold) 
                {
                    selectedStage = stage;
                }
            }
        }
        
        //If No stage threshold matches, use default

        if(selectedStage == null) 
        {
            currentStage = null;
            currentStageIndex = -1;

            ApplyDefaultObstacleInformation();
            UpdateStageVisuals();
       

            return;
        }

        int selectedIndex = Array.IndexOf(destructionStages, selectedStage);
        //Don't reapply an unchanged stage
        if(selectedIndex == currentStageIndex) 
        {
            return;
        }

        currentStage = selectedStage;
        currentStageIndex = selectedIndex;

        ApplyStageProperties(selectedStage);
        UpdateStageVisuals();
        PlayStageParticleEffect(selectedStage);

    

    }
    private void ApplyStageProperties(ObstacleDestructionStages stage) 
    {
        blocksMovement = stage.blockMovement;
        blockLineOfSight = stage.blockLineofSight;
        blockProjectile = stage.blockProjectile;
        blockSound = stage.blockSound;

        stateOfCover = stage.coverState;
        coverHeight = stage.obstacleHeight;
   
    }

    private void UpdateStageVisuals() 
    {
        ObstacleDestructionStages[] stages = destructionStages;

        foreach(ObstacleDestructionStages stage in stages) 
        {
            if (stage == null || stage.stageVisual == null) 
            {
                continue;
            }

            stage.stageVisual.SetActive(false);
        }

        if (currentStage != null && currentStage.stageVisual != null) 
        {
            currentStage.stageVisual.SetActive(true);
        }
    }
    private void PlayStageParticleEffect(ObstacleDestructionStages stage)
    {
        if (stage == null || stage.stageParticleEffect == null)
        {
            return;
        }

        ParticleSystem effect = Instantiate(
            stage.stageParticleEffect,
            transform.position,
            transform.rotation
        );

        effect.Play();
    }

    //Occupancy Information

    private void UpdateOccupancyRegistration(bool unregister) 
    {
        if (occupancy == null) 
        {
            return;

        }

        if (unregister)
        {
            if (!occupancyRegistered)
            {
                return;
            }

            UnregisterObstacle();
            occupancyRegistered = false;
        }
        else 
        {
            if (occupancyRegistered) 
            {
                return;
            }
           
            RegisterObstacle();
            occupancyRegistered = IsValidForRegistration();

        }
        
    }

    //Registering the obstacle to occupancy
    private void RegisterObstacle() 
    {
        
        switch (typeOfObstacle) 
        {
            case ObstacleType.Solid:
                {
                    if (firstnode != null && nodeCoverage == NodeCoverage.Single)
                    {
                        occupancy.RegisterSolidObstacles(firstnode, this);
                    }
                    else if (firstnode != null && nodeCoverage == NodeCoverage.Multiple) 
                    {

                        foreach (GridNode node in occupiedNodes) 
                        {
                            occupancy.RegisterSolidObstacles(node, this);
                        }
                    }
                }
                break;
            case ObstacleType.Edges:
                {
                  
                        occupancy.RegisterEdgeObstacles(firstnode, secondnode, this);
                  
                    
                }
                break;
            case ObstacleType.FloorConnectors:
                {
                    
                    switch (connector.connectorType) 
                    {
                        case ConnectorType.Stairs:
                            {
                                foreach(GridNode node in connector.nodes) 
                                {
                                    if (node != null) 
                                    {
                                        occupancy.RegisterSolidObstacles(node, this);
                                    }
                                }
                            }
                            break;
                    }
                }
                break;
            case ObstacleType.WorldSpace:
                {
                    occupancy.RegisterWorldSpaceObstacles(this);
                
                }
                    break;
                default:
                {
                    Debug.LogWarning($"Obstacle Controller -> RegisterObstacles Method: This case ({obstacleName} , {typeOfObstacle} was not added");

                }
                break;
        }

    }
    public void UnregisterObstacle() 
    {
        switch (typeOfObstacle)
        {
            case ObstacleType.Solid:
                {
                    if (firstnode != null)
                    {
                        occupancy.UnregisterSolidObstacles(firstnode, this);
                    }
                }
                break;
            case ObstacleType.Edges:
                {
                    if (firstnode != null && secondnode != null)
                    {
                        occupancy.UnregisterEdgeObstacles(firstnode, secondnode, this);
                    }

                }
                break;
            case ObstacleType.FloorConnectors:
                {

                    switch (connector.connectorType)
                    {
                        case ConnectorType.Stairs:
                            {
                                foreach (GridNode node in connector.nodes)
                                {
                                    if (node != null)
                                    {
                                        occupancy.UnregisterSolidObstacles(node, this);
                                    }
                                }
                            }
                            break;
                    }
                }
                break;
            case ObstacleType.WorldSpace:
                occupancy.UnregisterWorldSpaceObstacles(this);
                break;
            default:
                {
                    Debug.LogWarning("Obstacle Controller -> RegisterObstacles Method: This case was not added");

                }
                break;
        }
    }
    private bool IsValidForRegistration()
    {
        switch (typeOfObstacle)
        {
            case ObstacleType.Solid:
                return firstnode != null;

            case ObstacleType.Edges:
                return firstnode != null && secondnode != null;

            case ObstacleType.FloorConnectors:
                return connector != null && connector.nodes != null &&
                       connector.nodes.Count > 0;

            default:
                return false;
        }
    }

    private bool TryGetOccupiedNodes() 
    {
        occupiedNodes.Clear();

        if (obstacleFootprint == null || obstacleFootprint.x <= 0 || obstacleFootprint.y <= 0) 
        {
            return false;
        }

        firstnode = gridManager.GetNodeFromClosestWorldPosition(anchor.position);

        if(firstnode == null) 
        {
            return false;
        }

        Dictionary<Vector2Int, GridNode> floorGrid = gridManager.GetGrid(firstnode.floorID);

        for (int x = 0; x < obstacleFootprint.x; x++) 
        {
            for (int z = 0; z < obstacleFootprint.y; z++) 
            {
                Vector2Int coordinate = firstnode.coords + new Vector2Int(x, z);

                if (!floorGrid.TryGetValue(coordinate, out GridNode node))
                {
                    occupiedNodes.Clear();
                    return false;
                
                }

                occupiedNodes.Add(node);
            }
        }

        return true;
        
    }
    private bool TryGetNodeAtPosition(Vector3 position,out GridNode node) 
    {
        node = gridManager.GetNodeFromClosestWorldPosition(position);

        if (node == null) 
        {
            return false;
        }

        Vector2 samplePosition = new Vector2(position.x, position.y);

        Vector2 nodePosition = new Vector2(node.cellWorldPosition.x, node.cellWorldPosition.z);

        float tolerance = gridManager.GridCellSize * 0.2f;
        return Vector2.Distance(samplePosition, nodePosition) <= tolerance;
    }
    public void SetValue_Durability(int health) 
    {
        if(maxHealth <= 0 || IsDestroyed) 
        {
            return;
        }

        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        UpdateDestructionStage();
    }
   
    public int GetValue_Durability() 
    {
        return currentHealth;
    }
    public void DestroyObstacle()
    {
        if (!isDestructible || IsDestroyed)
        {
            return;
        }

        IsDestroyed = true;

        currentHealth = 0;

        UpdateDestructionStage();
        UpdateOccupancyRegistration(true);

        //Particle Effects and Delayed GameObject Cleanup
        StartCoroutine(CleanupAfterDestruction());
    }

    //Will be moved into the obstacle logic script later
    public void ModifyDurability(int amount)
    {
        if(!isDestructible|| IsDestroyed || amount == 0 || maxHealth <= 0) 
        {
            return;
        }
       
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);

        ObstaclesCurrentHealth = currentHealth;
        UpdateDestructionStage();

        if (currentHealth <= 0) 
        {
            DestroyObstacle();
        }
    }

       

    public void ChangeState(CoverState state) //Needed later for when we change state 
    {
        stateOfCover = state;
    }

    //CleanUp
    private void OnDestroy() 
    {
        UpdateOccupancyRegistration(true);
    }

    private IEnumerator CleanupAfterDestruction() 
    {
        if (destructionEffectPrefab == null) 
        {
            Destroy(gameObject);
            yield break;
        }

        ParticleSystem effect = Instantiate(destructionEffectPrefab, transform.position, transform.rotation);

        effect.Play(true);

        //Determine how long is the effect
        float effectDuration = 0f;

        ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem system in systems) 
        {
            ParticleSystem.MainModule main = system.main;
            float totalDuration = main.startDelay.constantMax + main.duration + main.startLifetime.constantMax;

            effectDuration = Mathf.Max(effectDuration, totalDuration);
        }

        //Disable Physical colliders
        Collider obstacleCollider = GetComponentInChildren<Collider>();

        if (obstacleCollider != null) 
        {
            obstacleCollider.enabled = false;
        }

        yield return new WaitForSeconds(effectDuration);
        Destroy(gameObject);
    }
}