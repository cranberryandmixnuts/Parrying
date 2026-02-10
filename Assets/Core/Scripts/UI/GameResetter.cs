using Sirenix.OdinInspector;
using UnityEngine;

public sealed class GameResetter : Singleton<GameResetter, SceneScope>
{
    private const string VolumeMasterKey = "Volume_Master_Db";
    private const string VolumeBgmKey = "Volume_BGM_Db";
    private const string VolumeSfxKey = "Volume_SFX_Db";
    private const string RebindsKey = "InputService_Rebinds";

    [Button]
    private void ResetGame()
    {
        PlayerPrefs.DeleteKey(VolumeMasterKey);
        PlayerPrefs.DeleteKey(VolumeBgmKey);
        PlayerPrefs.DeleteKey(VolumeSfxKey);
        PlayerPrefs.DeleteKey(RebindsKey);
        PlayerPrefs.Save();

        Debug.Log("GameReset");
    }

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
            controller.ClearSaved();
        }

        if (InputManager.Instance.IsRebinding)
            InputManager.Instance.CancelCurrentRebind();

        InputManager.Instance.ClearBindingOverrides();

        InputManager.Instance.SetAllModes(InputMode.Manual);

        InputManager.Instance.Actions.Disable();
        InputManager.Instance.Actions.Enable();

        ResetGame();

        KeyBindingButton[] buttons = FindAll<KeyBindingButton>();

        for (int i = 0; i < buttons.Length; i++)
        {
            KeyBindingButton button = buttons[i];

            button.RefreshKeyDisplay();
        }
    }

    private static T[] FindAll<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
}