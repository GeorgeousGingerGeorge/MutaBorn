using UnityEngine;
using UnityEngine.Rendering;

public class TileController : MonoBehaviour
{
    [Header("Reference Node")]
    public GridNode node { get; private set; }

    [Header("Visual Elements")]
    [SerializeField] private GameObject highlightOverlay;
    [SerializeField] private Renderer highlightRenderer;
    private bool isHovered;
    private bool isSelected;
    private RGB_Color currentColor = new RGB_Color(0f, 0f, 0f, 0f);
    public void Initialize(GridNode node)
    {
        this.node = node;

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
        SetHighlight(false, Color.white);
    }
    public void SetHover(bool hover) 
    {
        Debug.Log("Do we ever Set Hover");
        isHovered = hover;
        UpdateHighLight();
        
    }
    public void SetSelected(bool selected) 
    {
        isSelected = selected;
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
        bool visible = isHovered || isSelected;
        Color color = Color.red; 
        if (isSelected) 
        {
            color = Color.cyan;
        }
       
        SetHighlight(visible, color);
    }
    public void GetColors() 
    {
        highlightRenderer = highlightOverlay.GetComponent<Renderer>();
    }

    
}
