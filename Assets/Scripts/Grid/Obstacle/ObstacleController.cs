using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private ObstacleData data;

    public ObstacleData currentData { get; private set; }
    private int currentHealth;
    public void Awake()
    {
        currentData = data;
        if (currentData != null)
        {
            currentHealth = currentData.maxHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
    }

    public void RepairObstacle(int repair)
    {
        currentHealth += repair;
    }

    public void ChangeState() //Needed later for when we change state 
    {
        
    }
}
