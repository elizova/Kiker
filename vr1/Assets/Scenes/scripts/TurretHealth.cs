using UnityEngine;

public class TurretHealth : MonoBehaviour
{
    public float health = 100f;

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"Turret took {damage} damage. Health: {health}");

        if (health <= 0)
        {
            DestroyTurret();
        }
    }

    void DestroyTurret()
    {
        Debug.Log("Turret destroyed!");
        //gameObject.SetActive(false);
        Destroy(gameObject);
    }
}