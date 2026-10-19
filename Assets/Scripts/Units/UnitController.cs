using UnityEngine;

public class UnitController : MonoBehaviour
{
    [Header("Reference Node to the Grid ")]
    public GridNode currentNode { get; private set; }

    public UnitStats getStats;

    public float actionPoints;
    [Header("Controls Outline Visuals")]
    private OutlineController characterOutline;
    private bool isHovered = false;
    private bool isSelected = false;
    public void Awake() 
    {
        getStats = this.GetComponent<UnitStats>();
        GetOutlineController();
        actionPoints = getStats.MaxActionPoints * 1f;
    }

 
    //Sets the Node of the unit,
    public void InitializeNode() 
    {
        currentNode = FindAnyObjectByType<GridManager>().GetNodeFromClosestWorldPosition(transform.position);
        
    }
    public void SetNode(GridNode node) 
    {
        currentNode = node;
    }

    //Controls the Outline of the unit 
    public void GetOutlineController() 
    {
        characterOutline = GetComponent<OutlineController>();
    }
    public void SetHover(bool hover) 
    {
        isHovered = hover;
        GetOutlineController();
        characterOutline.SetupRenderer();
        characterOutline.SetHovered(isHovered);
        characterOutline.UpdateController(LayerMask.NameToLayer("Units"), this);
        characterOutline.UpdateColors();
    }
    public void SetSelected(bool selected) 
    {
        isSelected = selected;
      
        GetOutlineController();
        characterOutline.SetupRenderer();
        characterOutline.SetSelected(isSelected);
        characterOutline.UpdateController(LayerMask.NameToLayer("Units"), this);
        characterOutline.UpdateColors();
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
