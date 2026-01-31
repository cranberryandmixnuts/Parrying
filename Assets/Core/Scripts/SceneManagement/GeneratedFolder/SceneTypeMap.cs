using System;
using System.Collections.Generic;

public static class SceneTypeMap
{
    private static readonly string[] SceneNames =
    {
        "",
        "TitleScene",
        "GameStartScene",
        "TutorialScene",
        "ArenaScene",
        "BossRoomScene",
        "EndingScene",
    };

    private static readonly string[] ScenePaths =
    {
        "",
        "Assets/Core/Scenes/TitleScene.unity",
        "Assets/Core/Scenes/InGame/GameStartScene.unity",
        "Assets/Core/Scenes/InGame/TutorialScene.unity",
        "Assets/Core/Scenes/InGame/ArenaScene.unity",
        "Assets/Core/Scenes/InGame/BossRoomScene.unity",
        "Assets/Core/Scenes/EndingScene.unity",
    };

    private static readonly bool[] EnabledInBuildSettings =
    {
        false,
        true,
        true,
        true,
        true,
        true,
        true,
    };

    private static readonly Dictionary<string, SceneType> NameToType = new(StringComparer.Ordinal)
    {
        { "TitleScene", SceneType.TitleScene },
        { "GameStartScene", SceneType.GameStartScene },
        { "TutorialScene", SceneType.TutorialScene },
        { "ArenaScene", SceneType.ArenaScene },
        { "BossRoomScene", SceneType.BossRoomScene },
        { "EndingScene", SceneType.EndingScene },
    };

    public static int TotalCount => SceneNames.Length;
    public static int BuildSceneCount => SceneNames.Length - 1;
    public static string GetName(SceneType sceneType) => SceneNames[(int)sceneType];
    public static string GetPath(SceneType sceneType) => ScenePaths[(int)sceneType];
    public static bool IsEnabledInBuildSettings(SceneType sceneType) => EnabledInBuildSettings[(int)sceneType];
    public static bool TryGetTypeByName(string sceneName, out SceneType sceneType) => NameToType.TryGetValue(sceneName, out sceneType);
}
