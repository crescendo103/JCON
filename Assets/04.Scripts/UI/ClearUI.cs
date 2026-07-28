using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>클리어(생존 성공) 패널. GameManager 상태 변화(Clear)를 구독해 자동으로 표시된다.</summary>
public class ClearUI : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] TMP_Text surviveTimeText;
    [SerializeField] TMP_Text killCountText;
    [SerializeField] TMP_Text levelText;
    [SerializeField] Button restartButton;
    [SerializeField] Button mainMenuButton;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(() => GameFlowManager.RestartGame());
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(() => GameFlowManager.LoadMainMenu());
    }

    void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    void HandleStateChanged(GameState state)
    {
        if (state != GameState.Clear) return;

        if (surviveTimeText != null) surviveTimeText.text = GameManager.FormatTime(GameManager.Instance.ElapsedTime);
        if (killCountText != null && RunStats.Instance != null) killCountText.text = RunStats.Instance.KillCount.ToString();
        if (levelText != null && LevelSystem.Instance != null) levelText.text = LevelSystem.Instance.Level.ToString();

        if (panelRoot != null) panelRoot.SetActive(true);
    }
}
