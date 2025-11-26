using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isInvulnerable = false;

    [Header("UI Reference")]
    public HealthBar healthBar;

    [Header("Events")]
    public UnityEvent onDamageTaken;
    public UnityEvent onHeal;
    public UnityEvent onDeath;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (isInvulnerable || currentHealth <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        onDamageTaken?.Invoke();

        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth, maxHealth);

        Debug.Log($"Player took {damage} damage! Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        if (currentHealth <= 0) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        onHeal?.Invoke();

        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth, maxHealth);

        Debug.Log($"Player healed for {healAmount}! Health: {currentHealth}");
    }

    void Die()
    {
        Debug.Log("Player died!");

        onDeath?.Invoke();

        // Смерть
    }

    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
    }

    public void RestoreFullHealth()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth, maxHealth);
    }
}