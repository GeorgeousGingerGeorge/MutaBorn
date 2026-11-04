using UnityEngine;

public class LOSTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LineOfSight lineOfSightSystem;
    [SerializeField] private UnitController observer;

    [Header("Test Target")]
    [SerializeField] private UnitController target;

    [ContextMenu("Test Line Of Sight")]
    private void TestLineOfSight()
    {
        if (lineOfSightSystem == null ||
            observer == null ||
            target == null)
        {
            Debug.LogWarning("LOS Test: Missing references.");
            return;
        }

        bool canSee = lineOfSightSystem.CanSee(observer, target);

        Debug.Log(
            $"LOS Test: {observer.name} -> {target.name}: " +
            $"{(canSee ? "VISIBLE" : "NOT VISIBLE")}",
            this
        );
    }
}