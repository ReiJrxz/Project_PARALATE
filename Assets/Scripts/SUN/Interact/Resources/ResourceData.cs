using UnityEngine;

[CreateAssetMenu(fileName = "NewResource", menuName = "Items/Resource Data")]
public class ResourceData : ScriptableObject
{
    public string resourceName;
    public Sprite icon;
    public AudioClip pickupSound;

    [TextArea]
    [Tooltip("{0} = resourceName, {1} = amount")]
    public string promptFormat = "[ E ] to collect\nTake {0} x{1}";
}