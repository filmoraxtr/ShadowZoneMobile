using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Shadow Zone Mobile — Düşman Can Sistemi
/// WeaponBase içindeki IDamageable interface'ini kullanır.
/// EnemyAI ile aynı Enemy objesinde durur.
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Can")]
    [SerializeField] private float maxHealth = 60f;

    [Header("Hasar Geri Bildirimi")]
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private float hitFlashDuration = 0.08f;
    [SerializeField] private Color hitColor = Color.red;

    [Header("Ragdoll")]
    [SerializeField] private Rigidbody[] ragdollBodies;
    [SerializeField] private float ragdollForce = 400f;
    [SerializeField] private float destroyDelay = 5f;

    [Header("Ödül")]
    [SerializeField] private int scoreValue = 100;

    public event Action<int> OnScoreGranted;

    public bool IsDead { get; private set; }
    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    private Animator animator;
    private NavMeshAgent navAgent;
    private Collider mainCollider;
    private Color originalColor;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        CurrentHealth = maxHealth;

        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        mainCollider = GetComponent<Collider>();

        if (bodyRenderer != null)
        {
            originalColor = bodyRenderer.material.color;
        }

        SetRagdollActive(false);
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(HitFlash());

        if (CurrentHealth <= 0f)
        {
            Die(hitPoint, hitNormal);
        }
    }

    private IEnumerator HitFlash()
    {
        if (bodyRenderer == null) yield break;

        bodyRenderer.material.color = hitColor;

        yield return new WaitForSeconds(hitFlashDuration);

        if (!IsDead)
        {
            bodyRenderer.material.color = originalColor;
        }
    }

    private void Die(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (IsDead) return;

        IsDead = true;

        OnScoreGranted?.Invoke(scoreValue);

        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.enabled = false;
        }

        if (animator != null)
        {
            animator.enabled = false;
        }

        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }

        SetRagdollActive(true);
        ApplyDeathForce(hitPoint, hitNormal);

        StartCoroutine(DestroyAfterDelay());
    }

    private void SetRagdollActive(bool active)
    {
        if (ragdollBodies == null) return;

        foreach (Rigidbody rb in ragdollBodies)
        {
            if (rb == null) continue;

            rb.isKinematic = !active;

            Collider col = rb.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = active;
            }
        }
    }

    private void ApplyDeathForce(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (ragdollBodies == null || ragdollBodies.Length == 0) return;

        Rigidbody closest = null;
        float minDist = float.MaxValue;

        foreach (Rigidbody rb in ragdollBodies)
        {
            if (rb == null) continue;

            float distance = Vector3.Distance(rb.transform.position, hitPoint);

            if (distance < minDist)
            {
                minDist = distance;
                closest = rb;
            }
        }

        if (closest != null)
        {
            Vector3 force = (-hitNormal + Vector3.up * 0.3f).normalized * ragdollForce;
            closest.AddForce(force, ForceMode.Impulse);
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);

        Destroy(gameObject);
    }
}
