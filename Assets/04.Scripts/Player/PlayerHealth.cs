using System;
using UnityEngine;

/// <summary>플레이어 체력/피격/무적시간/사망 처리.</summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    public float maxHealth = 100f;
    public float invulnerabilityDuration = 0.5f;

    [Range(0f, 0.9f)] public float armorReduction = 0f;

    float currentHealth;
    float invulnerableTimer;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0f;

    /// <summary>current, max</summary>
    public event Action<float, float> OnHealthChanged;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (invulnerableTimer > 0f) invulnerableTimer -= Time.deltaTime;
    }

    public void SetArmorReductionPercent(float percent)
    {
        armorReduction = Mathf.Clamp01(percent / 100f);
    }

    public void TakeDamage(float amount, Vector2 sourcePosition)
    {
        if (!IsAlive || invulnerableTimer > 0f) return;

        float reduced = amount * (1f - armorReduction);
        currentHealth = Mathf.Max(0f, currentHealth - reduced);
        invulnerableTimer = invulnerabilityDuration;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            GameManager.Instance?.OnPlayerDied();
        }
    }
}
