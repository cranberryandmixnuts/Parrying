using UnityEngine;
using Sirenix.OdinInspector;

public sealed class SceneLoadButton : MonoBehaviour
{
    [ValidateInput(nameof(IsValidScene), "SceneType이 None이면 로드할 수 없습니다!"), SerializeField]
    private SceneType scene = SceneType.None;

    private bool Loaded = false;

    public void Load()
    {
        if (scene == SceneType.None || Loaded) return;
        Loaded = true;

        InputManager.Instance.SetAllModes(InputMode.Auto);
        SceneLoader.Instance.LoadScene(scene);
    }

    private void OnDisable()
    {
        if (Loaded && SceneLoader.Instance.IsTransitioning)
            InputManager.Instance.SetAllModes(InputMode.Manual);
    }

    private bool IsValidScene(SceneType value) => value != SceneType.None;
}