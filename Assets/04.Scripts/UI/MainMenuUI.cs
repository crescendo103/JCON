using UnityEngine;
using UnityEngine.UI;

/// <summary>메인 메뉴 버튼 바인딩.</summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] Button startButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button quitButton;

    void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(() => GameFlowManager.LoadGame());
        if (quitButton != null) quitButton.onClick.AddListener(() => GameFlowManager.QuitGame());
        if (settingsButton != null) settingsButton.onClick.AddListener(() => Debug.Log("설정 화면은 MVP 범위 밖입니다 (placeholder)."));
    }
}
