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
        SettingsWindow.SetActive(true);
        RootPanel.SetActive(true);
        AudioPanel.SetActive(false);
        ControlPanel.SetActive(false);
        CurrentTab = SettingsTab.Root;
    }

    public void CloseSettings()
    {
        RootPanel.SetActive(true);
        AudioPanel.SetActive(false);
        ControlPanel.SetActive(false);
        SettingsWindow.SetActive(false);
    }

    public void ChangeTab(SettingsTab tab)
    {
        CurrentTab = tab;
        RootPanel.SetActive(tab == SettingsTab.Root);
        AudioPanel.SetActive(tab == SettingsTab.Audio);
        ControlPanel.SetActive(tab == SettingsTab.Control);
    }
}