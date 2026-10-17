using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class WallHugController : MonoBehaviour
{
    [Header("Wall detection")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField, Min(0.05f)] private float enterDistance = 0.65f;
    [SerializeField, Min(0.05f)] private float exitDistance = 0.9f;
    [SerializeField, Min(0f)] private float checkHeight = 1f;

    [Header("Wall spacing")]
    [SerializeField, Min(0f)] private float desiredWallDistance = 0.45f;
    [SerializeField, Min(0f)] private float correctionSpeed = 3f;

    [Header("Input intent")]
    [Tooltip("Minimum input component pushing into the wall to enter/keep hugging")]
    [SerializeField, Range(0.05f, 0.9f)] private float intoWallThreshold = 0.2f;
    [Tooltip("Input component away from the wall that immediately releases hug")]
    [SerializeField, Range(0.05f, 0.9f)] private float awayFromWallThreshold = 0.25f;

    private bool isHugging;
    private Vector3 wallNormal;

    public bool IsHugging => isHugging;
    public Vector3 WallFacingDirection => wallNormal;

    // Call once per frame from TopDownPlayerController with the raw world move direction.
    public void UpdateWallHug(Vector3 desiredDirection)
    {
        desiredDirection.y = 0f;

        if (isHugging)
        {
            if (!HasWallInFrontOfBack(out RaycastHit currentHit))
            {
                ExitWallHug("wall lost");
                return;
            }

            SetWallNormal(currentHit.normal);

            if (desiredDirection.sqrMagnitude < 0.0001f)
            {
                ExitWallHug("input released");
                return;
            }

            Vector3 input = desiredDirection.normalized;
            float intoWall = Vector3.Dot(input, -wallNormal);
            float awayFromWall = Vector3.Dot(input, wallNormal);

            // Let the normal TopDown movement run freely when the player pushes away.
            if (awayFromWall >= awayFromWallThreshold)
            {
                ExitWallHug("moving away");
                return;
            }

            // Keep the hug only while input continues to press into the surface.
            if (intoWall < intoWallThreshold)
                ExitWallHug("no longer pressing into wall");

            return;
        }

        if (desiredDirection.sqrMagnitude < 0.0001f)
            return;

        TryEnterFromMoveDirection(desiredDirection.normalized);
    }

    private void TryEnterFromMoveDirection(Vector3 input)
    {
        // Probe toward the direction the player is actually pressing, so
        // approaching from the front, side, or a diagonal can all enter hug.
        Vector3 origin = GetCheckOrigin();
        const float castRadius = 0.2f;

        if (!Physics.SphereCast(origin, castRadius, input, out RaycastHit hit,
                enterDistance, wallLayer, QueryTriggerInteraction.Ignore))
            return;

        Vector3 candidateNormal = hit.normal;
        candidateNormal.y = 0f;
        if (candidateNormal.sqrMagnitude < 0.001f)
            return;
        candidateNormal.Normalize();

        // Only attach when input is actually pressing into the surface.
        float intoWall = Vector3.Dot(input, -candidateNormal);
        if (intoWall < intoWallThreshold)
            return;

        SetWallNormal(candidateNormal);
        isHugging = true;
        Debug.Log("Wall Hug entered", this);
    }
    private bool HasWallInFrontOfBack(out RaycastHit hit)
    {
        // wallNormal points from the wall toward the player, so -normal points into the wall.
        return Physics.Raycast(GetCheckOrigin(), -wallNormal, out hit,
            exitDistance, wallLayer, QueryTriggerInteraction.Ignore);
    }

    private Vector3 GetCheckOrigin()
    {
        return transform.position + Vector3.up * checkHeight;
    }

    private void SetWallNormal(Vector3 normal)
    {
        normal.y = 0f;
        if (normal.sqrMagnitude > 0.001f)
            wallNormal = normal.normalized;
    }

    private void ExitWallHug(string reason)
    {
        if (!isHugging)
            return;

        isHugging = false;
        Debug.Log($"Wall Hug exited: {reason}", this);
    }

    public Vector3 ConstrainMoveDirection(Vector3 direction)
    {
        if (!isHugging || direction.sqrMagnitude < 0.0001f)
            return direction;

        // Remove only the into-wall component; keep sideways movement along the wall.
        Vector3 alongWall = Vector3.ProjectOnPlane(direction, wallNormal);
        return alongWall.sqrMagnitude > 0.0001f
            ? alongWall.normalized
            : Vector3.zero;
    }

    public float GetWallMoveAmount(Vector3 direction)
    {
        if (!isHugging || direction.sqrMagnitude < 0.0001f)
            return 0f;

        Vector3 wallRight = Vector3.Cross(Vector3.up, wallNormal).normalized;
        return Mathf.Clamp(Vector3.Dot(direction.normalized, wallRight), -1f, 1f);
    }

    public Vector3 GetDistanceCorrectionVelocity()
    {
        if (!isHugging || !HasWallInFrontOfBack(out RaycastHit hit))
            return Vector3.zero;

        float error = hit.distance - desiredWallDistance;
        if (Mathf.Abs(error) < 0.03f)
            return Vector3.zero;

        // Too far: move toward the wall. Too close: move away from it.
        return -wallNormal * Mathf.Clamp(
            error * correctionSpeed,
            -correctionSpeed,
            correctionSpeed);
    }

    private void OnDisable()
    {
        isHugging = false;
    }
}


