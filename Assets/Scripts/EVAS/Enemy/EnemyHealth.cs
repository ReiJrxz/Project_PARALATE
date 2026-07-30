using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    public bool IsDead => isDead;
    public bool IsStunned => isStunned;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }
    private void LateUpdate()
    {
        if(healthCanvas != null && Camera.main != null)
        {
            healthCanvas.LookAt(healthCanvas.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
        }
    }
    public void KnockOut(float stunDuration)
    {
        if (isDead || isStunned)
            return;

        if (knockOutRoutine != null)
            StopCoroutine(knockOutRoutine);

        knockOutRoutine = StartCoroutine(KnockOutRoutine(stunDuration));
    }

    IEnumerator KnockOutRoutine(float stunDuration)
    {
        isStunned = true;
        standingRotation = transform.rotation;
        transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, transform.eulerAngles.z);

        if (healthCanvas != null)
            healthCanvas.gameObject.SetActive(false);

        yield return new WaitForSeconds(stunDuration);

        if (!isDead)
        {
            isStunned = false;
            transform.rotation = standingRotation;

            if (healthCanvas != null)
                healthCanvas.gameObject.SetActive(true);
        }

        knockOutRoutine = null;
    }

    public void TakeDamage(float amount)
    {
        if (isDead || isStunned) return;

        Debug.Log("<color=yellow>รับดาเมจมา: " + amount + " เลือดก่อนโดนฟันคือ: " + currentHealth + "</color>");

        currentHealth -= amount;

        if (currentHealth < 0) currentHealth = 0;

        Debug.Log("<color=green>เลือดหลังโดนฟันเหลือ: " + currentHealth + "</color>");

        UpdateHealthBar();

        if (currentHealth == 0)
        {
            Die();
        }
    }
    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            // Fill Amount รับค่าตั้งแต่ 0 ถึง 1 เท่านั้น เลยต้องเอา เลือดปัจจุบัน / เลือดสูงสุด
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }
    }
    private void Die()
    {
        isDead = true;

        switch (onDeath)
        {
            case DeathAction.Destroy:
                // ทำให้หายไปจากฉาก
                Destroy(gameObject);
                break;

            case DeathAction.FallOver:
                // หมุน Capsule ให้นอนลง 90 องศาแนวแกน X
                transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, transform.eulerAngles.z);

                // ปิดหลอดเลือด
                if (healthCanvas != null) healthCanvas.gameObject.SetActive(false);
                break;

            case DeathAction.Revive:
                // ชุบชีวิต กลับมาเลือดเต็ม 100 เหมือนเดิม
                isDead = false;
                currentHealth = maxHealth;
                UpdateHealthBar();
                Debug.Log("Enemy Revived!");
                break;
        }
    }
}
