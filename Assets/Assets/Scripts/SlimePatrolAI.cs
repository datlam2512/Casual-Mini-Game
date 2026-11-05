using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class SlimePatrolAI : MonoBehaviour
{
    [SerializeField] private GreenHillsGame game;
    [SerializeField] private Transform player;
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float patrolSpeed = 1.15f;
    [SerializeField] private float chaseSpeed = 2.15f;
    [SerializeField] private float detectRadius = 3.7f;

    private Rigidbody body;
    private Collider platform;
    private bool towardB = true;

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        if (FindGround(pointA.position, out var hit)) platform = hit.collider;
    }

    private void FixedUpdate()
    {
        Vector3 velocity = body.linearVelocity;
        Vector3 horizontal = Vector3.zero;
        if (game.IsPlaying && platform != null)
        {
            bool chase = (player.position - body.position).sqrMagnitude <= detectRadius * detectRadius
                && FindGround(player.position, out var playerGround) && playerGround.collider == platform
                && Mathf.Abs(player.position.y - playerGround.point.y) < 1.1f;

            Vector3 waypoint = towardB ? pointB.position : pointA.position;
            Vector3 toWaypoint = Flat(waypoint - body.position);
            if (!chase && toWaypoint.sqrMagnitude < .12f)
            {
                towardB = !towardB;
                waypoint = towardB ? pointB.position : pointA.position;
            }

            Vector3 goal = chase ? player.position : waypoint;
            var bounds = platform.bounds;
            goal.x = Mathf.Clamp(goal.x, bounds.min.x + .85f, bounds.max.x - .85f);
            goal.z = Mathf.Clamp(goal.z, bounds.min.z + .85f, bounds.max.z - .85f);
            Vector3 direction = Flat(goal - body.position).normalized;
            if (direction.sqrMagnitude > 0f)
            {
                Vector3 ahead = body.position + direction * .77f;
                if (FindGround(ahead, out var ground) && ground.collider == platform)
                {
                    horizontal = direction * (chase ? chaseSpeed : patrolSpeed);
                    body.MoveRotation(Quaternion.RotateTowards(body.rotation,
                        Quaternion.LookRotation(direction), 180f * Time.fixedDeltaTime));
                }
            }
        }
        Vector3 smooth = Vector3.MoveTowards(new Vector3(velocity.x, 0, velocity.z), horizontal, 5f * Time.fixedDeltaTime);
        body.linearVelocity = new Vector3(smooth.x, velocity.y, smooth.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() != null)
        {
            game.LoseLife("A slime caught you");
        }
    }

    private bool FindGround(Vector3 position, out RaycastHit hit)
    {
        return Physics.Raycast(position + Vector3.up, Vector3.down, out hit, 4f,
            groundMask, QueryTriggerInteraction.Ignore);
    }

    private static Vector3 Flat(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }
}
