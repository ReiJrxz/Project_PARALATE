using UnityEngine;

public class PlayerNoiseEmitter : MonoBehaviour
{
    [Header("Noise Settings")]
    [SerializeField] private float walkingNoiseRadius = 5f;
    [SerializeField] private float sprintingNoiseRadius = 15f;
    [SerializeField] private float shootingNoiseRadius = 35f;
    [SerializeField] private float whistlingNoiseRadius = 20f;
    [SerializeField] private float throwingNoiseRadius = 10f;

    [Header("Debug")]
    [SerializeField] private bool showNoiseRanges = false;
    private float lastEmittedNoiseRadius;
    private Vector3 lastEmittedPostion;
    private float gizmosDisplayTime;

    private void Update()
    {
        if(gizmosDisplayTime > 0f)
        {
            gizmosDisplayTime -= Time.deltaTime;
        }
    }

    #region Action Methods

    //เสียงเดิน/วิ่ง
    public void EmitFootstep(bool isSprinting)
    {
        if(isSprinting)
        {
            EmitNoise(transform.position, sprintingNoiseRadius, NoiseType.Sprinting);
        }
        else
        {
            EmitNoise(transform.position, walkingNoiseRadius, NoiseType.Walking);
        }
    }
    //เสียงยิงปืน
    public void EmitShooting(Vector3? gunMuzzlePos = null)
    {
        Vector3 origin = gunMuzzlePos ?? transform.position;
        EmitNoise(origin, shootingNoiseRadius, NoiseType.Shooting);
    }

    //เสียงผิวปาก
    public void EmitWhistling()
    {
        EmitNoise(transform.position, whistlingNoiseRadius, NoiseType.Whistling);
    }
    //เสียงขว้างของ
    public void EmitThrowing()
    {
        EmitNoise(transform.position, throwingNoiseRadius, NoiseType.Throwing);
    }
    //กระจายเสียงแบบกำหนดเอง
    public void EmitCustomNoise(Vector3 position, float radius, NoiseType type)
    {
        EmitNoise(position, radius, type);
    }
    #endregion

    #region Degug Gizmos
    private void EmitNoise(Vector3 origin, float radius, NoiseType type)
    {
        NoiseEmitterSystem.Emit(gameObject, origin, radius, type);

        if(showNoiseRanges)
        {
            lastEmittedNoiseRadius = radius;
            lastEmittedPostion = origin;
            gizmosDisplayTime = 0.5f;
        }
    }
    private void OnDrawGizmos()
    {
        if(!showNoiseRanges || gizmosDisplayTime <= 0f)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lastEmittedPostion, lastEmittedNoiseRadius);
    }
    #endregion
}
