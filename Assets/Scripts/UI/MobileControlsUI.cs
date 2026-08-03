using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 화면 좌측 하단(이동 조이스틱)/우측 하단(공격 버튼) 온스크린 콘솔을 통째로 런타임에 코드로 만든다.
// 프리팹/씬을 건드리지 않고 GamePlayerController.Awake()가 씬에 이게 없으면 하나 생성한다
// (PlayerStaminaBar.BuildBar()/CrosshairUI.BuildCrosshairSprite()와 같은 "런타임에 코드로 만든다" 관례).
public class MobileControlsUI : MonoBehaviour
{
    private const float JoystickBackgroundSize = 240f;
    private const float JoystickKnobSize = 100f;
    private const float JoystickRadius = 70f; // 노브 중심이 배경 중심에서 움직일 수 있는 최대 거리(px)
    private const float AttackButtonSize = 200f;
    private const float AttackKnobSize = 80f;
    private const float AttackRadius = 60f; // 이동 조이스틱과 같은 비율(배경 반지름 - 노브 반지름)
    private const float CornerMargin = 40f;

    // 공격 조이스틱 중앙 아이콘: 현재 장착 무기에 맞춰 매 프레임 갱신한다(AmmoBarUI/PlayerHealthBarUI와
    // 같은 폴링 방식 — 이 프로젝트에는 UI 이벤트 규약이 없다).
    private GamePlayerController player;
    private Image attackIconImage;
    private Sprite defaultAttackIconSprite;

    private void Awake()
    {
        EnsureEventSystem();
        BuildCanvas();
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
        if (player == null || attackIconImage == null) return;

        if (player.TryGetEquippedWeaponIcon(out Sprite sprite, out Color color))
        {
            attackIconImage.sprite = sprite;
            attackIconImage.color = color;
        }
        else
        {
            attackIconImage.sprite = defaultAttackIconSprite;
            attackIconImage.color = Color.white;
        }
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

    private void BuildJoystick()
    {
        RectTransform background = CreateRect(
            "JoystickBackground", transform,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0f, 0f), pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(CornerMargin + JoystickBackgroundSize * 0.5f, CornerMargin + JoystickBackgroundSize * 0.5f),
            size: new Vector2(JoystickBackgroundSize, JoystickBackgroundSize));
        var bgImage = background.gameObject.AddComponent<Image>();
        bgImage.sprite = BuildCircleSprite(new Color(1f, 1f, 1f, 0.25f));
        bgImage.raycastTarget = true; // 조이스틱 전체 입력은 배경이 받는다(노브는 시각 전용).

        RectTransform knob = CreateRect(
            "JoystickKnob", background,
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero,
            size: new Vector2(JoystickKnobSize, JoystickKnobSize));
        var knobImage = knob.gameObject.AddComponent<Image>();
        knobImage.sprite = BuildCircleSprite(new Color(1f, 1f, 1f, 0.6f));
        // 노브가 배경 위에 겹쳐 있어도 항상 배경(OnScreenJoystick이 붙은 오브젝트)이 입력을 받도록 끈다.
        knobImage.raycastTarget = false;

        var joystick = background.gameObject.AddComponent<OnScreenJoystick>();
        joystick.Init(background, knob, JoystickRadius);
    }

    private void BuildAttackButton()
    {
        // 이동 조이스틱(BuildJoystick)과 완전히 같은 구조: 배경 원이 입력을 받고, 노브가 드래그를
        // 따라와 공격 방향을 직접 조정할 수 있다. GamePlayerController.GetAimDirection()이 이 방향을
        // 이동 조이스틱과 동일한 우선순위로 조준에 쓴다.
        RectTransform background = CreateRect(
            "AttackBackground", transform,
            anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 0f), pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(-(CornerMargin + AttackButtonSize * 0.5f), CornerMargin + AttackButtonSize * 0.5f),
            size: new Vector2(AttackButtonSize, AttackButtonSize));
        var bgImage = background.gameObject.AddComponent<Image>();
        bgImage.sprite = BuildCircleSprite(new Color(1f, 1f, 1f, 0.25f));
        bgImage.raycastTarget = true; // 조이스틱 전체 입력은 배경이 받는다(노브는 시각 전용).

        RectTransform knob = CreateRect(
            "AttackKnob", background,
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero,
            size: new Vector2(AttackKnobSize, AttackKnobSize));
        var knobImage = knob.gameObject.AddComponent<Image>();
        knobImage.sprite = BuildCircleSprite(new Color(1f, 1f, 1f, 0.6f));
        knobImage.raycastTarget = false;

        var attackJoystick = background.gameObject.AddComponent<OnScreenAttackButton>();
        attackJoystick.Init(background, knob, AttackRadius);

        // 무기 아이콘을 노브 중앙에 얹는다. 기본값은 맨손 공격 때 캐릭터가 휘두르는 그 검 아트를 그대로
        // 재사용하고(WeaponVisuals.FistsSwordSprites — 새로 그리지 않는다), 무기를 장착하면 Update()가
        // TryGetEquippedWeaponIcon()으로 그 무기의 아이콘으로 갈아끼운다. 노브의 자식이라 드래그해도
        // 항상 따라온다.
        var swordSprites = WeaponVisuals.FistsSwordSprites;
        if (swordSprites.Length > 0)
        {
            RectTransform weaponIcon = CreateRect(
                "WeaponIcon", knob,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
                anchoredPos: Vector2.zero,
                size: new Vector2(AttackKnobSize * 0.8f, AttackKnobSize * 0.8f));
            attackIconImage = weaponIcon.gameObject.AddComponent<Image>();
            defaultAttackIconSprite = swordSprites[0];
            attackIconImage.sprite = defaultAttackIconSprite;
            attackIconImage.preserveAspect = true; // 원본 스프라이트 비율 유지(찌그러지지 않게).
            attackIconImage.raycastTarget = false; // 아이콘이 드래그 입력을 가로채지 않게(배경이 계속 입력을 받음).
        }
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
