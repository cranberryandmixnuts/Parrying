using UnityEngine;
using TMPro;

public sealed class PlayerStatsText : MonoBehaviour
{
    [SerializeField] private TMP_Text Text;

    private void Update()
    {
        PlayerController p = PlayerController.Instance;

        Text.text = "HP " + p.Health + "/" + p.MaxHealth + "  |  EN " + p.Energy + "/" + p.MaxEnergy;
    }
}