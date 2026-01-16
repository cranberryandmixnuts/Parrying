public static class SceneTypeMap
{
    private static readonly string[] SceneNames =
    {
        "",
        "TitleScene",
    };

    private static readonly string[] ScenePaths =
    {
        "",
        "Assets/Core/Scenes/TitleScene.unity",
    };

    private static readonly bool[] EnabledInBuildSettings =
    {
        false,
        true,
    };

    public static int TotalCount => SceneNames.Length;
    public static int BuildSceneCount => SceneNames.Length - 1;
    public static string GetName(SceneType sceneType) => SceneNames[(int)sceneType];
    public static string GetPath(SceneType sceneType) => ScenePaths[(int)sceneType];
    public static bool IsEnabledInBuildSettings(SceneType sceneType) => EnabledInBuildSettings[(int)sceneType];
}
