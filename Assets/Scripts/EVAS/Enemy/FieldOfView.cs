using System.Collections;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    public float radius;
    [Range(0, 360)]
    public float angle;

    public GameObject playerRef;

    public LayerMask targetMask;
    public LayerMask obstructionMask;

    public bool canSeePlayer;

    [Header("Vision Cone Visual")]
    public bool showVisionCone = true;
    public Color searchingColor = new Color(0f, 1f, 0f, 0.25f);
    public Color spottedColor = new Color(1f, 0f, 0f, 0.35f);
    public float coneHeightOffset = -0.05f;
    [Range(3, 120)]
    public int coneSegments = 40;

    [Tooltip("ความถี่ในการอัปเดตรูปทรงกรวยสายตา (วินาที) ยิ่งมากยิ่งประหยัด CPU")]
    public float coneUpdateInterval = 0.03f;

    private static readonly Collider[] rangeBuffer = new Collider[16];

    private EnemyHealth enemyHealth;
    private float nextConeUpdateTime;
    private GameObject coneVisual;
    private Mesh coneMesh;
    private MeshRenderer coneRenderer;
    private Material coneMaterial;
    private Vector3[] coneVertices;
    private int[] coneTriangles;

    void Start()
    {
        playerRef = GameObject.FindGameObjectWithTag("Player");
        enemyHealth = GetComponent<EnemyHealth>();
        CreateConeVisual();
        StartCoroutine(FOVRoutine());
    }

    void LateUpdate()
    {
        // รูปทรงกรวยเป็น child ของศัตรู จึงหมุนตามเองทุกเฟรม ส่วนการยิง raycast สร้าง mesh ใหม่ทำเป็นช่วงๆ พอ
        if (Time.time < nextConeUpdateTime)
            return;

        nextConeUpdateTime = Time.time + coneUpdateInterval;
        UpdateConeVisual();
    }

    private void OnDestroy()
    {
        if (coneMesh != null)
            Destroy(coneMesh);

        if (coneMaterial != null)
            Destroy(coneMaterial);
    }

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        // สุ่มเวลาเริ่ม เพื่อไม่ให้ศัตรูทุกตัวตรวจสายตาในเฟรมเดียวกัน
        yield return new WaitForSeconds(Random.Range(0f, 0.2f));

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }
    private void FieldOfViewCheck()
    {
        canSeePlayer = false;

        if (enemyHealth != null && (enemyHealth.IsStunned || enemyHealth.IsDead))
            return;

        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, rangeBuffer, targetMask);
        for (int i = 0; i < count; i++)
        {
            if (IsTargetInVisionCone(rangeBuffer[i].transform))
            {
                canSeePlayer = true;
                return;
            }
        }
    }
    public bool IsTargetInVisionCone(Transform target)
    {
        if (target == null)
            return false;

        Vector3 directionToTarget = (target.position - transform.position).normalized;


        if (Vector3.Angle(transform.forward, directionToTarget) >= angle / 2)
            return false;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget > radius)
            return false;

        return !Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask);
    }
    private void CreateConeVisual()
    {
        coneVisual = new GameObject("VisionConeVisual");
        coneVisual.transform.SetParent(transform, false);

        MeshFilter meshFilter = coneVisual.AddComponent<MeshFilter>();
        coneRenderer = coneVisual.AddComponent<MeshRenderer>();

        coneMesh = new Mesh();
        coneMesh.name = "Vision Cone Mesh";
        meshFilter.mesh = coneMesh;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Standard");

        coneMaterial = new Material(shader);
        coneMaterial.renderQueue = 3000;
        coneRenderer.material = coneMaterial;
    }
    private void UpdateConeVisual()
    {
        if (coneVisual == null)
            return;

        coneVisual.SetActive(showVisionCone);
        if (!showVisionCone)
            return;

        int segmentCount = Mathf.Max(3, coneSegments);
        EnsureMeshArrays(segmentCount);

        coneVertices[0] = new Vector3(0f, coneHeightOffset, 0f);

        float halfAngle = angle * 0.5f;
        Vector3 rayOrigin = transform.position + Vector3.up * coneHeightOffset;

        for (int i = 0; i <= segmentCount; i++)
        {
            float lerp = i / (float)segmentCount;
            float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, lerp);
            Vector3 localDirection = DirectionFromAngle(currentAngle);
            Vector3 worldDirection = transform.TransformDirection(localDirection);
            float vertexDistance = radius;

            if (Physics.Raycast(rayOrigin, worldDirection, out RaycastHit hit, radius, obstructionMask))
                vertexDistance = hit.distance;

            coneVertices[i + 1] = localDirection * vertexDistance + Vector3.up * coneHeightOffset;
        }

        // triangle/normal ถูกตั้งไว้แล้วใน EnsureMeshArrays จึงอัปเดตเฉพาะตำแหน่งจุด
        coneMesh.vertices = coneVertices;
        coneMesh.RecalculateBounds();

        SetConeColor(canSeePlayer ? spottedColor : searchingColor);
    }
    private void EnsureMeshArrays(int segmentCount)
    {
        int vertexCount = segmentCount + 2;
        int triangleCount = segmentCount * 3;

        if (coneVertices != null && coneVertices.Length == vertexCount &&
            coneTriangles != null && coneTriangles.Length == triangleCount)
            return;

        coneVertices = new Vector3[vertexCount];
        coneTriangles = new int[triangleCount];
        Vector3[] normals = new Vector3[vertexCount];

        for (int i = 0; i < vertexCount; i++)
            normals[i] = Vector3.up;

        for (int i = 0; i < segmentCount; i++)
        {
            int triangleIndex = i * 3;
            coneTriangles[triangleIndex] = 0;
            coneTriangles[triangleIndex + 1] = i + 1;
            coneTriangles[triangleIndex + 2] = i + 2;
        }

        coneMesh.Clear();
        coneMesh.vertices = coneVertices;
        coneMesh.triangles = coneTriangles;
        coneMesh.normals = normals;
    }
    private void SetConeColor(Color color)
    {
        if (coneMaterial == null)
            return;

        if (coneMaterial.HasProperty("_Color"))
            coneMaterial.SetColor("_Color", color);

        if (coneMaterial.HasProperty("_BaseColor"))
            coneMaterial.SetColor("_BaseColor", color);
    }
    private Vector3 DirectionFromAngle(float angleInDegrees)
    {
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0f, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
