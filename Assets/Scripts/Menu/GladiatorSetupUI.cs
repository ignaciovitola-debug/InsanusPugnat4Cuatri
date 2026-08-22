using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GladiatorSetupUI : MonoBehaviour
{
    [Header("Stats del gladiador principal")]
    [SerializeField] private int hp = 100;
    [SerializeField] private int damage = 10;

    [Header("UI (asignar en Inspector)")]
    [SerializeField] private TMP_Text hpLabel;
    [SerializeField] private TMP_Text damageLabel;

    [Header("Escena de combate")]
    [SerializeField] private string fightSceneName = "EscenaDePrueba";

    private void Start() => RefreshLabels();

    public void AddHP(int amount)
    {
        hp = Mathf.Clamp(hp + amount, 10, 300);
        RefreshLabels();
    }

    public void AddDamage(int amount)
    {
        damage = Mathf.Clamp(damage + amount, 1, 50);
        RefreshLabels();
    }

    public void StartFight()
    {
        // Guardamos los stats elegidos para que la escena de combate los lea
        PlayerPrefs.SetInt("PlayerHP", hp);
        PlayerPrefs.SetInt("PlayerDamage", damage);
        SceneManager.LoadScene(fightSceneName);
    }

    private void RefreshLabels()
    {
        hpLabel.text = $"HP: {hp}";
        damageLabel.text = $"Daño: {damage}";
    }
}