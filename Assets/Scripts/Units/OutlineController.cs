using UnityEngine;
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

    [Header("Selection Circle")]
    [SerializeField] private LineRenderer selectionCircle;
    [SerializeField] private float circleRadius = 0.75f;
    [SerializeField, Range(3, 128)] private int circleSegments = 48;
    
    private Color hoverColor;
    private Color selectedColor;
    private bool isHovered = false;
    private bool isSelected = false;

    
    
    public void SetupRenderer() 
    {
        properties = new MaterialPropertyBlock();
        if (outlineRenderer != null)
        {
            outlineRenderer.enabled = false;

        }

        if (selectionCircle != null)
        {
            selectionCircle.useWorldSpace = false;
            selectionCircle.loop = true;
            selectionCircle.positionCount = circleSegments;

            for (int i = 0; i < circleSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / circleSegments;

                float x = Mathf.Cos(angle) * circleRadius;
                float z = Mathf.Sin(angle) * circleRadius;

                selectionCircle.SetPosition(i, new Vector3(x, 0f, z));
            }

            selectionCircle.enabled = false;
        }
    }

    // The preplaced model may be replaced by the race-defined model.
    public void RebindRaceModel(Transform oldVisualRoot, Transform newVisualRoot)
    {
        if (selectionCircle == null ||
            (oldVisualRoot != null && selectionCircle.transform.IsChildOf(oldVisualRoot)))
        {
            selectionCircle = newVisualRoot.GetComponentInChildren<LineRenderer>(true);

        }

        if (outlineRenderer != null && oldVisualRoot != null &&
            outlineRenderer.transform.IsChildOf(oldVisualRoot))
        {
            string oldRendererName = outlineRenderer.gameObject.name;
            outlineRenderer = null;
            foreach (Renderer candidate in newVisualRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (candidate.gameObject.name == oldRendererName)
                {
                    outlineRenderer = candidate;

                    break;

                }

            }

        }

        if (outlineRenderer != null)
        {
            SetupRenderer();

        }

    }

    public void SetHovered(bool hovered) 
    {
        isHovered = hovered;
    }
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionCircle != null)
        {
            selectionCircle.enabled = selected;
        }
    }

    public void UpdateColors() 
    {
        if (outlineRenderer == null)
        {
            return;

        }

        if (properties == null)
        {
            properties = new MaterialPropertyBlock();

        }

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
                    ;
                    if (unit.CompareTag("Player"))
                    {
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

    private void Awake()
    {
        if (selectionCircle == null)
        {
            return;
        }

        selectionCircle.useWorldSpace = false;
        selectionCircle.loop = true;
        selectionCircle.positionCount = circleSegments;

        for (int i = 0; i < circleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / circleSegments;

            float x = Mathf.Cos(angle) * circleRadius;
            float z = Mathf.Sin(angle) * circleRadius;

            selectionCircle.SetPosition(i, new Vector3(x, 0f, z));
        }

        selectionCircle.enabled = false;
    }
}
