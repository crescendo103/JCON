using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>인게임 HUD(체력/경험치/타이머/처치수)를 실제 게임 데이터에 맞춰 갱신한다. UI 오브젝트 자체는 MCP로 미리 배치됨.</summary>
public class HUDController : MonoBehaviour
{
    [SerializeField] Slider healthSlider;
    [SerializeField] TMP_Text healthValueText;
    [SerializeField] Slider xpSlider;
    [SerializeField] TMP_Text levelText;
    [SerializeField] TMP_Text timerText;
    [SerializeField] TMP_Text killCountText;

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.PlayerHealth != null)
        {
            GameManager.Instance.PlayerHealth.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(GameManager.Instance.PlayerHealth.CurrentHealth, GameManager.Instance.PlayerHealth.MaxHealth);
        }

        if (LevelSystem.Instance != null)
        {
            LevelSystem.Instance.OnXPChanged += HandleXPChanged;
            HandleXPChanged(LevelSystem.Instance.Level, LevelSystem.Instance.CurrentXP, LevelSystem.Instance.XPToNext);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.PlayerHealth != null)
        {
            GameManager.Instance.PlayerHealth.OnHealthChanged -= HandleHealthChanged;
        }
        if (LevelSystem.Instance != null)
        {
            LevelSystem.Instance.OnXPChanged -= HandleXPChanged;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        if (timerText != null) timerText.text = GameManager.FormatTime(GameManager.Instance.ElapsedTime);
        if (killCountText != null && RunStats.Instance != null) killCountText.text = RunStats.Instance.KillCount.ToString();
    }

    void HandleHealthChanged(float current, float max)
    {
        if (healthSlider != null) healthSlider.value = max > 0f ? current / max : 0f;
        if (healthValueText != null) healthValueText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    void HandleXPChanged(int level, int currentXP, int xpToNext)
    {
        if (xpSlider != null) xpSlider.value = xpToNext > 0 ? (float)currentXP / xpToNext : 0f;
        if (levelText != null) levelText.text = $"Lv.{level}";
    }
}
