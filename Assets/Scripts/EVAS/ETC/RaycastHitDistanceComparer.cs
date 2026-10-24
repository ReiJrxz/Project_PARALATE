using System.Collections.Generic;
using UnityEngine;

/// <summary>ใช้เรียง RaycastHit ตามระยะ โดยไม่สร้าง delegate/array ใหม่ทุกครั้ง (ใช้คู่กับ *NonAlloc)</summary>
public sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
{
    public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

    public int Compare(RaycastHit a, RaycastHit b)
    {
        return a.distance.CompareTo(b.distance);
    }
}
