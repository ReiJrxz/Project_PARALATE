using UnityEngine;

public enum NoiseType
{
    Walking,
    Sprinting,
    Shooting,
    Whistling,
    Throwing,
}
public readonly struct NoiseData
{
    public readonly GameObject Source;
    public readonly Vector3 Position;
    public readonly float Radius;
    public readonly NoiseType NoiseType;

    public NoiseData(GameObject source, Vector3 position, float radius, NoiseType noiseType)
    {
        Source = source;
        Position = position;
        Radius = radius;
        NoiseType = noiseType;
    }
}