using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ดูแลให้มี EventSystem เพียงตัวเดียวเสมอ ทำงานอัตโนมัติ ไม่ต้องวางใน Scene
/// - Scene ที่ไม่มี EventSystem (เช่น ลืมใส่ใน GameSystem ของด่าน) ปุ่ม UI จะกดไม่ได้เลย
///   ระบบจะสร้าง EventSystem + Input System UI Input Module ให้
/// - Scene ที่โหลดซ้อน (เช่น OptionsMenu) ถ้ามี EventSystem ของตัวเองซ้ำกับ Scene หลัก จะปิดตัวที่ซ้ำให้
/// </summary>
public static class EventSystemGuard
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // กันสมัครซ้ำ (กรณีปิด Domain Reload)
        SceneManager.sceneLoaded += OnSceneLoaded;

        EnsureEventSystem();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive)
            DisableDuplicateEventSystems(scene);
        else
            EnsureEventSystem();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject go = new GameObject("EventSystem (Auto)");
        go.AddComponent<EventSystem>();

        InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions(); // ใช้ชุด UI Action มาตรฐาน (คลิก, เลื่อน, Submit, Cancel)

        Debug.LogWarning("EventSystemGuard: Scene นี้ไม่มี EventSystem จึงสร้างให้อัตโนมัติ " +
                         "(แนะนำให้ใส่ EventSystem ไว้ใน GameSystem Prefab)");
    }

    private static void DisableDuplicateEventSystems(Scene addedScene)
    {
        EventSystem[] all = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

        bool hostHasOne = false;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].gameObject.scene != addedScene)
                hostHasOne = true;
        }

        if (!hostHasOne)
            return;

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].gameObject.scene == addedScene)
                all[i].gameObject.SetActive(false);
        }
    }
}
