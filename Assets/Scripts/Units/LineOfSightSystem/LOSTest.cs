using UnityEngine;
using System.Collections.Generic;

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

    [ContextMenu("Test Physical Line Of Sight")]
    private void TestPhysicalLineOfSight()
    {
        if (lineOfSightSystem == null ||
            observer == null ||
            target == null)
        {
            Debug.LogWarning("LOS Test: Missing references.");
            return;
        }

        GridManager gridManager = FindAnyObjectByType<GridManager>();

        GridNode node = gridManager.GetNodeFromGridPosition(1, new Vector2Int(8, 9));
        Vector3 nodePosition = node.cellWorldPosition;
        Dictionary<Vector2Int, GridNode> grid =gridManager.GetGrid(node.floorID);

        bool canSee = lineOfSightSystem.HasPhysicalLineOfSight(
            observer.visionOrigin.position,
            nodePosition
        );

        Debug.Log(
            $"Physical LOS: {observer.name} -> {grid.ContainsKey(node.coords)} , {node.coords} : " +
            $"{(canSee ? "CLEAR" : "BLOCKED")}",
            this
        );
    }
    [ContextMenu("Test Node Visibility")]
    private void TestNodeVisibility()
    {
        if (lineOfSightSystem == null ||
            observer == null ||
            observer.currentNode == null)
        {
            Debug.LogError("Missing LOS references or observer node.");
            return;
        }

        GridManager gridManager =
            FindAnyObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError("GridManager not found.");
            return;
        }

        GridNode testNode = gridManager.GetNodeFromClosestWorldPosition(
            transform.position
        );

        if (testNode == null)
        {
            Debug.LogError("No GridNode found at test object's position.");
            return;
        }

        bool visible = lineOfSightSystem.IsNodeVisible(
            observer,
            testNode
        );
        lineOfSightSystem.DebugDrawTilePoints(testNode);

        Debug.Log(
            $"Node {testNode.coords} | Floor {testNode.floorID} | " +
            $"Visible: {visible}"
        );
    }

    [ContextMenu("Test All Visible Nodes")]
    private void TestAllVisibleNodes()
    {
        List<GridNode> visibleNodes =
            lineOfSightSystem.GetVisibleNodes(observer);

        Debug.Log($"Visible nodes: {visibleNodes.Count}");

        foreach (GridNode node in visibleNodes)
        {
            Debug.Log(
                $"Visible Node: {node.coords} | Floor: {node.floorID}"
            );
        }
    }

}