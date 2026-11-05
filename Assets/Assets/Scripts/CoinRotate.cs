using UnityEngine;

public class CoinRotate : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        // Xoay coin
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);

        // Nhấp nhô lên xuống
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}