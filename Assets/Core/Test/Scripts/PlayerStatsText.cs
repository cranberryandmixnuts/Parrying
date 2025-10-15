using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class PlayerStatsText : MonoBehaviour
{
    [SerializeField] private string format = "HP {0:0.#}/{1:0.#}  EN {2:0.#}/{3:0.#}";
    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        PlayerController p = PlayerController.Instance;
        if (p == null) return;
        label.text = string.Format(format, p.Health, p.MaxHealth, p.Energy, p.MaxEnergy);
    }
}