using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 화면 우상단(체력바/탄약바/일시정지 버튼 아래)에 표시되는 레이더식 미니맵. 플레이어는 항상
// 중앙에 고정하고, 반경(radius) 안에 있는 몬스터만 점으로 찍는다. MobileControlsUI/HelpCanvasUI와
// 같은 관례로 런타임에 코드로만 Canvas를 만든다(README: 인스펙터 드래그 연결 금지).
//
// 맵이 스테이지마다 절차적으로 생성되고(60x60 유닛, StageMapGenerator) 카메라는 세로 8유닛짜리
// 정사영이라 전체 맵을 다 담으면 점이 너무 작아진다. 그래서 라이브 카메라 렌더링(RenderTexture) 대신
// 플레이어 중심 좌표 변환만으로 그리는 레이더 방식을 쓴다 — 이 프로젝트에 2번째 카메라/RenderTexture
// 전례가 전혀 없고, 몬스터가 전부 Layer 0(Default)라 컬링마스크로 몬스터만 골라 찍을 수도 없다.
public class MinimapUI : MonoBehaviour
{
    [Header("레이더 범위")]
    [Tooltip("이 거리(월드 유닛) 안의 몬스터만 미니맵에 표시한다. 카메라 세로 크기(8유닛)의 약 2.25배")]
    [SerializeField] private float radius = 18f;

    [Header("패널 배치 (PlayUI의 체력바/탄약바/버튼들 아래)")]
    [SerializeField] private Vector2 panelAnchoredPosition = new Vector2(-20f, -190f);
    [SerializeField] private Vector2 panelSize = new Vector2(240f, 240f);

    [Header("색상")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color borderColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color playerColor = new Color(0.3f, 0.85f, 1f, 1f);
    [SerializeField] private Color monsterColor = new Color(0.95f, 0.2f, 0.15f, 0.95f);
    [SerializeField] private Color bossColor = new Color(1f, 0.85f, 0.1f, 1f);
    [SerializeField] private float playerDotSize = 16f;
    [SerializeField] private float monsterDotSize = 12f;

    private GamePlayerController player;
    private RectTransform panelRoot;
    private Image playerDot;
    // 몬스터 수는 스테이지마다 다르므로 고정 크기 풀 대신 필요할 때마다 늘어나는 리스트를 쓴다.
    private readonly List<Image> monsterDotPool = new List<Image>();
    private float panelHalfSize;
    private bool currentlyVisible = true;

    private void Awake()
    {
        panelHalfSize = panelSize.x * 0.5f;
        BuildUI();
    }

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go.GetComponent<GamePlayerController>();
    }

    private void Update()
    {
        // 씬 로드 순서상 플레이어가 아직 없을 수 있는 극단적인 경우를 대비해 계속 재탐색한다
        // (MobileControlsUI.Start()와 같은 방어).
        if (player == null) FindPlayer();

        // 일시정지/도움말/포기/결과 화면이나 사망 시에는 숨긴다(PlayerAimIndicator와 같은 관례).
        bool visible = player != null && Time.timeScale > 0f && !StageManager.IsGameOver && player.GetHealth() > 0f;

        if (currentlyVisible != visible)
        {
            currentlyVisible = visible;
            playerDot.enabled = visible;
            if (!visible) HideAllMonsterDots();
        }

        if (!visible) return;

        UpdateMonsterDots();
    }

    private void UpdateMonsterDots()
    {
        var monsters = StageManager.Instance != null ? StageManager.Instance.AliveMonsters : null;
        int shown = 0;

        if (monsters != null)
        {
            Vector2 playerPos = player.transform.position;

            foreach (var go in monsters)
            {
                if (go == null) continue;

                var mc = go.GetComponent<MonsterController>();
                // 사망 애니메이션이 끝날 때까지(최대 2초) GameObject가 안 지워지지만, 미니맵에서는
                // 죽는 즉시 사라져야 한다 — IsDead로 걸러낸다.
                if (mc == null || mc.IsDead) continue;

                Vector2 delta = (Vector2)go.transform.position - playerPos;
                if (delta.sqrMagnitude > radius * radius) continue; // 반경 밖은 표시하지 않는다

                Image dot = GetOrCreateMonsterDot(shown);
                dot.gameObject.SetActive(true);
                dot.color = (mc is BossController) ? bossColor : monsterColor;
                dot.rectTransform.anchoredPosition = delta * (panelHalfSize / radius);
                shown++;
            }
        }

        for (int i = shown; i < monsterDotPool.Count; i++)
        {
            if (monsterDotPool[i].gameObject.activeSelf) monsterDotPool[i].gameObject.SetActive(false);
        }
    }

    private void HideAllMonsterDots()
    {
        for (int i = 0; i < monsterDotPool.Count; i++)
        {
            if (monsterDotPool[i].gameObject.activeSelf) monsterDotPool[i].gameObject.SetActive(false);
        }
    }

    // 필요한 만큼만 점을 늘려가며 재사용한다(스테이지마다 몬스터 수가 다르므로 고정 풀 대신).
    private Image GetOrCreateMonsterDot(int index)
    {
        while (monsterDotPool.Count <= index)
        {
            RectTransform rect = CreateRect("MonsterDot", panelRoot,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
                anchoredPos: Vector2.zero, size: new Vector2(monsterDotSize, monsterDotSize));

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = DotSprite;
            image.raycastTarget = false;
            image.gameObject.SetActive(false);

            monsterDotPool.Add(image);
        }

        return monsterDotPool[index];
    }

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // MobileControlsCanvas(100)보다 위, HelpCanvasUI(200)보다 아래.
        canvas.sortingOrder = 120;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        // PlayUI 1의 CanvasScaler와 같은 값(0 = Match Width)으로 맞춰 좌표 감각을 일치시킨다.
        scaler.matchWidthOrHeight = 0f;

        gameObject.AddComponent<GraphicRaycaster>();

        panelRoot = CreateRect("MinimapPanel", transform,
            anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
            anchoredPos: panelAnchoredPosition, size: panelSize);

        RectTransform background = CreateRect("Background", panelRoot,
            anchorMin: Vector2.zero, anchorMax: Vector2.one, pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero, size: Vector2.zero);
        var backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.sprite = DiscSprite;
        backgroundImage.color = backgroundColor;
        backgroundImage.raycastTarget = false;

        RectTransform border = CreateRect("Border", panelRoot,
            anchorMin: Vector2.zero, anchorMax: Vector2.one, pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero, size: Vector2.zero);
        var borderImage = border.gameObject.AddComponent<Image>();
        borderImage.sprite = RingSprite;
        borderImage.color = borderColor;
        borderImage.raycastTarget = false;

        RectTransform playerRect = CreateRect("PlayerDot", panelRoot,
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero, size: new Vector2(playerDotSize, playerDotSize));
        playerDot = playerRect.gameObject.AddComponent<Image>();
        playerDot.sprite = DotSprite;
        playerDot.color = playerColor;
        playerDot.raycastTarget = false;
    }

    // MobileControlsUI.CreateRect와 같은 헬퍼(이 프로젝트는 공용 유틸 클래스 없이 파일마다 자기 것을 둔다).
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

    private static Sprite dotSprite;

    // 플레이어/몬스터/보스 점 전부 이 흰색 원 하나를 공유하고 Image.color로만 색을 바꾼다
    // (PlayerAimIndicator.BuildDotSprite와 같은 패턴).
    private static Sprite DotSprite
    {
        get
        {
            if (dotSprite == null) dotSprite = BuildSoftCircleSprite(16, 5.5f, 1.5f);
            return dotSprite;
        }
    }

    private static Sprite discSprite;

    // 미니맵 배경(반투명 원판). MobileControlsUI.BuildCircleSprite와 같은 절차형 패턴.
    private static Sprite DiscSprite
    {
        get
        {
            if (discSprite == null) discSprite = BuildSoftCircleSprite(128, 62f, 2f);
            return discSprite;
        }
    }

    private static Sprite ringSprite;

    // 미니맵 테두리(가장자리 얇은 링).
    private static Sprite RingSprite
    {
        get
        {
            if (ringSprite == null) ringSprite = BuildRingSprite(128, 60f, 4f, 1.5f);
            return ringSprite;
        }
    }

    private static Sprite BuildSoftCircleSprite(int size, float radiusPx, float feather)
    {
        var center = new Vector2(size * 0.5f, size * 0.5f);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.DontSave;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude;
                float alpha = Mathf.Clamp01((radiusPx - dist) / feather + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    private static Sprite BuildRingSprite(int size, float ringRadius, float ringThickness, float feather)
    {
        var center = new Vector2(size * 0.5f, size * 0.5f);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.DontSave;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude;
                float alpha = Mathf.Clamp01((ringThickness * 0.5f - Mathf.Abs(dist - ringRadius)) / feather + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }
}
