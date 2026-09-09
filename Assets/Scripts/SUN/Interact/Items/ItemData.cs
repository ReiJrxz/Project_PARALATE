using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/Item Data")]

public class ItemData : ScriptableObject
{
    public AudioClip pickupSound;
    public string itemName;
    public Sprite icon;
    [TextArea] public string promptText = "Press E to pick up";
}