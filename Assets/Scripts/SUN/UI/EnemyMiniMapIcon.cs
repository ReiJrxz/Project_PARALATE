using UnityEngine;

public class EnemyMinimapIcon : MonoBehaviour
{
    public float heightOffset = 10f;
    private Transform dotInstance;

    void Start()
    {
        if (MinimapManager.Instance == null || MinimapManager.Instance.enemyDotPrefab == null)
        {
            Debug.LogWarning("MinimapManager หรือ enemyDotPrefab ยังไม่ถูกตั้งค่า");
            return;
        }

        GameObject dot = Instantiate(MinimapManager.Instance.enemyDotPrefab);
        dot.name = gameObject.name + "_MinimapDot";
        dotInstance = dot.transform;
    }

    void LateUpdate()
    {
        if (dotInstance == null) return;
        Vector3 pos = transform.position;
        pos.y = heightOffset;
        dotInstance.position = pos;
    }

    void OnDestroy()
    {
        if (dotInstance != null)
            Destroy(dotInstance.gameObject);
    }
}