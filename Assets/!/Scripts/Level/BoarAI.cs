using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BoarAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public PlayerBuildingStatus playerBuildingStatus;
    public Animator animator;

    [Header("Patrol")]
    public float patrolRadius = 15f;
    public float walkSpeed = 1.5f;
    public float idleMinTime = 2f;
    public float idleMaxTime = 5f;

    [Header("Detection")]
    public float detectionRange = 15f;
    [Range(0f, 360f)]
    public float fieldOfView = 120f;
    public float closeDetectionRange = 3f;
    public float eyeHeight = 1f;

    [Header("Chase")]
    public float runSpeed = 4f;

    [Header("Attack")]
    public float attackRange = 1.8f;
    public float attackCooldown = 2f;
    public float attackDuration = 1f;

    private NavMeshAgent agent;
    private Vector3 patrolCenter;

    private enum BoarState
    {
        Idle,
        Walk,
        Chase,
        Attack
    }

    private BoarState state;
    private bool chasingPlayer;

    private float idleTimer;
    private float attackTimer;
    private float nextAttackTime;
    private float repathTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        patrolCenter = transform.position;

        if (player != null && playerBuildingStatus == null)
        {
            playerBuildingStatus =
                player.GetComponent<PlayerBuildingStatus>();
        }

        if (player == null || playerBuildingStatus == null)
        {
            Debug.LogError(
                "BoarAI: Player oder PlayerBuildingStatus fehlt!",
                this
            );
            enabled = false;
            return;
        }

        if (animator == null)
        {
            Debug.LogError("BoarAI: Animator fehlt!", this);
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "Boar steht nicht auf einem gebackenen NavMesh!",
                this
            );
            enabled = false;
            return;
        }

        agent.speed = walkSpeed;
        StartIdle();
    }

    void Update()
    {
        if (!agent.isOnNavMesh)
            return;

        // Gebäude sind sichere Zonen.
        if (playerBuildingStatus.IsInsideBuilding)
        {
            if (chasingPlayer ||
                state == BoarState.Chase ||
                state == BoarState.Attack)
            {
                chasingPlayer = false;
                StartIdle();
            }

            HandlePatrol();
            UpdateAnimator();
            return;
        }

        // Einmal entdeckt, verfolgt Boar den Spieler
        // bis dieser ein Gebäude betritt.
        if (!chasingPlayer && DetectPlayer())
        {
            chasingPlayer = true;
        }

        if (chasingPlayer)
        {
            HandleChaseAndAttack();
        }
        else
        {
            HandlePatrol();
        }

        UpdateAnimator();
    }

    bool DetectPlayer()
    {
        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance > detectionRange &&
            distance > closeDetectionRange)
        {
            return false;
        }

        // Sehr nahe Spieler auch hinter Boar erkennen.
        if (distance <= closeDetectionRange)
            return HasClearSight();

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        float angle = Vector3.Angle(
            transform.forward,
            direction
        );

        if (angle > fieldOfView * 0.5f)
            return false;

        return HasClearSight();
    }

    bool HasClearSight()
    {
        Vector3 origin =
            transform.position + Vector3.up * eyeHeight;

        Vector3 target =
            player.position + Vector3.up * eyeHeight;

        Vector3 direction = target - origin;

        if (Physics.Raycast(
            origin,
            direction.normalized,
            out RaycastHit hit,
            direction.magnitude,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            return hit.transform.root == player.root;
        }

        return false;
    }

    void HandlePatrol()
    {
        if (state == BoarState.Idle)
        {
            idleTimer -= Time.deltaTime;

            if (idleTimer <= 0f)
                ChoosePatrolPoint();
        }
        else if (state == BoarState.Walk)
        {
            if (!agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance)
            {
                StartIdle();
            }
        }
    }

    void ChoosePatrolPoint()
    {
        for (int i = 0; i < 15; i++)
        {
            Vector3 randomPoint =
                patrolCenter +
                Random.insideUnitSphere * patrolRadius;

            if (NavMesh.SamplePosition(
                randomPoint,
                out NavMeshHit hit,
                3f,
                agent.areaMask))
            {
                NavMeshPath path = new NavMeshPath();

                if (agent.CalculatePath(hit.position, path) &&
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.speed = walkSpeed;
                    agent.stoppingDistance = 0.2f;
                    agent.isStopped = false;
                    agent.SetPath(path);

                    state = BoarState.Walk;
                    return;
                }
            }
        }

        StartIdle();
    }

    void StartIdle()
    {
        agent.isStopped = true;
        agent.ResetPath();

        state = BoarState.Idle;
        idleTimer = Random.Range(idleMinTime, idleMaxTime);
    }

    void HandleChaseAndAttack()
    {
        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (state == BoarState.Attack)
        {
            agent.isStopped = true;
            FacePlayer();

            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
            {
                state = BoarState.Chase;
            }

            return;
        }

        if (distance <= attackRange && HasClearSight())
        {
            agent.isStopped = true;
            FacePlayer();

            if (Time.time >= nextAttackTime)
            {
                StartAttack();
            }
            else
            {
                state = BoarState.Chase;
            }

            return;
        }

        state = BoarState.Chase;

        agent.speed = runSpeed;
        agent.stoppingDistance = attackRange * 0.8f;
        agent.isStopped = false;

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            agent.SetDestination(player.position);
            repathTimer = 0.2f;
        }
    }

    void StartAttack()
    {
        state = BoarState.Attack;
        agent.isStopped = true;

        attackTimer = attackDuration;
        nextAttackTime = Time.time + attackCooldown;

        animator.SetTrigger("Attack");
    }

    void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotation,
                8f * Time.deltaTime
            );
        }
    }

    void UpdateAnimator()
    {
        float speed = 0f;

        if (!agent.isStopped && agent.velocity.magnitude > 0.1f)
        {
            if (state == BoarState.Walk)
                speed = 1f;
            else if (state == BoarState.Chase)
                speed = 2f;
        }

        animator.SetFloat("Speed", speed);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            Application.isPlaying ? patrolCenter : transform.position,
            patrolRadius
        );
    }
}
