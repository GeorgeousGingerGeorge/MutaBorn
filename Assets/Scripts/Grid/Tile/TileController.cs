using UnityEngine;


public class TileController : MonoBehaviour
{
    [Header("Reference Node")]
    public GridNode node { get; private set; }
    [SerializeField]private TerrainType _terrain;
    [Header("Visual Elements")]
    [SerializeField] private GameObject highlightOverlay;
    [SerializeField] private Renderer highlightRenderer;

    [Header("Controls Color states")]
    private bool isHovered;
    private bool isSelected;
    private bool isInMovementRange;
    public bool isUnitSelected { get; private set; }
   
    public void Initialize(GridNode node)
    {
        this.node = node;

        node.SetTerrain(_terrain);

        if (highlightOverlay == null) 
        {
            Transform overlay = transform.Find("HighlightOverlay");

            if (overlay != null) 
            {
                highlightOverlay = overlay.gameObject;
            }
        }

        if (highlightRenderer == null && highlightOverlay != null) 
        {
            highlightRenderer = highlightOverlay.GetComponentInChildren<Renderer>();
        }

        isHovered = false;
        isSelected = false;
        isInMovementRange = false;
        SetHighlight(false, Color.white);
    }
    public void SetHover(bool hover) 
    {
        
        isHovered = hover;
        UpdateHighLight();
        
    }
    public void SetSelected(bool selected) 
    { 
        isSelected = selected;
        UpdateHighLight();
    }
    public void SetMovementRange(bool active)
    {
        isInMovementRange = active;
        UpdateHighLight();
    }
    public void SetUnitSelected(bool selected) 
    {
        isUnitSelected = selected;
        UpdateHighLight();
    }
    public void SetHighlight(bool visible, Color color) 
    {
        highlightOverlay.SetActive(visible);
        if(visible && highlightRenderer != null) 
        {
            
            highlightRenderer.material.color = color; 
        }
    }
   
    private void UpdateHighLight() 
    {
        RGB_Color tileColors = new RGB_Color(0f, 0f, 0f, 0f);  // Assigning it clear
        bool visible = isHovered || isSelected || isInMovementRange || isUnitSelected;
        Color color = new Color(tileColors.red, tileColors.green, tileColors.blue, tileColors.alpha);
        tileColors.alpha = 0.15f;


        if (isSelected)
        {
            
            tileColors.green = 0.75f;
            tileColors.blue = 1f;
            tileColors.alpha = 0.5f; 
            color = new Color(tileColors.red, tileColors.green, tileColors.blue, tileColors.alpha);
        }
        else if (isUnitSelected) 
        {
            tileColors.red = 1f;
            tileColors.green = 1f;
            tileColors.alpha = 1f;
            color = new Color(tileColors.red, tileColors.green, tileColors.blue, tileColors.alpha);

        }
        else if (isInMovementRange)
        {
            tileColors.red = 0.2f;
            tileColors.green = 0.45f;


            color = new Color(tileColors.red, tileColors.green, tileColors.blue, tileColors.alpha);
        }
        else if (isHovered)
        {
            tileColors.red = 0.5f;
            tileColors.green = 0.45f;
            tileColors.blue = 0.8f;

            color = new Color(tileColors.red, tileColors.green, tileColors.blue, tileColors.alpha);
        }
       
        SetHighlight(visible, color);
    }
    public void GetColors() 
    {
        highlightRenderer = highlightOverlay.GetComponent<Renderer>();
    }

    
}
