using UnityEngine;

public class UnitController : MonoBehaviour
{
    [Header("Reference Node to the Grid ")]
    public GridNode currentNode { get; private set; }
    public Occupancy occupancy;
    [Header("Unit Stats")]
    [SerializeField]private UnitStats getStats;

    public float actionPoints;
    [Header("Controls Outline Visuals")]
  //  private OutlineController characterOutline;
    private bool isHovered = false;
    private bool isSelected = false;
    public void Start() 
    {
        getStats = this.GetComponent<UnitStats>();
       // GetOutlineController();
        actionPoints = getStats.MaxActionPoints * 1f;

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
        // GetOutlineController();
        //characterOutline.SetupRenderer();
        //characterOutline.SetHovered(isHovered);
        //characterOutline.UpdateController(LayerMask.NameToLayer("Units"), this);
        //characterOutline.UpdateColors();
    }
    public void SetSelected(bool selected) 
    {
        isSelected = selected;
      
        //GetOutlineController();
        //characterOutline.SetupRenderer();
        //characterOutline.SetSelected(isSelected);
        //characterOutline.UpdateController(LayerMask.NameToLayer("Units"), this);
        //characterOutline.UpdateColors();
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
