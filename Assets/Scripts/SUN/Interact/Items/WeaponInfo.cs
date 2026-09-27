using UnityEngine;

public class WeaponInfo : MonoBehaviour
{
    public ItemData itemData;

    public string DisplayName => itemData != null ? itemData.itemName : gameObject.name;
    public Sprite Icon => itemData != null ? itemData.icon : null;
}