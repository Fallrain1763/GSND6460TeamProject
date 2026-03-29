using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    PlayerUI ui;

    float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        ui = GetComponent<PlayerUI>();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log($"Player HP: {currentHealth}/{maxHealth}");
        ui.UpdateUI(currentHealth/maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        Debug.Log("Player died!");
        gameObject.SetActive(false);
    }
}