using UnityEngine;
using System.Collections.Generic;

public class SoundDetectionSystem : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Occupancy occupancy;


    private Dictionary<UnitController, SoundAwareness> soundAwareness = new Dictionary<UnitController, SoundAwareness>();

    private Dictionary<GridNode, float> CalculateSoundReach(GridNode origin,int loudness)
    {
        Dictionary<GridNode, float> distances = new Dictionary<GridNode, float>();

        if (origin == null || loudness < 0 || occupancy == null)
        {
            return distances;
        }
        List<GridNode> unvisited = new List<GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();

        distances[origin] = 0f;
        unvisited.Add(origin);

        while (unvisited.Count > 0)
        {
            GridNode current = unvisited[0];

            foreach (GridNode node in unvisited)
            {
                if (distances[node] < distances[current])
                { 
                    current = node; 
                }
            }

            unvisited.Remove(current);

            if (!visited.Add(current)) 
            {
                continue;

            }


            float currentCost = distances[current];

            if (currentCost > loudness)
            {
                break; 
            
            }

            foreach (GridNode neighbor in current.GetNeighbors())
            {
                if (neighbor == null || visited.Contains(neighbor))
                {
                    continue; 
                
                }

                // Check whether an obstacle blocks sound propagation.
                ObstacleController solidObstacle = occupancy.FindSolidObstacle(neighbor);

                if (solidObstacle != null && solidObstacle.blockSound)
                { 
                    continue; 
                
                }

                ObstacleController edgeObstacle = occupancy.FindEdgeObstacle(current, neighbor);

                if (edgeObstacle != null && edgeObstacle.blockSound)
                {
                    continue;
                }


                float dx = Mathf.Abs(neighbor.coords.x - current.coords.x);
                float dy = Mathf.Abs(neighbor.coords.y - current.coords.y);

                bool diagonal = dx == 1f && dy == 1f;

                float stepCost = neighbor.GetNodeMovementCost();

                if (diagonal)
                {
                    stepCost *= Mathf.Sqrt(2f);
                }
                if (stepCost < 0f)
                {
                    continue;
                }
                float newCost = currentCost + stepCost;

                if (newCost > loudness)
                {
                    continue;
                }
                if (!distances.TryGetValue(neighbor, out float oldCost) || newCost < oldCost)
                {
                    distances[neighbor] = newCost;

                    if (!unvisited.Contains(neighbor))
                    {
                        unvisited.Add(neighbor);
                    }
                }
            }
        }

        return distances;
    }
    public void RegisterSound(GridNode origin, SoundType type, int loudness, UnitController source = null) 
    {
        if (origin == null || loudness <= 0 || occupancy == null) 
        {
            return;
        }

        Dictionary<GridNode, float> soundDistances = CalculateSoundReach(origin, loudness);

        foreach (UnitController unit in occupancy.GetUnits()) 
        {
            if (unit == null || unit.currentNode == null || unit == source)
            {
                continue;
            }

            UnitFaction faction = unit.GetComponent<UnitFaction>();

            if (faction == null || faction.GetFactionID() == "playerFaction") 
            {
                continue;
            }

            //Only AI units should receieve soundAwareness

            if (!soundDistances.TryGetValue(unit.currentNode, out float distance)) 
            {
                continue;
            }

            int pathCost = Mathf.CeilToInt(distance);

            if (soundAwareness.TryGetValue(unit, out SoundAwareness existing)) 
            {
                //Replace currentSound if the new one is closer

                if (pathCost >= existing.pathCost) 
                {
                    continue;
                }

                existing.soundOrigin = origin;
                existing.soundType = type;
                existing.loudness = loudness;
                existing.pathCost = pathCost;


            }
            else 
            {
                soundAwareness.Add(unit, new SoundAwareness(origin, type, loudness, pathCost));
            }
        }
    }
    public SoundAwareness GetSoundAwareness(UnitController unit) 
    {
        if (unit == null) 
        {
            return null;
        }
        soundAwareness.TryGetValue(unit, out SoundAwareness awareness);

        return awareness;
    }

    public void ClearSoundAwareness(UnitController unit) 
    {
        if (unit == null) 
        {
            return;
        }

        soundAwareness.Remove(unit);    
    }
}
