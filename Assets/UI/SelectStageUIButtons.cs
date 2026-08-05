using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SelectStageUI(스테이지 선택 화면)의 홈/사운드 버튼을 코드로 연결한다.
/// 실제 사운드 버튼은 "homeButton (3)"이라는 이름 그대로 쓰고 있고(아직 이름/모양을 안 바꿈),
/// "homeButton (2)"는 이름 그대로 타이틀 화면으로 돌아가는 홈 버튼 역할이다.
/// 인스펙터 드래그 연결 대신 이름으로 찾아서 붙인다 (팀 규칙).
/// 사운드 버튼: StartSceneCanvasButtons의 OnClickSound와 같은 방식 — 씬 전환 없이
/// SoundSettingsCanvas를 오버레이로 띄운다(처음엔 인스턴스화, 이후엔 재사용). 닫는 쪽(BackButton)은
/// SoundSettingsUI.cs가 맡고 있어서 여기서는 여는 것만 처리한다.
/// 홈 버튼: UINavigator.OpenStartSceneCanvas()가 UIScene의 StartSceneCanvas만 켜고
/// SelectStageUI를 포함한 나머지 UICanvas는 알아서 꺼준다.
/// SelectStageUI 프리팹의 루트 오브젝트에 붙인다.
/// </summary>
public class SelectStageUIButtons : MonoBehaviour
{
    // 사운드 버튼으로 실제 쓰는 오브젝트 이름. "homeButton (3)"이라는 이름 그대로 사운드 버튼 역할을 한다.
    private const string SoundButtonName = "homeButton (3)";
    // 홈(타이틀로 돌아가기) 버튼으로 실제 쓰는 오브젝트 이름.
    private const string HomeButtonName = "homeButton (2)";

    [Header("사운드 설정 팝업")]
    [Tooltip("Assets/Prefabs/TitleContainer/SoundSettingsCanvas.prefab을 연결")]
    [SerializeField] private GameObject soundSettingsCanvasPrefab;

    private GameObject soundSettingsCanvas;

    private void Awake()
    {
        Button soundButton = FindButton(SoundButtonName);

        if (soundButton != null)
            soundButton.onClick.AddListener(OnClickSound);

        Button homeButton = FindButton(HomeButtonName);

        if (homeButton != null)
            homeButton.onClick.AddListener(OnClickHome);
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

    private void OnClickSound()
    {
        if (soundSettingsCanvas == null)
        {
            if (soundSettingsCanvasPrefab == null)
            {
                Debug.LogWarning("[SelectStageUIButtons] soundSettingsCanvasPrefab이 연결되지 않았다.");
                return;
            }

            soundSettingsCanvas = Instantiate(soundSettingsCanvasPrefab);
        }

        soundSettingsCanvas.SetActive(true);
    }

    // 타이틀 화면(StartSceneCanvas)으로 돌아간다. UINavigator가 SelectStageUI를 포함한
    // 나머지 UICanvas를 알아서 꺼주므로 여기서 따로 끌 필요가 없다.
    private void OnClickHome()
    {
        UINavigator.Instance.OpenStartSceneCanvas();
    }
}
