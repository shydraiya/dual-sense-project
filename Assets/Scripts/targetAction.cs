using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class targetAction : MonoBehaviour
{
    public enum PatrolActionType
    {
        None,
        Wait,
        LookAtTarget
    }

    public enum PatrolRouteMode
    {
        Loop,
        PingPong
    }

    [System.Serializable]
    public class PatrolPoint
    {
        public Transform point;
        public PatrolActionType actionType = PatrolActionType.None;
        public float actionDuration = 1f;
        public Transform lookTarget;
    }

    [Header("Patrol")]
    public List<PatrolPoint> patrolPoints = new List<PatrolPoint>();
    public PatrolRouteMode routeMode = PatrolRouteMode.PingPong;

    [Header("Detection")]
    public float detectionDistance = 10f;
    public float viewAngle = 45f;
    public float eyeHeight = 1.6f;
    public float playerEyeHeight = 1.6f;
    public LayerMask obstructionMask = ~0;
    public float chaseContactBuffer = 0.05f;
    public float destinationUpdateInterval = 0.1f;

    [Header("Speed")]
    public float speed = 10f;
    public float speed_detect = 30f;

    [Header("Movement Tuning")]
    public float patrolAcceleration = 8f;
    public float chaseAcceleration = 40f;
    public float patrolAngularSpeed = 120f;
    public float chaseAngularSpeed = 720f;

    [Header("Alert")]
    public float alertDelay = 1.2f;
    public Color alertColor = new Color(1f, 0.5f, 0f);

    private NavMeshAgent agent;
    private Rigidbody rb;
    private Transform player;
    private Collider playerCollider;
    private Collider enemyCollider;
    private Renderer rend;
    private Color originalColor;

    private bool isChasingPlayer;
    private bool isAlertingPlayer;
    private float alertStartTime;
    private float lastDestinationUpdateTime;

    private int currentPatrolIndex;
    private int patrolDirection = 1;
    private bool isPerformingPatrolAction;
    private float patrolActionEndTime;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();
        rend = GetComponent<Renderer>();

        ApplyPatrolMovementSettings();
        agent.stoppingDistance = 0f;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
        {
            playerCollider = player.GetComponent<Collider>();
        }

        if (rend != null)
        {
            originalColor = rend.material.color;
        }

        BeginPatrolAtCurrentPoint(true);
    }

    private void Update()
    {
        if (player != null)
        {
            Vector3 directionToPlayer = player.position - transform.position;
            float distanceToPlayer = directionToPlayer.magnitude;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer.normalized);

            if (distanceToPlayer < detectionDistance &&
                angleToPlayer < viewAngle * 0.5f &&
                HasLineOfSightToPlayer())
            {
                HandlePlayerDetection(directionToPlayer, distanceToPlayer);
                return;
            }

            if (isChasingPlayer || isAlertingPlayer)
            {
                ExitPlayerStateAndResumePatrol();
            }
        }

        HandlePatrol();
    }

    private void HandlePlayerDetection(Vector3 directionToPlayer, float distanceToPlayer)
    {
        if (!isChasingPlayer && !isAlertingPlayer)
        {
            isAlertingPlayer = true;
            alertStartTime = Time.time;

            if (rend != null)
            {
                rend.material.color = alertColor;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        if (isAlertingPlayer && !isChasingPlayer)
        {
            agent.angularSpeed = chaseAngularSpeed;
            RotateTowardsDirection(directionToPlayer);

            if (Time.time - alertStartTime < alertDelay)
            {
                return;
            }

            isAlertingPlayer = false;
            isChasingPlayer = true;

            if (rend != null)
            {
                rend.material.color = Color.red;
            }

            ApplyChaseMovementSettings();
            agent.isStopped = false;
            isPerformingPatrolAction = false;
        }

        float surfaceDistance = GetSurfaceDistanceToPlayer();
        Vector3 targetPosition = player.position;
        targetPosition.y = transform.position.y;

        if (surfaceDistance <= chaseContactBuffer)
        {
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }
        else
        {
            if (agent.isStopped)
            {
                agent.isStopped = false;
            }

            SetAgentDestination(targetPosition);
        }
    }

    private void ExitPlayerStateAndResumePatrol()
    {
        isChasingPlayer = false;
        isAlertingPlayer = false;

        if (rend != null)
        {
            rend.material.color = originalColor;
        }

        ApplyPatrolMovementSettings();
        isPerformingPatrolAction = false;
        BeginPatrolAtCurrentPoint(true);
    }

    private void HandlePatrol()
    {
        if (patrolPoints == null || patrolPoints.Count == 0)
        {
            return;
        }

        if (isPerformingPatrolAction)
        {
            UpdatePatrolAction();
            return;
        }

        if (agent.pathPending)
        {
            return;
        }

        if (agent.remainingDistance > agent.stoppingDistance)
        {
            return;
        }

        if (agent.hasPath && agent.velocity.sqrMagnitude >= 0.01f)
        {
            return;
        }

        if (TryStartPatrolAction())
        {
            return;
        }

        AdvanceToNextPatrolPoint();
        BeginPatrolAtCurrentPoint(true);
    }

    private bool TryStartPatrolAction()
    {
        PatrolPoint patrolPoint = GetCurrentPatrolPoint();
        if (patrolPoint == null)
        {
            return false;
        }

        if (patrolPoint.actionType == PatrolActionType.None)
        {
            return false;
        }

        isPerformingPatrolAction = true;
        patrolActionEndTime = Time.time + Mathf.Max(0f, patrolPoint.actionDuration);
        agent.isStopped = true;
        agent.ResetPath();
        return true;
    }

    private void UpdatePatrolAction()
    {
        PatrolPoint patrolPoint = GetCurrentPatrolPoint();
        if (patrolPoint == null)
        {
            isPerformingPatrolAction = false;
            return;
        }

        if (patrolPoint.actionType == PatrolActionType.LookAtTarget && patrolPoint.lookTarget != null)
        {
            RotateTowardsDirection(patrolPoint.lookTarget.position - transform.position);
        }

        if (Time.time < patrolActionEndTime)
        {
            return;
        }

        isPerformingPatrolAction = false;
        agent.isStopped = false;

        AdvanceToNextPatrolPoint();
        BeginPatrolAtCurrentPoint(true);
    }

    private void BeginPatrolAtCurrentPoint(bool force)
    {
        PatrolPoint patrolPoint = GetCurrentPatrolPoint();
        if (patrolPoint == null || patrolPoint.point == null)
        {
            return;
        }

        isPerformingPatrolAction = false;
        agent.isStopped = false;
        SetAgentDestination(patrolPoint.point.position, force);
    }

    private PatrolPoint GetCurrentPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Count == 0)
        {
            return null;
        }

        currentPatrolIndex = Mathf.Clamp(currentPatrolIndex, 0, patrolPoints.Count - 1);
        return patrolPoints[currentPatrolIndex];
    }

    private void AdvanceToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Count <= 1)
        {
            return;
        }

        if (routeMode == PatrolRouteMode.Loop)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
            return;
        }

        int nextIndex = currentPatrolIndex + patrolDirection;
        if (nextIndex < 0 || nextIndex >= patrolPoints.Count)
        {
            patrolDirection *= -1;
            nextIndex = currentPatrolIndex + patrolDirection;
        }

        currentPatrolIndex = Mathf.Clamp(nextIndex, 0, patrolPoints.Count - 1);
    }

    private bool HasLineOfSightToPlayer()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * playerEyeHeight;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
        {
            return true;
        }

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        return false;
    }

    private void RotateTowardsDirection(Vector3 direction)
    {
        Vector3 flatDirection = direction;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            agent.angularSpeed * Time.deltaTime);
    }

    private void ApplyPatrolMovementSettings()
    {
        agent.speed = speed;
        agent.acceleration = patrolAcceleration;
        agent.angularSpeed = patrolAngularSpeed;
    }

    private void ApplyChaseMovementSettings()
    {
        agent.speed = speed_detect;
        agent.acceleration = chaseAcceleration;
        agent.angularSpeed = chaseAngularSpeed;
    }

    private void SetAgentDestination(Vector3 destination, bool force = false)
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        if (!force && Time.time - lastDestinationUpdateTime < destinationUpdateInterval)
        {
            return;
        }

        if (!force && agent.hasPath)
        {
            Vector3 delta = destination - agent.destination;
            if (delta.sqrMagnitude < 0.04f)
            {
                return;
            }
        }

        agent.SetDestination(destination);
        lastDestinationUpdateTime = Time.time;
    }

    private float GetSurfaceDistanceToPlayer()
    {
        float fallbackDistance = agent != null ? agent.radius : 0f;
        if (player == null)
        {
            return fallbackDistance;
        }

        if (enemyCollider == null || playerCollider == null)
        {
            Vector3 flatOffset = player.position - transform.position;
            flatOffset.y = 0f;
            return Mathf.Max(0f, flatOffset.magnitude - agent.radius);
        }

        Vector3 enemyPoint = enemyCollider.ClosestPoint(player.position);
        Vector3 playerPoint = playerCollider.ClosestPoint(transform.position);

        Vector3 flatOffsetBetweenPoints = playerPoint - enemyPoint;
        flatOffsetBetweenPoints.y = 0f;

        return flatOffsetBetweenPoints.magnitude;
    }
}
