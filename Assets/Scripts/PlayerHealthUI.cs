using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement player;

    [Header("Health Bar")]
    public Image healthBarFill;

    [Header("Health Text")]
    public TextMeshProUGUI healthText;

    [Header("Respawn")]
    public TextMeshProUGUI respawnText;
    public GameObject respawnPanel;

    [Header("Colors")]
    public Color fullHealthColor = new Color(0.2f, 0.8f, 0.2f);
    public Color midHealthColor = new Color(0.9f, 0.7f, 0.1f);
    public Color lowHealthColor = new Color(0.9f, 0.2f, 0.2f);
    public float midHealthThreshold = 0.6f;
    public float lowHealthThreshold = 0.3f;

    void Start()
    {
        if (respawnPanel != null)
            respawnPanel.SetActive(false);
    }

    void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerMovement>();
            if (player == null) return;
        }

        UpdateHealthBar();
        UpdateRespawnUI();
    }

    private void UpdateHealthBar()
    {
        float healthPercent = (float)player.currentHealth / player.maxHealth;
        healthPercent = Mathf.Clamp01(healthPercent);

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = healthPercent;

            if (healthPercent <= lowHealthThreshold)
                healthBarFill.color = lowHealthColor;
            else if (healthPercent <= midHealthThreshold)
                healthBarFill.color = midHealthColor;
            else
                healthBarFill.color = fullHealthColor;
        }

        if (healthText != null)
        {
            healthText.text = $"{player.currentHealth} / {player.maxHealth}";
        }
    }

    private void UpdateRespawnUI()
    {
        bool dead = player.IsDead;

        if (respawnPanel != null)
            respawnPanel.SetActive(dead);

        if (dead && respawnText != null)
        {
            float timer = player.RespawnTimer;
            if (timer > 0f)
                respawnText.text = $"Respawning in {Mathf.CeilToInt(timer)}...";
            else
                respawnText.text = "Respawning...";
        }
    }
}