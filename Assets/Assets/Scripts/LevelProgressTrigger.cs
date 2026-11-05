using UnityEngine;
public sealed class LevelProgressTrigger : MonoBehaviour
{
    public GreenHillsGame game;
    public Transform checkpoint;
    public bool finish;
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        if (finish) game.CompleteLevel();
        else game.ActivateCheckpoint(checkpoint);
    }
}