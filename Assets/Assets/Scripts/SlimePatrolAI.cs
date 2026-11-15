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
    [SerializeField] private float chaseSpeed = 3.2f;
    [SerializeField] private float detectRadius = 6f;

    private const float GroundSampleSpacing = .42f;
    private const float MaxGroundStep = .40f;
    private const float Acceleration = 12f;
    private Rigidbody body;
    private bool towardB = true;
    private bool chasing;
    private float nextDetection;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (groundMask.value == 0) groundMask = LayerMask.GetMask("Ground");
    }

    private void FixedUpdate()
    {
        Vector3 velocity = body.linearVelocity;
        Vector3 desired = Vector3.zero;
        Vector3 position = body.position;
        bool hasGround = TryGround(position, out float ground);
        if (game != null && game.IsPlaying && hasGround)
        {
            if (Time.fixedTime >= nextDetection)
            {
                chasing = CanReachPlayer(position, ground);
                nextDetection = Time.fixedTime + .12f;
            }

            Vector3 waypoint = towardB ? pointB.position : pointA.position;
            if (!chasing && Flat(waypoint - position).sqrMagnitude < .18f)
            {
                towardB = !towardB;
                waypoint = towardB ? pointB.position : pointA.position;
            }

            Vector3 toward = Flat((chasing ? player.position : waypoint) - position);
            if (toward.sqrMagnitude > .012f)
            {
                Vector3 direction = toward.normalized;
                float speed = chasing ? chaseSpeed : patrolSpeed;
                if (SafeStep(position, ground, direction, speed))
                {
                    desired = direction * speed;
                    body.MoveRotation(Quaternion.RotateTowards(body.rotation,
                        Quaternion.LookRotation(direction), 270f * Time.fixedDeltaTime));
                }
            }
        }

        Vector3 motion = Vector3.MoveTowards(Flat(velocity), desired, Acceleration * Time.fixedDeltaTime);
        if (hasGround && motion.sqrMagnitude > .001f && !SafeStep(position, ground, motion.normalized, motion.magnitude))
            motion = Vector3.zero;
        body.linearVelocity = new Vector3(motion.x, velocity.y, motion.z);
    }

    private bool CanReachPlayer(Vector3 start, float height)
    {
        if (player == null) return false;
        Vector3 offset = Flat(player.position - start);
        float range = detectRadius + (chasing ? 1.4f : 0f);
        if (offset.sqrMagnitude > range * range ||
            !TryGround(player.position, out float playerGround) ||
            Mathf.Abs(player.position.y - playerGround) > 2.1f) return false;

        int samples = Mathf.Max(1, Mathf.CeilToInt(offset.magnitude / GroundSampleSpacing));
        float current = height;
        for (int step = 1; step <= samples; step++)
        {
            Vector3 sample = start + offset * (step / (float)samples);
            sample.y = current;
            if (!TryGround(sample, out float next) || Mathf.Abs(next - current) > MaxGroundStep) return false;
            current = next;
        }
        return Mathf.Abs(current - playerGround) <= MaxGroundStep;
    }

    private bool SafeStep(Vector3 position, float height, Vector3 direction, float speed)
    {
        // Probe past the leading edge of the capsule before committing to a step.
        Vector3 ahead = position + direction * (.58f + speed * Time.fixedDeltaTime);
        return TryGround(ahead, out float floor) && Mathf.Abs(floor - height) <= MaxGroundStep;
    }

    private bool TryGround(Vector3 position, out float floor)
    {
        bool found = Physics.Raycast(position + Vector3.up * .8f, Vector3.down, out var hit,
            2.65f, groundMask, QueryTriggerInteraction.Ignore);
        floor = hit.point.y;
        return found;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (game != null && other.GetComponentInParent<PlayerMovement>() != null)
            game.LoseLife("A slime caught you");
    }

    private static Vector3 Flat(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }
}
