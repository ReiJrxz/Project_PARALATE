using UnityEngine;

/// <summary>
/// Keycard ที่เก็บได้ด้วยปุ่ม Interact (ใช้ระบบเดียวกับ ItemPickup/PickupDetector)
/// ต้องมี Collider และอยู่ใน Layer ที่ PickupDetector ตรวจ (itemLayer)
/// </summary>
[RequireComponent(typeof(Collider))]
public class KeycardPickup : MonoBehaviour, IInteractable
{
    [Min(1)] public int level = 1;

    [Tooltip("{0} = ระดับของบัตร")]
    public string promptTemplate = "กด E เพื่อเก็บ Keycard Lv.{0}";

    public AudioClip pickupSound;

    public string PromptText => string.Format(promptTemplate, level);

    public void Interact(GameObject interactor)
    {
        PlayerKeycards.GetOrAdd(interactor).Grant(level);
        AudioManager.PlayPickup(pickupSound, transform.position);

        Destroy(gameObject);
    }
}
