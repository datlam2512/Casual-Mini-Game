using UnityEngine;

[DisallowMultipleComponent]
public sealed class ThirdPersonCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.16f;

    private Vector3 offset;
    private Vector3 velocity;

    private void OnEnable()
    {
        velocity = Vector3.zero;
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        velocity = Vector3.zero;
        transform.position = target.position + offset;
    }

    private void LateUpdate()
    {
        if (target == null || Time.deltaTime <= 0f)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref velocity, smoothTime);
    }
}
