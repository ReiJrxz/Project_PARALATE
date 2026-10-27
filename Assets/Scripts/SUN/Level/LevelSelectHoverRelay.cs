using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ติดกับปุ่มจุดเกิดที่ LevelSelectUI สร้างขึ้นตอนรันไทม์ เพื่อบอกว่าเมาส์ชี้ (หรือเลือกด้วยคีย์บอร์ด/จอย)
/// ปุ่มไหนอยู่ จะได้โชว์รายละเอียดฝั่งขวา ไม่ต้องติดเองใน Prefab
/// </summary>
public class LevelSelectHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private Action onEnter;
    private Action onExit;

    public void Init(Action enter, Action exit)
    {
        onEnter = enter;
        onExit = exit;
    }

    public void OnPointerEnter(PointerEventData eventData) => onEnter?.Invoke();
    public void OnPointerExit(PointerEventData eventData) => onExit?.Invoke();
    public void OnSelect(BaseEventData eventData) => onEnter?.Invoke();
    public void OnDeselect(BaseEventData eventData) => onExit?.Invoke();
}
