using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float health;
    public float maxHealth;
    public Image healthBar;
    public Image DeadScene;

    [Header("Debug")]
    public bool isInvincible;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool isDead;
    private CharacterController characterController;
    private TopDownPlayerController movementController;

    void Start()
    {
        maxHealth = health;
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        characterController = GetComponent<CharacterController>();
        movementController = GetComponent<TopDownPlayerController>();

        if (DeadScene != null)
            DeadScene.gameObject.SetActive(false);
    }

    void Update()
    {
        UpdateHealthBar();

        if(!isDead && health <= 0)
            Die();
    }

    public void TakeDamage(float damage)
    {
        if (isDead || isInvincible)
            return;

        health = Mathf.Max(health - damage, 0f);
        UpdateHealthBar();

        if (health <= 0f)
            Die();
    }

    public void Respawn()
    {
        isDead = false;
        health = maxHealth;

        if (characterController != null)
            characterController.enabled = false;

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        if (characterController != null)
            characterController.enabled = true;

        if (movementController != null)
            movementController.SetMovementLocked(false);

        if (DeadScene != null)
            DeadScene.gameObject.SetActive(false);

        UpdateHealthBar();
    }

    private void Die()
    {
        isDead = true;
        health = 0f;

        if (movementController != null)
            movementController.SetMovementLocked(true);

        if (DeadScene != null)
            DeadScene.gameObject.SetActive(true);

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthBar == null || maxHealth <= 0f)
            return;

        healthBar.fillAmount = Mathf.Clamp01(health / maxHealth);
    }
}
