using Sirenix.OdinInspector;
using UnityEngine;

public sealed class TitleSceneManager : Singleton<TitleSceneManager, SceneScope>
{
    private const string VolumeMasterKey = "Volume_Master_Db";
    private const string VolumeBgmKey = "Volume_BGM_Db";
    private const string VolumeSfxKey = "Volume_SFX_Db";
    private const string RebindsKey = "InputService_Rebinds";

    private void Start()
    {
        AudioManager.Instance.SetBGM("TitleBGM", 1f);
        PlayerVitals.Instance.InitializePlayerStatus();
    }

    public void StartAtFirst()
    {
        PlayerVitals.Instance.InitializePlayerStatus();
        InputManager.Instance.SetAllModes(InputMode.Auto);
        SceneLoader.Instance.LoadScene(SceneType.GameStartScene);
    }

    public void SkipTutorial()
    {
        PlayerVitals.Instance.InitializePlayerStatus();
        PlayerVitals.Instance.TryConsumeEnergy(485);
        InputManager.Instance.SetAllModes(InputMode.Auto);
        SceneLoader.Instance.LoadScene(SceneType.ArenaScene);
    }

    [Button, DisableInEditorMode]
    public void ResetGameAndApply()
    {
        SettingsWindowManager.Instance.CloseSettings();

        VolumeController[] volumeControllers = FindAll<VolumeController>();

        for (int i = 0; i < volumeControllers.Length; i++)
        {
            VolumeController controller = volumeControllers[i];

            if (controller == null)
                continue;

            controller.ResetToDefaults();
        }

        if (InputManager.Instance.IsRebinding)
            InputManager.Instance.CancelCurrentRebind();

        InputManager.Instance.ClearBindingOverrides();

        InputManager.Instance.SetAllModes(InputMode.Manual);

        InputManager.Instance.Actions.Disable();
        InputManager.Instance.Actions.Enable();

        PlayerPrefs.DeleteKey(VolumeMasterKey);
        PlayerPrefs.DeleteKey(VolumeBgmKey);
        PlayerPrefs.DeleteKey(VolumeSfxKey);
        PlayerPrefs.DeleteKey(RebindsKey);
        PlayerPrefs.Save();
        PlayerVitals.Instance.InitializePlayerStatus();

        KeyBindingButton[] buttons = FindAll<KeyBindingButton>();

        for (int i = 0; i < buttons.Length; i++)
        {
            KeyBindingButton button = buttons[i];

            button.RefreshKeyDisplay();
        }

        Debug.Log("GameReset");
    }

    private static T[] FindAll<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
}