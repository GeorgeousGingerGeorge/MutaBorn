using UnityEngine;

public class UnitController : MonoBehaviour
{
    [Header("Reference Node to the Grid ")]
    public GridNode currentNode { get; private set; }
    
    [Header("Controls Outline Visuals")]
    private OutlineController characterOutline;
    private bool isHovered = false;
    private bool isSelected = false; 
    


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
    }
    public void SetSelected(bool selected) 
    {
        isSelected = selected; 
    }

    
    
}
