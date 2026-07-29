using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScoreCanvas(스테이지 결과 화면)의 재시작/홈 버튼을 코드로 연결한다.
/// 인스펙터 드래그 연결 대신 이름으로 찾아서 붙인다 (팀 규칙).
/// ScoreCanvas 프리팹의 루트 오브젝트에 붙인다.
/// </summary>
public class ScoreCanvasButtons : MonoBehaviour
{
    private void Awake()
    {
        Button restartButton = FindButton("restartButton");
        Button homeButton = FindButton("homeButton (2)");
        Button nextStageButton = FindButton("playButton (1)");

        if (restartButton != null)
            restartButton.onClick.AddListener(OnClickRestart);

        if (homeButton != null)
            homeButton.onClick.AddListener(OnClickHome);

        if (nextStageButton != null)
            nextStageButton.onClick.AddListener(OnClickNextStage);
    }

    // 하위 계층이 몇 단계든 상관없이 이름으로 버튼을 찾는다.
    private Button FindButton(string buttonName)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.name == buttonName)
                return button;
        }
        return null;
    }

    // 지금 스테이지를 처음부터 다시 로드한다.
    private void OnClickRestart()
    {
        UINavigator.Instance.ReloadCurrentScene();
    }

    // 시작 화면(UIScene의 StartSceneCanvas)으로 돌아간다.
    private void OnClickHome()
    {
        UINavigator.Instance.OpenStartSceneCanvas();
    }

    // CurrentStage로 진입한다 (별 3개일 때만 이 버튼이 보이므로, 실제로는 다음 스테이지로 들어간다).
    private void OnClickNextStage()
    {
        UINavigator.Instance.OpenNextStage();
    }
}
