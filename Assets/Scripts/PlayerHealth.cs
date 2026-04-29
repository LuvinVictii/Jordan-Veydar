using UnityEngine;
using UnityEngine.Events;
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }
    public UnityEvent<float> onHealthChanged;
    public UnityEvent onDeath;
    void Awake() => currentHealth = maxHealth;
    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
        if (currentHealth <= 0f) Die();
    }
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }
    private void Die()
    {
        onDeath?.Invoke();
        Debug.Log(gameObject.name + " has died.");
    }
}