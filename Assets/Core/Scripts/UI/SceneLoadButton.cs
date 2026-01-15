using UnityEngine;
using Sirenix.OdinInspector;

public sealed class SceneLoadButton : MonoBehaviour
{
    [ValidateInput(nameof(IsValidScene), "SceneType이 None이면 로드할 수 없습니다."), SerializeField]
    private SceneType scene = SceneType.None;

    public void Load() => SceneLoader.Instance.LoadScene(scene);

    private bool IsValidScene(SceneType value) => value != SceneType.None;
}