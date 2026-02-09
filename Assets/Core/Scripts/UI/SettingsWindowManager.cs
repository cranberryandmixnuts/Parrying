using UnityEngine;

public enum SettingsTab
{
    Root = 0,
    Audio,
    Control,
}

public sealed class SettingsWindowManager : Singleton<SettingsWindowManager, SceneScope>
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

        if (EscapeToOpen) SetCusorMode(false);
        else SetCusorMode(true);
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
        cachedInputState = InputManager.Instance.GetModes();
        cachedTimeScale = Time.timeScale;

        if (!timePausedByThis)
        {
            Time.timeScale = 0f;
            timePausedByThis = true;
        }

        SettingsWindow.SetActive(true);
        RootPanel.SetActive(true);
        AudioPanel.SetActive(false);
        ControlPanel.SetActive(false);

        if (EscapeToOpen) SetCusorMode(true);

        CurrentTab = SettingsTab.Root;
    }

    public void CloseSettings()
    {
        CurrentTab = SettingsTab.Root;

        RootPanel.SetActive(true);
        AudioPanel.SetActive(false);
        ControlPanel.SetActive(false);
        SettingsWindow.SetActive(false);

        if (EscapeToOpen) SetCusorMode(false);

        if (InputManager.Instance != null)
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

    private void SetCusorMode(bool mode)
    {
        if (mode) Cursor.lockState = CursorLockMode.None;
        else Cursor.lockState = CursorLockMode.Locked;

        Cursor.visible = mode;
    }

    private void OnDisable() => CloseSettings();
}