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
        PauseTime();

        SettingsWindow.SetActive(true);
        RootPanel.SetActive(true);
        AudioPanel.SetActive(false);
        ControlPanel.SetActive(false);
        CurrentTab = SettingsTab.Root;
    }

    public void CloseSettings()
    {
        CurrentTab = SettingsTab.Root;

        RootPanel.SetActive(true);
        AudioPanel.SetActive(false);
        ControlPanel.SetActive(false);
        SettingsWindow.SetActive(false);

        ResumeTime();
    }

    public void ChangeTab(SettingsTab tab)
    {
        CurrentTab = tab;
        RootPanel.SetActive(tab == SettingsTab.Root);
        AudioPanel.SetActive(tab == SettingsTab.Audio);
        ControlPanel.SetActive(tab == SettingsTab.Control);
    }

    private void PauseTime()
    {
        if (timePausedByThis) return;

        cachedInputState = InputManager.Instance.GetModes();
        cachedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        timePausedByThis = true;
    }

    private void ResumeTime()
    {
        if (!timePausedByThis) return;

        InputManager.Instance.SetModes(cachedInputState);
        Time.timeScale = cachedTimeScale;
        timePausedByThis = false;
    }

    private void OnDisable()
    {
        if (timePausedByThis) ResumeTime();
    }
}