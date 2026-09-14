using UnityEngine;

public class MinimapManager : MonoBehaviour
{
    public static MinimapManager Instance { get; private set; }

    [Tooltip("ตั้งค่าตรงนี้ที่เดียว ทุก enemy จะดึงไปใช้เอง")]
    public GameObject enemyDotPrefab;

    void Awake()
    {
        Instance = this;
    }
}