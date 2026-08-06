using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 화면 좌측 절반(이동 조이스틱)/우측 절반(공격 조이스틱) 온스크린 콘솔을 통째로 런타임에 코드로 만든다.
// 프리팹/씬을 건드리지 않고 GamePlayerController.Awake()가 씬에 이게 없으면 하나 생성한다
// (PlayerStaminaBar.BuildBar()/CrosshairUI.BuildCrosshairSprite()와 같은 "런타임에 코드로 만든다" 관례).
//
// 조이스틱은 고정된 자리에 있지 않고 "플로팅"이다: 좌/우 절반 어디를 터치하든 그 지점에 배경 원이
// 순간이동해서 뜨고, 손을 떼면 사라진다(OnScreenJoystickBase가 재배치/표시·숨김을 담당, 여기서는
// 절반짜리 투명 터치 영역만 만들어 붙여준다).
public class MobileControlsUI : MonoBehaviour
{
    private const float JoystickBackgroundSize = 240f;
    private const float JoystickKnobSize = 100f;
    private const float JoystickRadius = 70f; // 노브 중심이 배경 중심에서 움직일 수 있는 최대 거리(px)
    private const float AttackButtonSize = 200f;
    private const float AttackKnobSize = 80f;
    private const float AttackRadius = 60f; // 이동 조이스틱과 같은 비율(배경 반지름 - 노브 반지름)
    private const float CornerMargin = 40f;

    // 화면 상단 이 높이(1080 기준)만큼은 터치 영역에서 제외한다 — PlayUI의 일시정지/재시작/도움말
    // 버튼(우상단, anchor(1,1))이 화면 절반짜리 터치 영역에 가려 안 눌리는 걸 막는다.
    private const float HudTopBandHeight = 160f;

    // 조이스틱을 안 누르고 있을 때도 "지금 장착한 무기"를 항상 보여주는 아이콘의 반투명도.
    private const float IdleWeaponIconAlpha = 0.55f;

    // 공격 조이스틱 중앙 아이콘: 현재 장착 무기에 맞춰 매 프레임 갱신한다(AmmoBarUI/PlayerHealthBarUI와
    // 같은 폴링 방식 — 이 프로젝트에는 UI 이벤트 규약이 없다).
    private GamePlayerController player;
    private Image attackIconImage;
    private Sprite defaultAttackIconSprite;

    // 일시정지/도움말/포기 팝업/결과 화면은 전부 Time.timeScale=0으로 게임을 멈춘다. 그동안은 이
    // sortingOrder=100 터치 영역이 그 UI들의 버튼(sortingOrder 0~15)을 가로채므로 통째로 꺼둔다.
    private RectTransform leftTouchArea;
    private RectTransform rightTouchArea;

    private void Awake()
    {
        EnsureEventSystem();
        BuildCanvas();
        BuildWeaponIcon();
        BuildJoystick();
        BuildAttackButton();
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.GetComponent<GamePlayerController>();
    }

    private void Update()
    {
        // 일시정지/도움말/포기 팝업/스테이지 종료는 전부 Time.timeScale=0으로 멈춘다. 다만 씬에
        // StageManager가 없으면 사망 시 GamePlayerController.Hit()이 EndStage()를 거치지 않고
        // SpawnScoreCanvas()만 불러 timeScale이 1로 남는다 — 체력도 같이 봐야 그 결과 화면 버튼이
        // 이 뒤(sortingOrder 100)의 투명 터치 영역에 가려지지 않는다.
        bool controlsActive = Time.timeScale > 0f && (player == null || player.GetHealth() > 0f);
        if (leftTouchArea != null && leftTouchArea.gameObject.activeSelf != controlsActive)
            leftTouchArea.gameObject.SetActive(controlsActive);
        if (rightTouchArea != null && rightTouchArea.gameObject.activeSelf != controlsActive)
            rightTouchArea.gameObject.SetActive(controlsActive);

        if (player == null || attackIconImage == null) return;

        Color color;
        Sprite sprite;
        if (player.TryGetEquippedWeaponIcon(out sprite, out color))
        {
            attackIconImage.sprite = sprite;
        }
        else
        {
            sprite = defaultAttackIconSprite;
            color = Color.white;
            attackIconImage.sprite = defaultAttackIconSprite;
        }

        color.a *= IdleWeaponIconAlpha;
        attackIconImage.color = color;
    }

    // 씬에 EventSystem이 없으면(플레이어 프리팹만 있는 테스트 씬 등) 하나 만든다.
    // 이 프로젝트가 실제로 쓰는 UnityEngine.InputSystem.UI.InputSystemUIInputModule을 그대로 맞춘다
    // (kimdongjuplayer 1.unity 씬의 기존 EventSystem이 이 모듈을 씀).
    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private Canvas canvas;

    private void BuildCanvas()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 다른 HUD(PlayUI 등) 위에 항상 그려지도록 충분히 큰 값을 준다.
        canvas.sortingOrder = 100;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
    }

    // 조이스틱이 안 떠 있을 때도 우하단에 항상 보이는 반투명 무기 아이콘. 좌/우 터치 영역보다 먼저
    // 만들어서 형제 순서상 뒤에 그려지는 터치 영역(과 그 자식인 조이스틱 배경)이 이 아이콘 위로
    // 겹쳐 보이게 한다 — 터치 중에는 조이스틱이, 안 누르고 있을 때는 이 아이콘만 남는다.
    private void BuildWeaponIcon()
    {
        var swordSprites = WeaponVisuals.FistsSwordSprites;
        if (swordSprites.Length == 0) return;

        RectTransform weaponIcon = CreateRect(
            "WeaponIcon", transform,
            anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 0f), pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(-(CornerMargin + AttackButtonSize * 0.5f), CornerMargin + AttackButtonSize * 0.5f),
            size: new Vector2(AttackKnobSize * 0.8f, AttackKnobSize * 0.8f));

        attackIconImage = weaponIcon.gameObject.AddComponent<Image>();
        defaultAttackIconSprite = swordSprites[0];
        attackIconImage.sprite = defaultAttackIconSprite;
        attackIconImage.preserveAspect = true; // 원본 스프라이트 비율 유지(찌그러지지 않게).
        attackIconImage.raycastTarget = false; // 터치 입력은 이 아래(뒤)의 터치 영역이 전담한다.

        var color = attackIconImage.color;
        color.a *= IdleWeaponIconAlpha;
        attackIconImage.color = color;
    }

    private void BuildJoystick()
    {
        // 화면 좌측 절반, 상단 HUD 버튼 높이만큼만 제외한 투명 터치 영역. 여기 어디를 눌러도
        // OnScreenJoystick.OnPointerDown이 눌린 지점에 배경 원을 옮겨서 띄운다.
        RectTransform touchArea = CreateStretchedRect(
            "LeftTouchArea", transform,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0.5f, 1f),
            offsetMin: Vector2.zero, offsetMax: new Vector2(0f, -HudTopBandHeight));
        var areaImage = touchArea.gameObject.AddComponent<Image>();
        areaImage.color = new Color(1f, 1f, 1f, 0f); // 완전 투명. raycastTarget은 알파와 무관하게 동작한다.
        areaImage.raycastTarget = true;
        leftTouchArea = touchArea;

        RectTransform background = CreateRect(
            "JoystickBackground", touchArea,
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero,
            size: new Vector2(JoystickBackgroundSize, JoystickBackgroundSize));
        var bgImage = background.gameObject.AddComponent<Image>();
        bgImage.sprite = BuildCircleSprite(new Color(1f, 1f, 1f, 0.25f));
        bgImage.raycastTarget = false; // 조이스틱 전체 입력은 이제 터치 영역(LeftTouchArea)이 받는다.

        RectTransform knob = CreateRect(
            "JoystickKnob", background,
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero,
            size: new Vector2(JoystickKnobSize, JoystickKnobSize));
        var knobImage = knob.gameObject.AddComponent<Image>();
        knobImage.sprite = BuildCircleSprite(new Color(1f, 1f, 1f, 0.6f));
        knobImage.raycastTarget = false;

        var joystick = touchArea.gameObject.AddComponent<OnScreenJoystick>();
        joystick.Init(touchArea, background, knob, JoystickRadius);
    }

    private void BuildAttackButton()
    {
        // 이동 조이스틱(BuildJoystick)과 완전히 같은 구조: 화면 우측 절반(상단 HUD 버튼 높이 제외)
        // 어디를 눌러도 배경 원이 그 지점에 떠서, 그 안에서 노브를 드래그해 공격 방향을 직접 조정할
        // 수 있다. GamePlayerController.GetAimDirection()이 이 방향을 이동 조이스틱과 동일한
        // 우선순위로 조준에 쓴다.
        RectTransform touchArea = CreateStretchedRect(
            "RightTouchArea", transform,
            anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(1f, 1f),
            offsetMin: Vector2.zero, offsetMax: new Vector2(0f, -HudTopBandHeight));
        var areaImage = touchArea.gameObject.AddComponent<Image>();
        areaImage.color = new Color(1f, 1f, 1f, 0f);
        areaImage.raycastTarget = true;
        rightTouchArea = touchArea;

        RectTransform background = CreateRect(
            "AttackBackground", touchArea,
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero,
            size: new Vector2(AttackButtonSize, AttackButtonSize));
        var bgImage = background.gameObject.AddComponent<Image>();
        bgImage.sprite = BuildCircleSprite(new Color(1f, 1f, 1f, 0.25f));
        bgImage.raycastTarget = false; // 조이스틱 전체 입력은 이제 터치 영역(RightTouchArea)이 받는다.

        RectTransform knob = CreateRect(
            "AttackKnob", background,
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero,
            size: new Vector2(AttackKnobSize, AttackKnobSize));
        var knobImage = knob.gameObject.AddComponent<Image>();
        knobImage.sprite = BuildCircleSprite(new Color(1f, 1f, 1f, 0.6f));
        knobImage.raycastTarget = false;

        var attackJoystick = touchArea.gameObject.AddComponent<OnScreenAttackButton>();
        attackJoystick.Init(touchArea, background, knob, AttackRadius);
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        return rt;
    }

    // CreateRect는 고정 크기 사각형용이라 화면 절반처럼 "부모를 따라 늘어나는" 영역은 못 만든다.
    // offsetMin/offsetMax는 pivot과 무관하게 앵커 기준 사각형의 실제 모서리를 그대로 지정한다
    // (예: anchorMax=(0.5,1)에 offsetMax=(0,-HudTopBandHeight)를 주면 상단만 그만큼 줄어든다).
    private static RectTransform CreateStretchedRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        // pivot(0.5,0.5) — OnScreenJoystickBase가 이 영역 중심 기준 로컬 좌표를 그대로 배경 원의
        // anchoredPosition으로 쓰므로(둘 다 같은 중심 pivot이어야 좌표계가 맞는다).
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        return rt;
    }

    // CrosshairUI.BuildCrosshairSprite()와 같은 패턴(Texture2D를 코드로 그려 스프라이트로 감싼다).
    // 실제 아트가 생기기 전까지 쓰는 플레이스홀더라 가장자리만 살짝 페더링해 계단 현상을 줄인다.
    private static Sprite BuildCircleSprite(Color color)
    {
        const int size = 128;
        const float feather = 2f;
        float radius = size / 2f - 1f;
        var center = new Vector2(size / 2f, size / 2f);

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.DontSave;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01((radius - dist) / feather + 0.5f);
                var c = color;
                c.a *= alpha;
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }
}
