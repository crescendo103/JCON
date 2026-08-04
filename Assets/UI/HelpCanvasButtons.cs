using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HelpCanvas(도움말 화면, 테스트용)의 닫기 버튼을 코드로 연결한다.
/// 인스펙터 드래그 연결 대신 이름으로 찾아서 붙인다 (팀 규칙).
/// HelpCanvas 프리팹의 루트 오브젝트에 붙인다.
/// </summary>
public class HelpCanvasButtons : MonoBehaviour
{
    private void Awake()
    {
        Button closeButton = FindButton("CloseButton");

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);
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

    // 시작 화면으로 돌아간다.
    private void OnClickClose()
    {
        UINavigator.Instance.OpenStartSceneCanvas();
    }
}
