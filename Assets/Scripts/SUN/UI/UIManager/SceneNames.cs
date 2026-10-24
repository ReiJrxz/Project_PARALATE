/// <summary>
/// ชื่อ Scene ทั้งหมดของเกม รวมไว้ที่เดียว (ต้องตรงกับชื่อใน Build Settings เป๊ะๆ)
/// LevelManager, AudioManager, MainMenuController, SceneNavigation และ Playtest hotkey ใช้ค่าชุดนี้ร่วมกัน
/// </summary>
public static class SceneNames
{
    public const string MainMenu = "MainMenu";
    public const string Base = "Base";
    public const string Tutorial = "Level0";

    /// <summary>Scene หน้า Options ที่โหลดแบบ Additive ซ้อนทับ MainMenu</summary>
    public const string Options = "OptionsMenu";

    /// <summary>ด่านที่เลือกเล่นได้จากฮับ เรียงตาม index ที่ LevelManager.LoadLevel ใช้</summary>
    public static readonly string[] Levels = { "Level1", "Level2", "Level3" };
}