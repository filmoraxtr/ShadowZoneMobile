using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Shadow Zone Mobile — Oyuncu Can Sistemi
/// EnemyAI hasar vermek için TakeDamage() çağırır.
/// GameManager ölümü OnDeath event'inden dinler.
/// Health bar UI Slider doğrudan buradan güncellenir.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Can Ayarları")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float regenDelay = 4f;
    [SerializeField] private float regenPerSecond = 8f;

    [Header("UI Opsiyonel")]
    [SerializeField] private UnityEngine.UI.Slider healthBar;
    [SerializeField] private UnityEngine.UI.Image damageFlashImage;

    [Header("Inspector Eventleri")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private float lastDamageTime = -999f;
    private float flashAlpha;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = CurrentHealth;
        }

        if (damageFlashImage != null)
        {
            damageFlashImage.color = new Color(1f, 0f, 0f, 0f);
        }
    }

    private void Update()
    {
        HandleRegen();
        HandleDamageFlash();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        lastDamageTime = Time.time;
        flashAlpha = 0.45f;

        PushHealthToUI();
        onDamaged?.Invoke();

#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        PushHealthToUI();
    }

    private void HandleRegen()
    {
        if (IsDead || regenPerSecond <= 0f) return;
        if (CurrentHealth >= maxHealth) return;
        if (Time.time - lastDamageTime < regenDelay) return;

        CurrentHealth = Mathf.Min(
            maxHealth,
            CurrentHealth + regenPerSecond * Time.deltaTime
        );

        PushHealthToUI();
    }

    private void HandleDamageFlash()
    {
        if (damageFlashImage == null) return;

        flashAlpha = Mathf.Lerp(flashAlpha, 0f, 4f * Time.deltaTime);
        damageFlashImage.color = new Color(1f, 0f, 0f, flashAlpha);
    }

    private void PushHealthToUI()
    {
        if (healthBar != null)
        {
            healthBar.value = CurrentHealth;
        }

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Die()
    {
        IsDead = true;

        OnDeath?.Invoke();
        onDeath?.Invoke();
    }
}
