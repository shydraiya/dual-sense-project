using UnityEngine;
using UnityEngine.AI;

public class targetAction : MonoBehaviour
{
    // 순찰 지점과 플레이어 감지 설정.
    public Transform pointA;
    public Transform pointB;
    public float detectionDistance = 10f;
    public float viewAngle = 45f;

    // 이전 버전에 비해 추가됨:
    // 플레이어를 끝까지 들이받지 않도록 일정 거리를 두고 멈춘다.
    public float chaseStopDistance = 1.2f;

    // 이전 버전에 비해 추가됨:
    // 목적지를 너무 자주 다시 계산하지 않게 해서 떨림을 줄인다.
    public float destinationUpdateInterval = 0.1f;

    [SerializeField]
    public float speed = 10f;

    // 순찰과 추적 동작에 필요한 주요 참조들.
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Transform currentTarget;
    private Transform player;
    private Renderer rend;
    private Color originalColor;
    private bool isChasingPlayer;

    // 이전 버전에 비해 추가됨:
    // 마지막으로 목적지를 갱신한 시점을 저장해서 더 부드럽게 움직이게 한다.
    private float lastDestinationUpdateTime;

    private void Start()
    {
        // 시작할 때 필요한 컴포넌트를 한 번만 받아온다.
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();

        // pointA부터 순찰을 시작하고, Inspector에 넣은 속도를 NavMeshAgent에 반영한다.
        currentTarget = pointA;
        agent.speed = speed;

        // 이전 버전에 비해 변경됨:
        // 플레이어 위치까지 파고들지 않고 최소 정지 거리를 유지한다.
        agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, chaseStopDistance);

        if (rb != null)
        {
            // 이전 버전에 비해 변경됨:
            // NavMeshAgent와 동적인 Rigidbody가 서로 충돌하면 플레이어를 밀 수 있어서
            // 이 오브젝트는 내비게이션으로만 움직이게 만든다.
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 플레이어에 태그를 붙였으므로 Player 태그만 보고 대상을 찾는다.
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 추적이 끝났을 때 원래 색으로 되돌리기 위해 초기 색을 저장한다.
        if (rend != null)
        {
            originalColor = rend.material.color;
        }

        // 시작하자마자 순찰을 시작한다.
        SetAgentDestination(currentTarget.position, true);
    }

    private void Update()
    {
        if (player != null)
        {
            // 기본 시야 판정: 플레이어가 감지 거리 안에 있고 시야각 안에 있을 때만 추적한다.
            Vector3 directionToPlayer = player.position - transform.position;
            float distanceToPlayer = directionToPlayer.magnitude;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer.normalized);

            if (distanceToPlayer < detectionDistance && angleToPlayer < viewAngle / 2f)
            {
                // 추적 상태로 한 번만 전환하고, 색을 바꿔 상태 변화를 보여준다.
                if (!isChasingPlayer)
                {
                    isChasingPlayer = true;
                    if (rend != null)
                    {
                        rend.material.color = Color.green;
                    }
                }

                // 이전 버전에 비해 추가됨:
                // 플레이어의 정확한 위치까지 가는 대신, 앞쪽의 약간 떨어진 지점을 목표로 잡는다.
                float stopDistance = Mathf.Max(chaseStopDistance, agent.radius + 0.1f);
                Vector3 targetPosition = player.position - directionToPlayer.normalized * stopDistance;
                targetPosition.y = transform.position.y;

                if (distanceToPlayer <= stopDistance)
                {
                    // 이전 버전에 비해 추가됨:
                    // 충분히 가까워졌으면 완전히 멈춰서 계속 밀어붙이지 않게 한다.
                    if (!agent.isStopped)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                    }
                }
                else
                {
                    // 플레이어가 다시 멀어지면 추적 이동을 재개한다.
                    if (agent.isStopped)
                    {
                        agent.isStopped = false;
                    }

                    // 이전 버전에 비해 변경됨:
                    // 목적지 갱신을 보조 함수로 모아서 더 안정적으로 처리한다.
                    SetAgentDestination(targetPosition);
                }

                return;
            }

            // 플레이어가 감지 범위를 벗어나면 다시 순찰 상태로 돌아가고 색도 복구한다.
            if (isChasingPlayer)
            {
                isChasingPlayer = false;
                if (rend != null)
                {
                    rend.material.color = originalColor;
                }

                currentTarget = pointA;
                agent.isStopped = false;
                SetAgentDestination(currentTarget.position, true);
            }
        }

        // 플레이어를 추적 중이 아닐 때는 pointA와 pointB를 오가며 순찰한다.
        if (!isChasingPlayer && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f)
            {
                currentTarget = currentTarget == pointA ? pointB : pointA;
                SetAgentDestination(currentTarget.position, true);
            }
        }
    }

    private void SetAgentDestination(Vector3 destination, bool force = false)
    {
        // 에이전트가 없거나 아직 NavMesh 위에 올라가지 않은 경우를 대비한 안전 처리.
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        // 이전 버전에 비해 추가됨:
        // 너무 짧은 간격의 경로 재계산은 건너뛰어 흔들림을 줄인다.
        if (!force && Time.time - lastDestinationUpdateTime < destinationUpdateInterval)
        {
            return;
        }

        // 이전 버전에 비해 추가됨:
        // 아주 작은 목적지 차이는 무시해서 잔떨림만 생기지 않게 한다.
        if (!force && agent.hasPath)
        {
            Vector3 delta = destination - agent.destination;
            if (delta.sqrMagnitude < 0.04f)
            {
                return;
            }
        }

        // 새 목적지를 적용하고, 갱신 시점을 기록한다.
        agent.SetDestination(destination);
        lastDestinationUpdateTime = Time.time;
    }
}
