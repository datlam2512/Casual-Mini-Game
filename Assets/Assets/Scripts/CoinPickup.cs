using UnityEngine;

public sealed class CoinPickup : MonoBehaviour
{
    [SerializeField] private GreenHillsGame game;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() != null && game.TryCollect())
        {
            gameObject.SetActive(false);
        }
    }
}
