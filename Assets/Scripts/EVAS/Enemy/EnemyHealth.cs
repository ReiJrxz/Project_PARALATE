using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public enum DeathAction { Destroy, FallOver, Revive }

    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI Settings")]
    public Image healthBarFill; // ใส่ Image ของ Health
    public Transform healthCanvas; // ใส่ Canvas ให้หันเข้ากล้อง

    [Header("Death Settings")]
    public DeathAction onDeath;

    private bool isDead = false;
    private bool isStunned = false;
    private Quaternion standingRotation;
    private Coroutine knockOutRoutine;
    private EnemyController enemyController;
    private Camera mainCamera;

    public bool IsDead => isDead;
    public bool IsStunned => isStunned;

    private void Awake()
    {
        enemyController = GetComponent<EnemyController>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    private void LateUpdate()
    {
        if (healthCanvas == null || !healthCanvas.gameObject.activeInHierarchy)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        // Billboard: หมุนให้ตรงกับกล้องพอดี
        healthCanvas.rotation = mainCamera.transform.rotation;
    }

    public void KnockOut(float stunDuration)
    {
        if (isDead || isStunned)
            return;

        if (knockOutRoutine != null)
            StopCoroutine(knockOutRoutine);

        knockOutRoutine = StartCoroutine(KnockOutRoutine(stunDuration));
    }

    private IEnumerator KnockOutRoutine(float stunDuration)
    {
        isStunned = true;
        SetMovementLocked(true);
        standingRotation = transform.rotation;
        transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, transform.eulerAngles.z);

        if (healthCanvas != null)
            healthCanvas.gameObject.SetActive(false);

        yield return new WaitForSeconds(stunDuration);

        if (!isDead)
        {
            isStunned = false;
            transform.rotation = standingRotation;
            SetMovementLocked(false);

            if (healthCanvas != null)
                healthCanvas.gameObject.SetActive(true);
        }

        knockOutRoutine = null;
    }

    public void TakeDamage(float amount)
    {
        if (isDead || isStunned)
            return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        UpdateHealthBar();

        if (currentHealth <= 0f)
            Die();
    }

    private void UpdateHealthBar()
    {
        // fillAmount รับค่า 0-1 เท่านั้น จึงต้องหารด้วยเลือดสูงสุด
        if (healthBarFill != null && maxHealth > 0f)
            healthBarFill.fillAmount = currentHealth / maxHealth;
    }

    private void Die()
    {
        isDead = true;
        SetMovementLocked(true);

        switch (onDeath)
        {
            case DeathAction.Destroy:
                Destroy(gameObject);
                break;

            case DeathAction.FallOver:
                // หมุนให้นอนลง 90 องศาแนวแกน X และปิดหลอดเลือด
                transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, transform.eulerAngles.z);

                if (healthCanvas != null)
                    healthCanvas.gameObject.SetActive(false);
                break;

            case DeathAction.Revive:
                // ชุบชีวิต กลับมาเลือดเต็ม
                isDead = false;
                SetMovementLocked(false);
                currentHealth = maxHealth;
                UpdateHealthBar();
                break;
        }
    }

    private void SetMovementLocked(bool locked)
    {
        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();

        if (enemyController != null)
            enemyController.SetMovementLocked(this, locked);
    }
}
