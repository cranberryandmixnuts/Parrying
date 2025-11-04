using UnityEngine;
using TMPro;

public sealed class PlayerStatsText : MonoBehaviour
{
    [SerializeField] private TMP_Text Text;

    private PlayerController player;

    private void Start()
    {
        player = PlayerController.Instance;
    }

    private void Update()
    {
        Text.text = "HP " + player.Vitals.Health + "/" + player.Vitals.MaxHealth + "  |  EN " + player.Vitals.Energy + "/" + player.Vitals.MaxEnergy;
    }
}