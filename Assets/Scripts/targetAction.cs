using UnityEngine;
using UnityEngine.AI;

public class targetAction : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;

    private NavMeshAgent agent;
    private Transform currentTarget;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentTarget = pointA;
        agent.SetDestination(currentTarget.position);
        agent.speed = 10;
    }

    private void Update()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f)
            {
                currentTarget = (currentTarget == pointA) ? pointB : pointA;
                agent.SetDestination(currentTarget.position);
            }
        }
    }
}
