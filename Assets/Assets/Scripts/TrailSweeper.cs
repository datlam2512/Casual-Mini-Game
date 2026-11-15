using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class TrailSweeper : MonoBehaviour
{
    [SerializeField] private GreenHillsGame game;
    [SerializeField] private float travel;
    [SerializeField] private float period;
    private Rigidbody body;
    private Vector3 center;
    private float phase;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        center = body.position;
    }

    private void FixedUpdate()
    {
        if (!game.IsPlaying) return;
        phase = Mathf.Repeat(phase + Time.fixedDeltaTime / period, 1f);
        body.MovePosition(center + Vector3.right * (Mathf.Cos(phase * Mathf.PI * 2f) * travel));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() != null) game.LoseLife("A sweeper hit you");
    }
}
