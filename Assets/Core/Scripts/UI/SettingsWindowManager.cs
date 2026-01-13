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

    [SerializeField] private bool EscToOpen = true;

    public SettingsTab CurrentTab { get; private set; } = SettingsTab.Root;

    private float cachedTimeScale = 1f;
    private bool timePausedByThis;

    private void Update()
    {
        if (!InputManager.Instance.EscapeDown) return;

        if (!SettingsWindow.activeSelf && EscToOpen)
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

        cachedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        timePausedByThis = true;
    }

    private void ResumeTime()
    {
        if (!timePausedByThis) return;

        Time.timeScale = cachedTimeScale;
        timePausedByThis = false;
    }

    private void OnDisable()
    {
        if (timePausedByThis) ResumeTime();
    }
}