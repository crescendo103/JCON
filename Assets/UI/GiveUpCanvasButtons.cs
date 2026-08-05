using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GiveUpCanvas(게임 포기 확인 화면, 테스트용)의 버튼들을 코드로 연결한다.
/// 인스펙터 드래그 연결 대신 이름으로 찾아서 붙인다 (팀 규칙).
/// GiveUpCanvas 프리팹의 루트 오브젝트에 붙인다.
/// </summary>
public class GiveUpCanvasButtons : MonoBehaviour
{
    private void Awake()
    {
        Button confirmButton = FindButton("ConfirmButton");
        Button cancelButton = FindButton("CancelButton");

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnClickConfirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnClickCancel);
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

    // 포기를 확정하고 시작 화면으로 돌아간다.
    private void OnClickConfirm()
    {
        UINavigator.Instance.OpenStartSceneCanvas();
    }

    // 포기하지 않고 스테이지 선택 화면으로 돌아간다.
    private void OnClickCancel()
    {
        UINavigator.Instance.OpenStageSelectCanvas();
    }
}
