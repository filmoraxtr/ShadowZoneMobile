using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Shadow Zone Mobile — Düşman Yapay Zekası
/// State Machine: Patrol, Chase, Attack, Dead.
/// EnemyHealth ile aynı Enemy objesine eklenir.
/// Oyuncuda Player tag'i olmalı.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    private enum State
    {
        Patrol,
        Chase,
        Attack,
        Dead
    }

    [Header("Algılama")]
    [SerializeField] private float sightRange = 20f;
    [SerializeField] private float sightAngle = 110f;
    [SerializeField] private float hearingRange = 10f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Saldırı")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackInterval = 1.4f;
    [SerializeField, Range(0f, 1f)] private float hitChance = 0.55f;
    [SerializeField] private AudioSource attackAudio;

    [Header("Hareket")]
    [SerializeField] private float patrolSpeed = 2.2f;
    [SerializeField] private float chaseSpeed = 4.8f;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float loseTargetTime = 4f;

    [Header("Optimizasyon")]
    [SerializeField] private float thinkInterval = 0.2f;

    private NavMeshAgent agent;
    private EnemyHealth health;
    private Transform player;
    private PlayerHealth playerHealth;
    private Animator animator;

    private State currentState;
    private int patrolIndex;
    private float patrolWaitTimer;
    private float loseTargetTimer;
    private float nextAttackTime;
    private float nextThinkTime;
    private Vector3 lastKnownPlayerPos;
    private bool hasLastKnownPos;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        animator = GetComponent<Animator>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerHealth = playerObject.GetComponent<PlayerHealth>();
        }

        if (health != null)
        {
            health.OnScoreGranted += HandleEnemyDead;
        }
    }

    private void Start()
    {
        currentState = State.Dead;
        TransitionTo(State.Patrol);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnScoreGranted -= HandleEnemyDead;
        }
    }

    private void Update()
    {
        if (currentState == State.Dead) return;
        if (agent == null || !agent.enabled) return;

        if (Time.time >= nextThinkTime)
        {
            nextThinkTime = Time.time + thinkInterval;
            Think();
        }

        Act();
        UpdateAnimator();
    }

    private void Think()
    {
        if (player == null)
        {
            TransitionTo(State.Patrol);
            return;
        }

        if (playerHealth != null && playerHealth.IsDead)
        {
            TransitionTo(State.Patrol);
            return;
        }

        bool canSee = CanSeePlayer();
        bool canHear = Vector3.Distance(transform.position, player.position) <= hearingRange;

        if (canSee)
        {
            lastKnownPlayerPos = player.position;
            hasLastKnownPos = true;
            loseTargetTimer = 0f;

            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= attackRange)
            {
                TransitionTo(State.Attack);
            }
            else
            {
                TransitionTo(State.Chase);
            }
        }
        else if (canHear)
        {
            lastKnownPlayerPos = player.position;
            hasLastKnownPos = true;
            TransitionTo(State.Chase);
        }
        else
        {
            if (currentState == State.Chase || currentState == State.Attack)
            {
                loseTargetTimer += thinkInterval;

                if (loseTargetTimer >= loseTargetTime)
                {
                    hasLastKnownPos = false;
                    loseTargetTimer = 0f;
                    TransitionTo(State.Patrol);
                }
            }
        }
    }

    private void Act()
    {
        switch (currentState)
        {
            case State.Patrol:
                DoPatrol();
                break;

            case State.Chase:
                DoChase();
                break;

            case State.Attack:
                DoAttack();
                break;
        }
    }

    private void DoPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;

        if (!agent.pathPending && agent.remainingDistance < 0.6f)
        {
            patrolWaitTimer += Time.deltaTime;

            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolWaitTimer = 0f;
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[patrolIndex].position);
            }
        }
    }

    private void DoChase()
    {
        agent.isStopped = false;

        Vector3 destination = hasLastKnownPos ? lastKnownPlayerPos : transform.position;
        agent.SetDestination(destination);

        if (hasLastKnownPos &&
            Vector3.Distance(transform.position, lastKnownPlayerPos) < 1.5f &&
            !CanSeePlayer())
        {
            loseTargetTimer += Time.deltaTime;
        }
    }

    private void DoAttack()
    {
        if (player == null)
        {
            TransitionTo(State.Patrol);
            return;
        }

        agent.isStopped = true;

        FaceTarget(player.position);

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange + 1f)
        {
            TransitionTo(State.Chase);
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackInterval;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (attackAudio != null)
        {
            attackAudio.Play();
        }

        if (!HasLineOfSight()) return;
        if (Random.value > hitChance) return;
        if (playerHealth == null || playerHealth.IsDead) return;

        playerHealth.TakeDamage(attackDamage);
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * 1.4f;
        Vector3 targetPosition = player.position + Vector3.up * 1.0f;
        Vector3 directionToPlayer = targetPosition - origin;
        float distance = directionToPlayer.magnitude;

        if (distance > sightRange) return false;

        float angle = Vector3.Angle(transform.forward, directionToPlayer.normalized);

        if (angle > sightAngle * 0.5f) return false;

        if (Physics.Raycast(
                origin,
                directionToPlayer.normalized,
                distance,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return true;
    }

    private bool HasLineOfSight()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * 1.4f;
        Vector3 targetPosition = player.position + Vector3.up * 1.0f;
        Vector3 direction = targetPosition - origin;

        return !Physics.Raycast(
            origin,
            direction.normalized,
            direction.magnitude,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void TransitionTo(State newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (newState)
        {
            case State.Patrol:
                agent.speed = patrolSpeed;
                agent.isStopped = false;

                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    patrolIndex = Mathf.Clamp(patrolIndex, 0, patrolPoints.Length - 1);
                    agent.SetDestination(patrolPoints[patrolIndex].position);
                }

                break;

            case State.Chase:
                agent.speed = chaseSpeed;
                agent.isStopped = false;
                break;

            case State.Attack:
                agent.speed = 0f;
                agent.isStopped = true;
                break;

            case State.Dead:
                if (agent != null && agent.enabled)
                {
                    agent.isStopped = true;
                    agent.enabled = false;
                }

                enabled = false;
                break;
        }
    }

    private void HandleEnemyDead(int score)
    {
        TransitionTo(State.Dead);
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            10f * Time.deltaTime
        );
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", agent.velocity.magnitude, 0.1f, Time.deltaTime);
        animator.SetBool("IsAttacking", currentState == State.Attack);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.1f);

        UnityEditor.Handles.DrawSolidArc(
            transform.position,
            Vector3.up,
            Quaternion.Euler(0f, -sightAngle * 0.5f, 0f) * transform.forward,
            sightAngle,
            sightRange
        );

        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }
#endif
}
