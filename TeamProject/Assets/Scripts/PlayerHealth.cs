using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    PlayerUI ui;

    float currentHealth;

    [Header("i-frames")]
    public float iframeDuration = 5f;
    public bool isInvulnerable = false;

    void Start()
    {
        currentHealth = maxHealth;
        ui = GetComponent<PlayerUI>();
    }

    public void TakeDamage(float amount)
    {
        if (isInvulnerable) return;
        currentHealth -= amount;
        Debug.Log($"Player HP: {currentHealth}/{maxHealth}");
        ui.UpdateUI(currentHealth/maxHealth);

        if (currentHealth <= 0f)
            Die();
            
        StartCoroutine(StartIFrame());
    }

    void Die()
    {
        Debug.Log("Player died!");
        gameObject.SetActive(false);
    }

    IEnumerator StartIFrame()
    {
        isInvulnerable = true;
        yield return new WaitForSecondsRealtime(iframeDuration);
        isInvulnerable = false;
    }
}