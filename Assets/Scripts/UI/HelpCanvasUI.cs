using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 타이틀 화면 오른쪽 하단 별 버튼(HelpButton, 구 StartButton)이 여는 가이드라인(Help) 화면.
// MobileControlsUI.cs와 동일한 관례를 따른다 — 프리팹/씬을 직접 만들지 않고 런타임에 코드로
// Canvas 전체를 생성한다(CreateRect 헬퍼 포함). StartSceneCanvasButtons.Awake()가 씬에 이 오브젝트가
// 없으면 하나 만들어 두고(UINavigator.OpenCanvas()는 이미 존재하는 씬 루트 오브젝트만 이름으로
// 찾으므로, 버튼을 누르기 전에 미리 존재해야 한다), HelpButton을 누르면 UINavigator.OpenHelpCanvas()가
// "UICanvas" 태그 + "HelpCanvas" 이름으로 이 오브젝트를 찾아 켠다.
//
// 무기/픽업 아이콘은 여기서 직접 에셋을 참조하지 않는다 — 이 오브젝트가 런타임에 코드로만 생성되어
// 인스펙터 슬롯이 없기 때문에, StartSceneCanvasButtons(실제 프리팹에 붙어 있어 인스펙터로 스프라이트를
// 참조할 수 있는 컴포넌트)가 Configure()로 넘겨준다 — MobileControlsUI.BuildJoystick()이
// AddComponent 직후 OnScreenJoystick.Init()을 호출하는 것과 같은 관례.
public class HelpCanvasUI : MonoBehaviour
{
    // 항목명(굵게) + 설명. 실제 게임 동작과 다른 부분은 보정했다:
    // - "Knife" → 실제 기본 무기는 맨손일 때 휘두르는 검이라 "Sword"로 표기
    // - 전기톱의 "사용시간 제한"은 미구현(탄약 무제한)이라 빼고, 강점인 다중 타격으로 대체
    // - 아드레날린 주사는 이동속도 증가가 아니라 대쉬(Shift 스프린트) 자체를 해금하는 픽업
    // Configure()가 채우는 icons 배열과 반드시 같은 순서를 유지해야 한다.
    private static readonly (string name, string desc)[] Entries =
    {
        ("Sword (Basic Weapon)", "Unlimited Use"),
        ("Chainsaw", "High Attack Rate, Hits Multiple Targets"),
        ("Rifle", "High Fire Rate, Low Damage"),
        ("Sniper Rifle", "Low Fire Rate, High Damage, Piercing Bullet"),
        ("Shotgun", "Low Fire Rate, Radial Bullet"),
        ("Supply Box", "Get a Random Weapon"),
        ("First Aid Kit", "Heals Player Health"),
        ("Adrenaline Juice", "Unlocks Dash (Sprint)"),
    };

    private const float RowHeight = 90f;
    private const float RowStartY = -220f; // 제목 아래 첫 줄까지의 거리(위쪽 기준 오프셋)
    private const float RowWidth = 1200f;
    private const float IconSize = 64f;
    private const float IconLeftMargin = 20f;
    private const float TextLeftMargin = IconLeftMargin + IconSize + 24f;

    private Sprite[] icons; // Entries와 같은 순서(8개). Configure() 호출 전까지는 null.

    private void Awake()
    {
        gameObject.tag = UINavigator.CanvasTag;

        BuildCanvas();
        BuildBackground();
        BuildTitle();
        BuildCloseButton();

        // 표의 각 줄(BuildRows)은 아이콘이 필요하므로 여기서 만들지 않는다 — Configure()가
        // AddComponent 직후 호출되어 아이콘을 채운 다음 BuildRows()를 실행한다.
    }

    /// <summary>
    /// StartSceneCanvasButtons가 AddComponent 직후 호출한다. 인스펙터로 연결된 실제 게임 스프라이트
    /// 4종(전기톱/라이플/샷건/저격총 장착 아이콘)과 픽업 상자 3종(웨폰드랍/힐링드랍/대쉬드랍)을 받는다.
    /// 맨손(검) 아이콘만 별도 에셋 연결 없이 WeaponVisuals에서 바로 가져온다. chainsawIcon이 비어 있으면
    /// (아직 실제 아트를 연결하지 않은 경우) WeaponVisuals.ChainsawIcon(절차형)으로 대체한다.
    /// </summary>
    public void Configure(Sprite chainsawIcon, Sprite rifleIcon, Sprite shotgunIcon, Sprite sniperIcon, Sprite weaponDropIcon, Sprite healingDropIcon, Sprite dashDropIcon)
    {
        var swordSprites = WeaponVisuals.FistsSwordSprites;
        Sprite swordIcon = swordSprites.Length > 0 ? swordSprites[0] : null;

        // Entries와 같은 순서: Sword, Chainsaw, Rifle, Sniper Rifle, Shotgun, Supply Box, First Aid Kit, Adrenaline Juice.
        icons = new[]
        {
            swordIcon,
            chainsawIcon != null ? chainsawIcon : WeaponVisuals.ChainsawIcon,
            rifleIcon,
            sniperIcon,
            shotgunIcon,
            weaponDropIcon,
            healingDropIcon,
            dashDropIcon,
        };

        BuildRows();

        // UINavigator.OpenCanvas()가 이름으로 찾아 켤 때까지는 숨겨둔다 — 만들어지자마자
        // 타이틀 화면을 덮으면 안 된다.
        gameObject.SetActive(false);
    }

    private void BuildCanvas()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
    }

    private void BuildBackground()
    {
        RectTransform bg = CreateRect("Background", transform,
            anchorMin: Vector2.zero, anchorMax: Vector2.one, pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero, size: Vector2.zero);

        var image = bg.gameObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.85f);
        image.raycastTarget = true; // 뒤쪽(타이틀 화면) 클릭을 막는다.
    }

    private void BuildTitle()
    {
        CreateText("Title", transform,
            anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0f, -80f), size: new Vector2(600f, 80f),
            text: "HELP", fontSize: 56, bold: true);
    }

    // 표 형태: 각 줄 왼쪽에 아이콘(64x64), 오른쪽에 "이름 - 설명" 텍스트를 나란히 배치한다.
    private void BuildRows()
    {
        for (int i = 0; i < Entries.Length; i++)
        {
            var entry = Entries[i];
            Sprite icon = icons != null && i < icons.Length ? icons[i] : null;

            RectTransform row = CreateRect($"Row{i}", transform,
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
                anchoredPos: new Vector2(0f, RowStartY - i * RowHeight), size: new Vector2(RowWidth, RowHeight));

            if (icon != null)
            {
                RectTransform iconRect = CreateRect("Icon", row,
                    anchorMin: new Vector2(0f, 0.5f), anchorMax: new Vector2(0f, 0.5f), pivot: new Vector2(0f, 0.5f),
                    anchoredPos: new Vector2(IconLeftMargin, 0f), size: new Vector2(IconSize, IconSize));

                var iconImage = iconRect.gameObject.AddComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            string line = $"<b>{entry.name}</b>  -  {entry.desc}";

            CreateText("Text", row,
                anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0f, 1f), pivot: new Vector2(0f, 0.5f),
                anchoredPos: new Vector2(TextLeftMargin, 0f), size: new Vector2(RowWidth - TextLeftMargin - 20f, 0f),
                text: line, fontSize: 32, bold: false, horizontalAlignment: HorizontalAlignmentOptions.Left);
        }
    }

    private void BuildCloseButton()
    {
        RectTransform closeRect = CreateRect("CloseButton", transform,
            anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
            anchoredPos: new Vector2(-40f, -40f), size: new Vector2(140f, 60f));

        var image = closeRect.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.15f);

        var button = closeRect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(OnClickClose);

        CreateText("Text", closeRect,
            anchorMin: Vector2.zero, anchorMax: Vector2.one, pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero, size: Vector2.zero,
            text: "Close", fontSize: 28, bold: false);
    }

    private void OnClickClose()
    {
        UINavigator.Instance.OpenStartSceneCanvas();
    }

    // MobileControlsUI.CreateRect()와 동일한 헬퍼.
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

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, string text, int fontSize, bool bold, HorizontalAlignmentOptions horizontalAlignment = HorizontalAlignmentOptions.Center)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, pivot, anchoredPos, size);

        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.horizontalAlignment = horizontalAlignment;
        tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        tmp.color = Color.white;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.raycastTarget = false;

        return tmp;
    }
}
