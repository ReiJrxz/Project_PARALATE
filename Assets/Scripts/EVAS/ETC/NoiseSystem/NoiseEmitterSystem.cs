using UnityEngine;
using System.Collections.Generic;

public class NoiseEmitterSystem : MonoBehaviour
{
    public static NoiseEmitterSystem Instance { get; private set; }

    [Header("Detection Settings")]
    [SerializeField] private LayerMask listenerLayers;
    [SerializeField] private LayerMask obstacleLayers;

    [Range(0f, 1f)]
    [SerializeField] private float wallDampening = 0.5f;

    private const int MAX_LISTENERS = 32;
    private static readonly Collider[] hitColliders = new Collider[MAX_LISTENERS];
    private static readonly HashSet<INoiseListener> listeners = new HashSet<INoiseListener>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public static void Emit(GameObject source, Vector3 position, float radius, NoiseType type)
    {
        if (radius <= 0f)
            return;

        if (Instance == null)
        {
            GameObject systemObject = new GameObject(nameof(NoiseEmitterSystem));
            Instance = systemObject.AddComponent<NoiseEmitterSystem>();
        }

        Instance.EmitNoise(new NoiseData(source, position, radius, type));
    }

    public static void Register(INoiseListener listener)
    {
        if (listener != null)
            listeners.Add(listener);
    }

    public static void Unregister(INoiseListener listener)
    {
        if (listener != null)
            listeners.Remove(listener);
    }

    private void EmitNoise(NoiseData noise)
    {
        foreach (INoiseListener listener in listeners)
            NotifyIfAudible(listener, noise);

        int count = Physics.OverlapSphereNonAlloc(noise.Position, noise.Radius, hitColliders, listenerLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider col = hitColliders[i];

            if (noise.Source != null && (col.gameObject == noise.Source || col.transform.IsChildOf(noise.Source.transform)))
                continue;

            MonoBehaviour[] parentComponents = col.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour component in parentComponents)
            {
                if (component is INoiseListener listener && !listeners.Contains(listener))
                    NotifyIfAudible(listener, noise);
            }
        }
    }

    private void NotifyIfAudible(INoiseListener listener, in NoiseData noise)
    {
        if (!(listener is Behaviour listenerBehaviour) || !listenerBehaviour.isActiveAndEnabled)
            return;

        Component listenerComponent = listenerBehaviour;

        if (noise.Source != null && (listenerComponent.gameObject == noise.Source || listenerComponent.transform.IsChildOf(noise.Source.transform)))
            return;

        Vector3 targetPosition = listenerComponent.transform.position + Vector3.up;
        float effectiveRadius = noise.Radius;

        if (obstacleLayers.value != 0 && Physics.Linecast(noise.Position, targetPosition, obstacleLayers, QueryTriggerInteraction.Ignore))
            effectiveRadius *= 1f - wallDampening;

        if ((targetPosition - noise.Position).sqrMagnitude <= effectiveRadius * effectiveRadius)
            listener.OnNoiseHeard(noise);
    }
}
