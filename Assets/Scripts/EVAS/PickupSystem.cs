using UnityEngine;

public class PickupSystem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Transform PickUp;      // ใส่ PickUp
    public float pickupRange = 3f;   // ระยะหยิบของ
    public LayerMask itemLayer;      // เลเยอร์ของไอเทม (ถ้าต้องการกรอง)

    public Vector3 holdPositionOffset; // ปรับตำแหน่งเผื่อปืนจมเข้าไปในตัวหรือลอยไป
    public Vector3 holdRotationOffset; // ปรับองศาการหันของปืน (X, Y, Z)

    private GameObject heldItem;     // เก็บข้อมูลของที่เราถืออยู่

    void Update()
    {
        // กดปุ่ม E เพื่อหยิบหรือวาง
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldItem == null)
            {
                TryPickup();
            }
            else
            {
                //DropItem();
            }
        }
    }

    void TryPickup()
    {
        // สร้าง Raycast พุ่งไปข้างหน้าตัวละครเพื่อตรวจจับไอเทม
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // ถ้า Raycast ชนวัตถุในระยะ
        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            // ตรวจสอบว่าวัตถุนั้นมี Tag ว่า "Pickup" หรือไม่
            if (hit.collider.CompareTag("Pickup"))
            {
                PickUpObject(hit.collider.gameObject);
            }
        }
    }

    void PickUpObject(GameObject item)
    {
        heldItem = item;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        Collider coll = item.GetComponent<Collider>();
        if (coll != null)
            coll.enabled = false;

        item.transform.SetParent(PickUp);

        item.transform.localPosition = holdPositionOffset;
        item.transform.localEulerAngles = holdRotationOffset;

        GunAction gun = item.GetComponent<GunAction>();
        if (gun != null)
            gun.isHeld = true;

        KnifeAction knife = item.GetComponent<KnifeAction>();
        if(knife != null )
            knife.isHeld = true;
    }

    /*void DropItem()
    {
        // เอาของออกจาก PickUp
        heldItem.transform.SetParent(null);

        // เปิดฟิสิกส์ให้ของตกลงพื้น
        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // เปิด Collider อีกครั้ง
        Collider coll = heldItem.GetComponent<Collider>();
        if (coll != null)
        {
            coll.enabled = true;
        }

        // เคลียร์ค่าตัวแปร
        heldItem = null;
    }*/
}
