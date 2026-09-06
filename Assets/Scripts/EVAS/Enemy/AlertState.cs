using UnityEngine;
using UnityEngine.Serialization;


public class AlertState : MonoBehaviour
{
    public enum AlertDistanceZone //ระยะการ alert
    {
        None,
        Close,
        Middle,
        Far,
        OutsideVision
    }

    public enum AlertAIState //สถานะของ AI
    {
        Neutral,
        Suspicious,
        Alert,
        Chasing,
        Return
    }

    [Header("References")]
    [SerializeField] private FieldOfView fieldOfView;
    [SerializeField] private EnemyController enemyController;

    [Header("State Points")]
    [FormerlySerializedAs("suspiciousThreshold")]
    [SerializeField] private float neutralToSuspiciousPoint = 4f; //จำนวนแต้ม จากปกติ -> สงสัย
    [FormerlySerializedAs("alertThreshold")]
    [SerializeField] private float suspiciousToAlertPoint = 2f; //จำนวนแต้ม จากสงสัย -> alert

    [Header("Distance Multipliers")]
    [SerializeField] private float closeDistance = 4f;
    [SerializeField] private float middleDistance = 8f;
    [SerializeField] private float farAlertPerSecond = 1f;
    [SerializeField] private float middleAlertPerSecond = 2f;
    [SerializeField] private float closeAlertPerSecond = 4f;

    [Header("Return")]
    [SerializeField] private float returnPointDecrease = 1f; //แต้มลดลง ต่อ ระยะเวลากลับ
    [SerializeField] private float returnDecreaseInterval = 2f; //ระยะเวลาในการลดแต้มกลับ

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    
    [SerializeField] private bool logStateChanges;
    [SerializeField] private Color closeDistanceColor = new Color(1f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color middleDistanceColor = new Color(1f, 0.85f, 0.1f, 0.8f);
    [SerializeField] private Color farDistanceColor = new Color(0.2f, 0.6f, 1f, 0.8f);

    [Header("Runtime Debug")]
    [SerializeField] private bool isCountingAlertPoint;
    [SerializeField] private float currentAlertGainPerSecond;
    [SerializeField] private AlertDistanceZone currentDistanceZone;
    private float currentDistanceToPlayer;

    private float neutralPoint;
    private float suspiciousPoint;
    private float returnDecreaseTimer;
    private Vector3 lastKnownPlayerPosition;
    private AlertAIState currentState;

    //public float NeutralPoint => neutralPoint;
    //public float SuspiciousPoint => suspiciousPoint;
    //public float CurrentTotalPoint => neutralPoint + suspiciousPoint;
    //public float NeutralToSuspiciousPoint => neutralToSuspiciousPoint;
    //public float SuspiciousToAlertPoint => suspiciousToAlertPoint;
    //public Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;
    public AlertAIState CurrentState => currentState;
    //public AlertDistanceZone CurrentDistanceZone => currentDistanceZone;
    //public float CurrentDistanceToPlayer => currentDistanceToPlayer;
    //public float CurrentAlertGainPerSecond => currentAlertGainPerSecond;
    //public bool IsCountingAlertPoint => isCountingAlertPoint;
    public bool IsAlert => currentState == AlertAIState.Alert || currentState == AlertAIState.Chasing;

    private void Awake()
    {
        CacheReferences();
    }

    private void Update()
    {
        UpdateRuntimeDebugValues();

        if (fieldOfView == null || fieldOfView.playerRef == null)
            return;

        if (fieldOfView.canSeePlayer)
        {
            lastKnownPlayerPosition = fieldOfView.playerRef.transform.position;
            AddSeenPlayerPoint(GetAlertGainPerSecond() * Time.deltaTime);
            return;
        }

        UpdateReturn();
    }

    private void OnValidate()
    {
        neutralToSuspiciousPoint = Mathf.Max(0f, neutralToSuspiciousPoint);
        suspiciousToAlertPoint = Mathf.Max(0f, suspiciousToAlertPoint);
        closeDistance = Mathf.Max(0f, closeDistance);
        middleDistance = Mathf.Max(closeDistance, middleDistance);
        farAlertPerSecond = Mathf.Max(0f, farAlertPerSecond);
        middleAlertPerSecond = Mathf.Max(0f, middleAlertPerSecond);
        closeAlertPerSecond = Mathf.Max(0f, closeAlertPerSecond);
        returnPointDecrease = Mathf.Max(0f, returnPointDecrease);
        returnDecreaseInterval = Mathf.Max(0.01f, returnDecreaseInterval);
        CacheReferences();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
            return;

        CacheReferences();
        DrawDistanceGizmo(closeDistance, closeDistanceColor);
        DrawDistanceGizmo(middleDistance, middleDistanceColor);

        float farDistance = fieldOfView != null ? fieldOfView.radius : middleDistance;
        DrawDistanceGizmo(farDistance, farDistanceColor);
    }

    public void ResetAlert()
    {
        neutralPoint = 0f;
        suspiciousPoint = 0f;
        returnDecreaseTimer = 0f;
        SetState(AlertAIState.Neutral);
    }

    public void AddSuspiciousPoint(Vector3 sourcePosition, float amount)
    {
        if (amount <= 0f)
            return;

        lastKnownPlayerPosition = sourcePosition;

        AddStatePoint(amount);
    }

    public void EnterReturnState()
    {
        if (currentState == AlertAIState.Neutral)
            return;

        SetState(AlertAIState.Return);
        returnDecreaseTimer = 0f;
    }

    private void AddSeenPlayerPoint(float amount)
    {
        AddStatePoint(amount);
    }

    private void AddStatePoint(float amount)
    {
        if (amount <= 0f || currentState == AlertAIState.Chasing)
            return;

        if (currentState == AlertAIState.Alert)
        {
            EnterChasing();
            return;
        }

        if (currentState == AlertAIState.Return)
            SetState(neutralPoint >= neutralToSuspiciousPoint || suspiciousPoint > 0f ? AlertAIState.Suspicious : AlertAIState.Neutral);

        if (neutralPoint < neutralToSuspiciousPoint)
        {
            float remainingNeutralPoint = neutralToSuspiciousPoint - neutralPoint;
            float usedPoint = Mathf.Min(amount, remainingNeutralPoint);
            neutralPoint += usedPoint;
            amount -= usedPoint;

            if (neutralPoint >= neutralToSuspiciousPoint)
                EnterSuspicious();
        }

        if (amount <= 0f || currentState != AlertAIState.Suspicious)
            return;

        suspiciousPoint = Mathf.Min(suspiciousPoint + amount, suspiciousToAlertPoint);

        if (suspiciousPoint >= suspiciousToAlertPoint)
            EnterAlert();
        else if (enemyController != null)
            enemyController.MoveToInvestigationPoint(lastKnownPlayerPosition);
    }

    private void UpdateReturn()
    {
        if (currentState != AlertAIState.Return)
            return;

        returnDecreaseTimer += Time.deltaTime;

        if (returnDecreaseTimer < returnDecreaseInterval)
            return;

        returnDecreaseTimer = 0f;
        DecreaseStatePoint(returnPointDecrease);
    }

    private void DecreaseStatePoint(float amount)
    {
        if (amount <= 0f)
            return;

        if (suspiciousPoint > 0f)
        {
            suspiciousPoint = Mathf.Max(0f, suspiciousPoint - amount);

            if (suspiciousPoint > 0f)
                return;
        }

        if (neutralPoint > 0f)
        {
            neutralPoint = Mathf.Max(0f, neutralPoint - amount);

            if (neutralPoint > 0f)
                return;
        }

        ResetAlert();
    }

    private void EnterSuspicious()
    {
        SetState(AlertAIState.Suspicious);

        if (enemyController != null)
            enemyController.MoveToInvestigationPoint(lastKnownPlayerPosition);
    }

    private void EnterAlert()
    {
        SetState(AlertAIState.Alert);

        if (fieldOfView != null && fieldOfView.canSeePlayer)
            EnterChasing();
        else if (enemyController != null)
            enemyController.MoveToInvestigationPoint(lastKnownPlayerPosition);
    }

    private void EnterChasing()
    {
        SetState(AlertAIState.Chasing);

        if (enemyController != null)
            enemyController.StartChase();
    }

    private float GetAlertGainPerSecond()
    {
        if (currentDistanceZone == AlertDistanceZone.Close)
            return closeAlertPerSecond;

        if (currentDistanceZone == AlertDistanceZone.Middle)
            return middleAlertPerSecond;

        if (currentDistanceZone == AlertDistanceZone.Far)
            return farAlertPerSecond;

        return 0f;
    }

    private void UpdateRuntimeDebugValues()
    {
        currentDistanceToPlayer = -1f;
        currentDistanceZone = AlertDistanceZone.None;
        currentAlertGainPerSecond = 0f;
        isCountingAlertPoint = false;

        if (fieldOfView == null || fieldOfView.playerRef == null)
            return;

        currentDistanceToPlayer = Vector3.Distance(transform.position, fieldOfView.playerRef.transform.position);
        currentDistanceZone = GetDistanceZone(currentDistanceToPlayer);
        currentAlertGainPerSecond = GetGainPerSecond(currentDistanceZone);
        isCountingAlertPoint = fieldOfView.canSeePlayer && currentAlertGainPerSecond > 0f && currentState != AlertAIState.Chasing;
    }

    private AlertDistanceZone GetDistanceZone(float distanceToPlayer)
    {
        float farDistance = fieldOfView != null ? fieldOfView.radius : middleDistance;

        if (distanceToPlayer > farDistance)
            return AlertDistanceZone.OutsideVision;

        if (distanceToPlayer <= closeDistance)
            return AlertDistanceZone.Close;

        if (distanceToPlayer <= middleDistance)
            return AlertDistanceZone.Middle;

        return AlertDistanceZone.Far;
    }

    private float GetGainPerSecond(AlertDistanceZone distanceZone)
    {
        switch (distanceZone)
        {
            case AlertDistanceZone.Close:
                return closeAlertPerSecond;

            case AlertDistanceZone.Middle:
                return middleAlertPerSecond;

            case AlertDistanceZone.Far:
                return farAlertPerSecond;

            default:
                return 0f;
        }
    }

    private void CacheReferences()
    {
        if (fieldOfView == null)
            fieldOfView = GetComponent<FieldOfView>();

        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();
    }

    private void DrawDistanceGizmo(float radius, Color color)
    {
        if (radius <= 0f)
            return;

        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void SetState(AlertAIState state)
    {
        if (currentState == state)
            return;

        AlertAIState previousState = currentState;
        currentState = state;

        if (logStateChanges)
            Debug.Log($"{name} Alert State: {previousState} -> {currentState}", this);
    }
}
