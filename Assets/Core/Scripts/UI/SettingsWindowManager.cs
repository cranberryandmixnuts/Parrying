using UnityEngine;

public enum SettingsTab
{
    Root = 0,
    Audio,
    Control,
}

public sealed class SettingsWindowManager : MonoBehaviour
{
    [SerializeField] private GameObject SettingsWindow;
    [SerializeField] private GameObject RootPanel;
    [SerializeField] private GameObject AudioPanel;
    [SerializeField] private GameObject ControlPanel;

    [SerializeField] private bool EscapeToOpen = true;

    public SettingsTab CurrentTab { get; private set; } = SettingsTab.Root;

    private bool timePausedByThis = false;
    private float cachedTimeScale;
    private InputModeState cachedInputState;

    private void Start()
    {
        cachedTimeScale = Time.timeScale;
        cachedInputState = InputManager.Instance.GetModes();

        if (EscapeToOpen)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void Update()
    {
        if (!InputManager.Instance.EscapeDown) return;

        if (!SettingsWindow.activeSelf && EscapeToOpen)
        {
            OpenSettings();
            return;
        }

        if (CurrentTab == SettingsTab.Root)
        {
            CloseSettings();
            return;
        }

        ChangeTab(SettingsTab.Root);
    }

    public void OpenSettings()
    {
        if (!timePausedByThis)
        {
            Time.timeScale = 0f;
            timePausedByThis = true;
        }

        cachedInputState = InputManager.Instance.GetModes();
        cachedTimeScale = Time.timeScale;

        SettingsWindow.SetActive(true);
        RootPanel.SetActive(true);
        AudioPanel.SetActive(false);
        ControlPanel.SetActive(false);

        if (EscapeToOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        CurrentTab = SettingsTab.Root;
    }

    public void CloseSettings()
    {
        CurrentTab = SettingsTab.Root;

        RootPanel.SetActive(true);
        AudioPanel.SetActive(false);
        ControlPanel.SetActive(false);
        SettingsWindow.SetActive(false);

        if (EscapeToOpen)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        InputManager.Instance.SetModes(cachedInputState);

        if (timePausedByThis)
        {
            Time.timeScale = cachedTimeScale;
            timePausedByThis = false;
        }
    }

    public void ChangeTab(SettingsTab tab)
    {
        CurrentTab = tab;
        RootPanel.SetActive(tab == SettingsTab.Root);
        AudioPanel.SetActive(tab == SettingsTab.Audio);
        ControlPanel.SetActive(tab == SettingsTab.Control);
    }

    private void OnDisable() => CloseSettings();
}