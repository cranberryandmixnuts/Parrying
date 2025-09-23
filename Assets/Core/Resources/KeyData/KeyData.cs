using UnityEngine;

[CreateAssetMenu(fileName = "KeyData", menuName = "Scriptable Objects/KeyData")]
public class KeyData : ScriptableObject
{
    [Header("Moves")]
    public KeyCode LeftMoveKey;
    public KeyCode RightMoveKey;
    public KeyCode DashKey;
    public KeyCode JumpKey;

    [Header("Combats")]
    public KeyCode Parry;
}