using UnityEngine;

[RequireComponent(typeof(WorldTrap))]
public class WorldTrapDestructible : MonoBehaviour
{
    [SerializeField, Min(1)] private int maximumHealth = 10;
    [SerializeField] private GameObject visualRoot;
    private WorldTrap trap;
    private int health;

    private void Awake()
    {
        trap = GetComponent<WorldTrap>();
        health = maximumHealth;

    }

    // Receiver for object-damage actions; the combat object-targeting hookup is separate.
    public void TakeDamage(int amount)
    {
        if (health <= 0 || amount <= 0)
        {

            return;

        }

        health = Mathf.Max(0, health - amount);
        if (health == 0)
        {
            Detonate();

        }

    }

    public void Detonate()
    {
        if (health < 0 || !trap.IsArmed)
        {

            return;

        }

        health = -1;
        trap.Fire();

    }

    public void HideAfterExplosion()
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(false);

        }

    }

}
