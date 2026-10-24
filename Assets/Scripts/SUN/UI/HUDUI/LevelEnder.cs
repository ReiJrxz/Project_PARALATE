using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelExitTrigger : MonoBehaviour
{
    private bool triggered;

    void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player"))
            return;

        triggered = true;

        if (LevelManager.Instance != null)
            LevelManager.Instance.CompleteLevel();
    }
}
