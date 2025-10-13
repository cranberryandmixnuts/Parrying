using UnityEngine;

[CreateAssetMenu(fileName = "KeyData", menuName = "Scriptable Objects/KeyData")]
public class KeyData : ScriptableObject
{
    public PlayerData Player;
    public UI Ui;

    [System.Serializable]
    public class PlayerData
    {
        public KeyCode LeftMoveKey;
        public KeyCode RightMoveKey;
        public KeyCode DownMoveKey;
        public KeyCode JumpKey;
        public KeyCode DashKey;
        public KeyCode ParryKey;
        public KeyCode HealKey;
    }

    [System.Serializable]
    public class UI
    {
        public KeyCode UpKey;
        public KeyCode DownKey;
        public KeyCode LeftKey;
        public KeyCode RightKey;
        public KeyCode SelectKey;
        public KeyCode PauseKey;
    }
}
