using System;
using System.Collections.Generic;

public static class SceneTypeMap
{
    private static readonly string[] SceneNames =
    {
        "",
        "TitleScene",
        "GameStartScene",
        "Tutorial1Scene",
        "SlateRoomScene",
        "Tutorial2Scene",
        "ArenaScene",
        "BossRoomScene",
        "EndingScene",
    };

    private static readonly string[] ScenePaths =
    {
        "",
        "Assets/Core/Scenes/TitleScene.unity",
        "Assets/Core/Scenes/Stage/GameStartScene.unity",
        "Assets/Core/Scenes/Stage/Tutorial1Scene.unity",
        "Assets/Core/Scenes/Stage/SlateRoomScene.unity",
        "Assets/Core/Scenes/Stage/Tutorial2Scene.unity",
        "Assets/Core/Scenes/Stage/ArenaScene.unity",
        "Assets/Core/Scenes/Stage/BossRoomScene.unity",
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
        true,
        true,
    };

    private static readonly Dictionary<string, SceneType> NameToType = new(StringComparer.Ordinal)
    {
        { "TitleScene", SceneType.TitleScene },
        { "GameStartScene", SceneType.GameStartScene },
        { "Tutorial1Scene", SceneType.Tutorial1Scene },
        { "SlateRoomScene", SceneType.SlateRoomScene },
        { "Tutorial2Scene", SceneType.Tutorial2Scene },
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
