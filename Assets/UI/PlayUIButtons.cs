using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PlayUI(인게임 HUD)의 HelpButton/PauseButton/RestartButton을 코드로 연결한다.
/// UINavigator는 UIScene에만 존재하고 이 프리팹이 있는 스테이지 씬에는 없어서
/// 인스펙터에서 드래그로 참조할 대상이 없다. UINavigator.Instance는 어느 씬에서
/// 호출해도 알아서 찾거나 새로 만들어주는 싱글턴이라, 이렇게 코드로 연결하면
/// 인스펙터 참조 없이도 항상 동작한다 (팀 규칙: 이름으로 찾아서 붙인다).
///
/// HelpButton은 타이틀 화면과 똑같은 HelpCanvasUI(무기 가이드)를 그대로 재사용한다 —
/// StartSceneCanvasButtons와 동일한 방식으로 여기서도 코드로 직접 만들고 아이콘을 넘긴다.
/// 다만 씬 전환은 하지 않는다: 지금 씬 위에 오버레이로 띄우고 게임을 멈췄다가,
/// 닫으면 그대로 이어서 재생한다(onCloseOverride로 HelpCanvasUI의 기본 "시작 화면으로"
/// 동작을 이 씬에 맞는 동작으로 바꿔치기한다).
/// PlayUI 프리팹의 루트 오브젝트에 붙인다.
/// </summary>
public class PlayUIButtons : MonoBehaviour
{
    [Header("도움말 화면 아이콘 (StartSceneCanvasButtons와 같은 그림을 연결)")]
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

    private HelpCanvasUI helpCanvas;

    private void Awake()
    {
        Button helpButton = FindButton("HelpButton");
        Button restartButton = FindButton("RestartButton");
        Button pauseButton = FindButton("PauseButton");

        if (helpButton != null)
            helpButton.onClick.AddListener(OnClickHelp);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnClickRestart);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnClickPause);
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

    // 도움말 화면(타이틀과 동일한 HelpCanvasUI)을 지금 씬 위에 그대로 띄우고 게임을 멈춘다.
    private void OnClickHelp()
    {
        if (helpCanvas == null)
        {
            helpCanvas = new GameObject("HelpCanvas").AddComponent<HelpCanvasUI>();
            helpCanvas.Configure(chainsawIcon, rifleIcon, shotgunIcon, sniperIcon, weaponDropIcon, healingDropIcon, dashDropIcon);
            helpCanvas.onCloseOverride = OnHelpClosed;
        }

        Time.timeScale = 0f;
        AudioListener.pause = true;
        helpCanvas.gameObject.SetActive(true);
    }

    // 닫기 버튼: 씬 전환 없이 오버레이만 숨기고 게임을 이어서 재생한다.
    private void OnHelpClosed()
    {
        helpCanvas.gameObject.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    // 지금 스테이지를 처음부터 다시 로드한다.
    private void OnClickRestart()
    {
        UINavigator.Instance.ReloadCurrentScene();
    }

    // 씬을 완전히 멈추고(다시 누르면 재개), 씬 전환 없이 그 자리에서 정지한다.
    // 아이콘 토글(재생/정지 스프라이트 전환)은 PauseButton에 붙는 ButtonStateEffect가 전담한다.
    private void OnClickPause()
    {
        bool isPaused = Time.timeScale == 0f;

        Time.timeScale = isPaused ? 1f : 0f;
        AudioListener.pause = !isPaused;
    }
}
