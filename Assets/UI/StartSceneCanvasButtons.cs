using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StartSceneCanvas(시작 화면)의 Play 버튼을 코드로 연결한다.
/// 인스펙터 드래그 연결 대신 이름으로 찾아서 붙인다 (팀 규칙).
/// StartSceneCanvas 프리팹의 루트 오브젝트에 붙인다.
/// </summary>
public class StartSceneCanvasButtons : MonoBehaviour
{
    private void Awake()
    {
        Button playButton = FindButton("PlayButton");

        if (playButton != null)
            playButton.onClick.AddListener(OnClickPlay);
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

    // 스테이지 선택 화면(SelectStageUI)을 띄운다.
    private void OnClickPlay()
    {
        UINavigator.Instance.OpenStageSelectCanvas();
    }
}
