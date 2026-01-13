using UnityEngine;

public class ChangeTabButton : MonoBehaviour
{
    [SerializeField] private SettingsWindowManager settingsWindowManager;
    [SerializeField] private SettingsTab tab = SettingsTab.Root;

    public void ChangeTab() => settingsWindowManager.ChangeTab(tab);
}