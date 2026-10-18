using Unity.AppUI.UI;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.CanvasScaler;

public struct RGB_Color
{
    public float red { get; set; }
    public float green { get; set; }
    public float blue { get; set; }
    public float alpha { get; set; }

    public RGB_Color(float r, float g, float b, float a) 
    {
        red = r;
        green = g;
        blue = b;
        alpha = a;
    }
};
public class OutlineController : MonoBehaviour
{
    
    [Header("Outline Renderer (ShaderGraph)")]
    [SerializeField] private Renderer outlineRenderer;
    private MaterialPropertyBlock properties;
    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
    [Header("Layers for Outlines")]
    [SerializeField]private LayerMask obstacleLayer;
    [SerializeField]private LayerMask unitLayer;

    [Header("Selection/Hover Color Control")]
  
    private Color hoverColor;
    private Color selectedColor;
    private bool isHovered = false;
    private bool isSelected = false;

    
    
    public void SetupRenderer() 
    {
        properties = new MaterialPropertyBlock();
        outlineRenderer.enabled = false; 
    }

    public void SetHovered(bool hovered) 
    {
        isHovered = hovered;
    }
    public void SetSelected(bool selected) 
    {
        isSelected = selected; 
    }

    public void UpdateColors() 
    {
        outlineRenderer.GetPropertyBlock(properties);
        Color color;
        if (isSelected)
        {
            color = selectedColor;
        }
        else if (isHovered)
        {
            color = hoverColor;
        }
        else 
        {
            color = Color.clear; 
        }

        properties.SetColor(OutlineColorID, color);
        outlineRenderer.SetPropertyBlock(properties);

        outlineRenderer.enabled = isHovered || isSelected;
    }

    public void UpdateController(LayerMask targetLayer, UnitController unit = null, ObstacleController obstacle = null) 
    {
        RGB_Color changeColor = new RGB_Color(0f, 0f, 0f, 0f);

        switch (targetLayer)
        {
            case 6: //Unit Layer
                {
                    Debug.Log($"Do we enter the Units case. {targetLayer.ToString()}");
                    if (unit.CompareTag("Player"))
                    {
                        Debug.Log("UnitsLayer, Player, Do we enter?");
                        changeColor.green = 0.5f;
                        changeColor.blue = 1f;
                        changeColor.alpha = 0.15f;
                        hoverColor = new Color(changeColor.red, changeColor.green, changeColor.blue, changeColor.alpha);

                        changeColor.green = 1f;
                        changeColor.alpha = 0.5f;
                        selectedColor = new Color(changeColor.red, changeColor.green, changeColor.blue, changeColor.alpha);
                    }
                    else if (unit.CompareTag("Enemy"))
                    {
                        changeColor.red = 1f;
                        changeColor.alpha = 0.15f;
                        hoverColor = new Color(changeColor.red, changeColor.green, changeColor.blue, changeColor.alpha);

                        changeColor.alpha = 0.5f;
                        selectedColor = new Color(changeColor.red, changeColor.green, changeColor.blue, changeColor.alpha);
                    }
                    else if (unit.CompareTag("Ally"))
                    {
                        changeColor.green = 1f;
                        changeColor.alpha = 0.15f;
                        hoverColor = new Color(changeColor.red, changeColor.green, changeColor.blue, changeColor.alpha);

                        changeColor.alpha = 0.5f;
                        selectedColor = new Color(changeColor.red, changeColor.green, changeColor.blue, changeColor.alpha);
                    }
                    else if (unit.CompareTag("Neutral"))
                    {
                        changeColor.red = 1f;
                        changeColor.green = 1f;
                        changeColor.alpha = 0.15f;
                        hoverColor = new Color(changeColor.red, changeColor.green, changeColor.blue, changeColor.alpha);

                        changeColor.alpha = 0.5f;
                        selectedColor = new Color(changeColor.red, changeColor.green, changeColor.blue, changeColor.alpha);
                    }
                }
                break;
            case 7: //Obstacle Layer
                {

                }
                break;

        }
    }
}
