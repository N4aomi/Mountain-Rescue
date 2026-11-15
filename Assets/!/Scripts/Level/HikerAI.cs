using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HikerAI : MonoBehaviour
{
    public enum HikerState
    {
        Waiting,
        Following,
        AtCamp,
        Evacuating
    }

    [Header("References")]
    public Animator animator;

    [Header("Follow Settings")]
    public float followDistance = 2f;
    public float positionOffset = 0f;
    public float speedMultiplier = 1.05f;

    [Header("Animation")]
    public float animationSmoothTime = 0.1f;

    public HikerState CurrentState { get; private set; }

    private NavMeshAgent agent;
    private Transform player;
    private PlayerLocomotion playerLocomotion;
    private InputManager inputManager;

    private float updateTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        agent.acceleration = 30f;
        agent.angularSpeed = 540f;
    }

    public bool Initialize(
        Transform playerTransform,
        Vector3 spawnPosition,
        int hikerIndex)
    {
        player = playerTransform;

        playerLocomotion =
            player.GetComponent<PlayerLocomotion>();

        inputManager =
            player.GetComponent<InputManager>();

        positionOffset = (hikerIndex - 1) * 1.5f;

        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        if (!NavMesh.SamplePosition(
            spawnPosition,
            out NavMeshHit hit,
            5f,
            filter))
        {
            Debug.LogError(
                name +
                ": Kein Hiker-NavMesh am Versteck gefunden!" +
                " Spawn Position: " + spawnPosition +
                " | Agent Type ID: " + agent.agentTypeID
            );

            return false;
        }

        if (!agent.Warp(hit.position))
        {
            Debug.LogError(
                name + ": Konnte nicht auf das NavMesh gesetzt werden!"
            );

            return false;
        }

        CurrentState = HikerState.Waiting;
        StopMoving();
        SetAnimation(0f);

        return true;
    }

    private void Update()
    {
        if (player == null || !agent.isOnNavMesh)
            return;

        if (CurrentState == HikerState.Following ||
            CurrentState == HikerState.Evacuating)
        {
            FollowPlayer();
        }
        else
        {
            StopMoving();
            SetAnimation(0f);
        }
    }

    private void FollowPlayer()
    {
        agent.isStopped = false;

        bool playerIsSprinting =
            playerLocomotion != null &&
            playerLocomotion.isSprinting;

        float playerMoveAmount =
            inputManager != null
            ? inputManager.moveAmount
            : 1f;

        float animationValue;

        if (playerIsSprinting && playerMoveAmount > 0.1f)
        {
            agent.speed =
                playerLocomotion.sprintingSpeed *
                speedMultiplier;

            animationValue = 2f;
        }
        else if (playerMoveAmount >= 0.5f)
        {
            agent.speed =
                playerLocomotion.runningSpeed *
                speedMultiplier;

            animationValue = 1f;
        }
        else
        {
            agent.speed =
                playerLocomotion.walkingSpeed *
                speedMultiplier;

            animationValue = 0.5f;
        }

        agent.stoppingDistance = 0.3f;
        updateTimer -= Time.deltaTime;

        if (updateTimer <= 0f)
        {
            Vector3 followPosition;

            if (CurrentState == HikerState.Evacuating)
            {
                followPosition =
                    player.position -
                    player.forward * followDistance +
                    player.right * positionOffset;
            }
            else
            {
                followPosition =
                    player.position -
                    player.forward * followDistance;
            }

            NavMeshQueryFilter filter = new NavMeshQueryFilter
            {
                agentTypeID = agent.agentTypeID,
                areaMask = agent.areaMask
            };

            if (NavMesh.SamplePosition(
                followPosition,
                out NavMeshHit hit,
                2.5f,
                filter))
            {
                agent.SetDestination(hit.position);
            }

            updateTimer = 0.2f;
        }

        if (agent.velocity.magnitude < 0.1f)
        {
            animationValue = 0f;
        }

        SetAnimation(animationValue);
    }

    private void SetAnimation(float vertical)
    {
        if (animator == null)
            return;

        animator.SetFloat(
            "Horizontal",
            0f,
            animationSmoothTime,
            Time.deltaTime
        );

        animator.SetFloat(
            "Vertical",
            vertical,
            animationSmoothTime,
            Time.deltaTime
        );
    }

    private void StopMoving()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = true;

        if (agent.hasPath)
            agent.ResetPath();
    }

    public void StartFollowing()
    {
        CurrentState = HikerState.Following;
        updateTimer = 0f;
    }

    public void ArriveAtCamp()
    {
        CurrentState = HikerState.AtCamp;
        StopMoving();
        SetAnimation(0f);
    }

    public void StartEvacuation()
    {
        CurrentState = HikerState.Evacuating;
        updateTimer = 0f;
    }
}
