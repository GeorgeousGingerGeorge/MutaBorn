using UnityEngine;

public class UnitController : MonoBehaviour
{
    [Header("Units Data- Status, Inventory, Equipment, Mutations")]
    UnitData data; 

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
        InitializeNode();

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

    //Controls the Outline of the unit 
    //public void GetOutlineController() 
    //{
    //    characterOutline = GetComponent<OutlineController>();
    //}
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
        return actionPoints;
    }
}
