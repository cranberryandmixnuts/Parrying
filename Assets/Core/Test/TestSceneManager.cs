using UnityEngine;

public class TestSceneManager : MonoBehaviour
{
    [SerializeField] private bool DoInitializePlayerStatus = true;
    [SerializeField] private bool StartToPlayTestAutoPilot = false;

    [Header("References")]
    [SerializeField] PlayerSettings playerSettings;
    [SerializeField] TestAutoPilot testAutoPilot;

    private void Start()
    {
        InputManager.Instance.SetCursorMode(true);

        if (DoInitializePlayerStatus)
            playerSettings.InitializePlayerStatus();

        if (StartToPlayTestAutoPilot)
            testAutoPilot.StartAutoPilot();
    }
}