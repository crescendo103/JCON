using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "게임을 포기하시겠습니까?" 확인 팝업의 YesButton/NoButton을 코드로 연결한다.
/// 인스펙터 드래그 연결 대신 이름으로 찾아서 붙인다 (팀 규칙).
/// HelpCanvasUI/PlayUIButtons의 도움말 팝업과 같은 방식으로, 지금 씬 위에 오버레이로 띄워 쓰는
/// 프리팹이다 — 이 팝업을 여는 쪽(PlayUIButtons 등)이 Time.timeScale을 0으로 멈춰두므로,
/// No는 씬 전환 없이 이 팝업만 끄고 게임을 다시 재생시킨다. Yes는 UINavigator.OpenCanvas()가
/// 씬을 옮기기 전에 알아서 시간을 되돌리므로 별도로 처리하지 않고 시작 화면(StartSceneCanvas)만 켠다.
/// GiveUpConfirmCanvas 프리팹의 루트 오브젝트에 붙인다.
/// </summary>
public class GiveUpConfirmCanvas : MonoBehaviour
{
    private void Awake()
    {
        Button yesButton = FindButton("YesButton");
        Button noButton = FindButton("NoButton");

        if (yesButton != null)
            yesButton.onClick.AddListener(OnClickYes);

        if (noButton != null)
            noButton.onClick.AddListener(OnClickNo);
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

    // 포기를 확정한다: UIScene으로 돌아가 시작 화면(StartSceneCanvas)만 켠다.
    private void OnClickYes()
    {
        UINavigator.Instance.OpenStartSceneCanvas();
    }

    // 포기하지 않는다: 씬 전환 없이 이 팝업만 닫고 게임을 다시 재생시킨다.
    private void OnClickNo()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
