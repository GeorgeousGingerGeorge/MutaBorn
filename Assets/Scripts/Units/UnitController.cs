using UnityEngine;

public class UnitController : MonoBehaviour
{
    [Header("Unit's Size")]
    [SerializeField] private Vector2Int footPrintSize = Vector2Int.one;
    public Vector2Int footPrint => footPrintSize;

    [Header("Units Data- Status, Inventory, Equipment, Mutations")]

    [SerializeField] public string unitName; 
    [SerializeField] public string raceName;
    DynamicStats race = null;

    public UnitData data; 

    [Header("Reference Node to the Grid ")]
    public GridNode currentNode { get; private set; }
    public Occupancy occupancy;
    public float actionPoints;
    [Header("Controls Outline Visuals")]
  //  private OutlineController characterOutline;
    private bool isHovered = false;
    private bool isSelected = false;
    public void Start() 
    {
        InitializeUnitData();
        InitializeNode();
        Debug.Log(data.characterName);
        Debug.Log(data.raceName);
        Debug.Log(data.maxHealth);
        Debug.Log(data.maxMana);
        Debug.Log(data.maxActionPoints);
       
    }

  


    private void InitializeUnitData() 
    {
        race = GameDataBaseLoader.Instance.raceData.GetRace(raceName);
        data = UnitData.CreateNewUnit(unitName, race);
    }
    //Sets the Node of the unit,
    public void InitializeNode() 
    {
        currentNode = FindAnyObjectByType<GridManager>().GetNodeFromClosestWorldPosition(transform.position);
        occupancy = FindAnyObjectByType<Occupancy>();
        occupancy.RegisterUnit(currentNode, this);
        
    }
    public void SetNode(GridNode node) 
    {
        occupancy.UnregisterUnit(currentNode, this);
        currentNode = node;
        occupancy.RegisterUnit(currentNode, this);
    }
    public void SetHover(bool hover) 
    {
        isHovered = hover;

    }
    public void SetSelected(bool selected) 
    {
        isSelected = selected;
      

    }

    public bool GetSelected() 
    {
        return isSelected; 
    }

    public float CurrentActionPoints() 
    {
        return data.currentActionPoints;
    }
}
