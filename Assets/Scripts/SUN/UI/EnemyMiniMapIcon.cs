using UnityEngine;

public class EnemyMinimapIcon : MonoBehaviour
{
    [Tooltip("Prefab ของจุดแดงที่จะโชว์บน minimap")]
    public GameObject minimapDotPrefab;

    [Tooltip("ความสูงคงที่ที่ dot ลอยอยู่ ต้องเท่ากับค่าที่ตั้งไว้ฝั่ง Player")]
    public float heightOffset = 10f;

    private Transform dotInstance;

    void Start()
    {
        // สร้าง dot ของตัวเองตอน enemy ตัวนี้ถูก spawn เข้ามา
        GameObject dot = Instantiate(minimapDotPrefab);
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
        // เมื่อ enemy ตายหรือถูกลบ ให้ลบ dot ตามไปด้วย ไม่งั้น dot จะค้างอยู่บน minimap
        if (dotInstance != null)
            Destroy(dotInstance.gameObject);
    }
}