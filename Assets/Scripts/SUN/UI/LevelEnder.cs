using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelExitTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LevelManager.Instance.CompleteLevel();
        }
    }
}