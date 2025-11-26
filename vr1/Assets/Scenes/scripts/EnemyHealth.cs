using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float health = 30f;

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"Enemy took {damage} damage. Health: {health}");

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Enemy died!");
        gameObject.SetActive(false); // Destroy(gameObject);
    }
}