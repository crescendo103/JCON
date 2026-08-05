using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StartSceneCanvas(시작 화면)의 Play/Help 버튼을 코드로 연결한다.
/// 인스펙터 드래그 연결 대신 이름으로 찾아서 붙인다 (팀 규칙).
/// StartSceneCanvas 프리팹의 루트 오브젝트에 붙인다.
/// </summary>
public class StartSceneCanvasButtons : MonoBehaviour
{
    [Header("가이드라인(Help) 화면 아이콘")]
    [Tooltip("Assets/Weapons/Chainsaw.asset의 equippedSprite와 같은 그림")]
    [SerializeField] private Sprite chainsawIcon;
    [Tooltip("Assets/Weapons/Rifle.asset의 equippedSprite와 같은 그림")]
    [SerializeField] private Sprite rifleIcon;
    [Tooltip("Assets/Weapons/Shotgun.asset의 equippedSprite와 같은 그림")]
    [SerializeField] private Sprite shotgunIcon;
    [Tooltip("Assets/Weapons/Sniper.asset의 equippedSprite와 같은 그림")]
    [SerializeField] private Sprite sniperIcon;
    [Tooltip("WeaponPickup(에어드랍 무기 상자)의 boxSprite와 같은 그림")]
    [SerializeField] private Sprite weaponDropIcon;
    [Tooltip("MedicalPickup(구급상자)의 boxSprite와 같은 그림")]
    [SerializeField] private Sprite healingDropIcon;
    [Tooltip("DashPickup(아드레날린 주사)의 boxSprite와 같은 그림")]
    [SerializeField] private Sprite dashDropIcon;

    [Header("사운드 설정 팝업")]
    [Tooltip("Assets/Prefabs/TitleContainer/SoundSettingsCanvas.prefab을 연결")]
    [SerializeField] private GameObject soundSettingsCanvasPrefab;

    private GameObject soundSettingsCanvas;

    private void Awake()
    {
        Button playButton = FindButton("PlayButton");

        if (playButton != null)
            playButton.onClick.AddListener(OnClickPlay);

        Button helpButton = FindButton("HelpButton");

        if (helpButton != null)
            helpButton.onClick.AddListener(OnClickHelp);

        Button soundButton = FindButton("SoundButton");

        if (soundButton != null)
            soundButton.onClick.AddListener(OnClickSound);

        // UINavigator.OpenCanvas()는 씬에 이미 존재하는 루트 오브젝트만 이름으로 찾으므로,
        // HelpButton을 누르기 전에 미리 만들어 둔다(GamePlayerController.Awake()가
        // MobileControlsUI를 자동 생성하는 것과 같은 관례). HelpCanvasUI는 런타임에 코드로만
        // 만들어져 인스펙터 슬롯이 없으므로, 여기서 인스펙터로 연결된 아이콘을 Configure()로 넘긴다.
        if (FindFirstObjectByType<HelpCanvasUI>() == null)
        {
            var helpCanvas = new GameObject("HelpCanvas").AddComponent<HelpCanvasUI>();
            helpCanvas.Configure(chainsawIcon, rifleIcon, shotgunIcon, sniperIcon, weaponDropIcon, healingDropIcon, dashDropIcon);
        }
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

    // 가이드라인(Help) 화면을 띄운다.
    private void OnClickHelp()
    {
        UINavigator.Instance.OpenHelpCanvas();
    }

    // 사운드 설정 팝업(배경음악/효과음 슬라이더)을 지금 씬 위에 그대로 띄운다. 씬 전환이 없으므로
    // 닫을 때(BackButton, SoundSettingsUI.cs)도 그냥 이 캔버스만 끄고 타이틀 화면으로 돌아온다.
    private void OnClickSound()
    {
        if (soundSettingsCanvas == null)
        {
            if (soundSettingsCanvasPrefab == null)
            {
                Debug.LogWarning("[StartSceneCanvasButtons] soundSettingsCanvasPrefab이 연결되지 않았다.");
                return;
            }

            soundSettingsCanvas = Instantiate(soundSettingsCanvasPrefab);
        }

        soundSettingsCanvas.SetActive(true);
    }
}
