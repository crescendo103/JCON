using UnityEngine;
using UnityEngine.Rendering;

// 플레이어가 조준하고 있는 방향에 레이저 포인터(가는 빨간 선 + 벽에 찍히는 광점)를 그린다.
// 방향은 GamePlayerController.GetAimDirection()을 매 프레임 그대로 폴링한다(이 프로젝트에는
// UI 이벤트 규약이 없다 — PlayerHealthBarUI/AmmoBarUI와 같은 방식). 발사할 때 쓰는 값과
// 똑같은 값이라 PC 마우스든 모바일 공격 조이스틱이든 화면과 탄도가 항상 일치한다.
//
// 레이저는 GameProjectile.cs가 총알을 지우는 것과 같은 레이어(WallGrid)에서 Physics2D.Raycast로
// 멈춘다 — 그래서 레이저가 끊기는 지점이 실제로 총알이 사라지는 지점과 같다. 근접(전기톱)과
// 맨손은 사거리가 0.7~0.8의 원형 판정이라 긴 직선을 그리면 사거리를 오해하게 되므로,
// 총(원거리 무기)을 들었을 때만 켠다.
//
// 렌더러를 플레이어 자식이 아니라 씬 루트에 따로 만드는 이유: 플레이어 루트의 SortingGroup
// 안에 들어가면 sortingLayerID가 그룹 내부 순서로만 쓰이고 그룹 밖에서는 무의미해진다
// (SpawnMuzzleEffect()의 주석과 완전히 같은 이유).
[RequireComponent(typeof(GamePlayerController))]
public class PlayerAimIndicator : MonoBehaviour
{
    [Header("레이저")]
    [Tooltip("벽에 막히지 않았을 때 레이저가 뻗는 최대 길이. 저격총 사거리(30)를 덮는다")]
    [SerializeField] private float maxLaserDistance = 30f;
    [Tooltip("총구가 몸통에 파묻히지 않도록 발사 원점에서 앞으로 띄우는 거리")]
    [SerializeField] private float startOffset = 0.35f;
    [SerializeField] private float laserWidth = 0.03f;
    [SerializeField] private Color laserColor = new Color(1f, 0.15f, 0.1f, 0.85f);
    [SerializeField] private Color dotColor = new Color(1f, 0.35f, 0.3f, 0.95f);
    [SerializeField] private float dotScale = 1f;

    // 총구 이펙트가 sortingOrder + 1을 이미 쓰므로(SpawnMuzzleEffect) 그 위로 얹는다.
    private const int LaserSortingOffset = 2;
    private const int DotSortingOffset = 3;

    private GamePlayerController player;
    private SortingGroup sortingGroup;
    private Transform indicatorRoot;
    private Transform dotTransform;
    private LineRenderer laserLine;
    private SpriteRenderer dotRenderer;
    private int wallMask;

    private void Awake()
    {
        player = GetComponent<GamePlayerController>();
        // GamePlayerController.Awake()가 AddComponent로 이걸 붙이면 이 Awake가 그 자리에서 바로
        // 실행되므로, 그쪽 필드에 기대지 않고 SortingGroup을 직접 찾는다.
        sortingGroup = GetComponent<SortingGroup>();

        // GameProjectile.cs가 쓰는 것과 같은 레이어. 못 찾으면(레이어 이름이 바뀐 경우) 벽을
        // 무시하고 항상 최대 길이로 직진하는 쪽으로 안전하게 폴백한다.
        int wallLayer = LayerMask.NameToLayer("WallGrid");
        if (wallLayer < 0)
        {
            Debug.LogWarning("PlayerAimIndicator: \"WallGrid\" 레이어를 찾을 수 없다. 레이저가 벽을 무시하고 항상 최대 길이로 나간다.");
            wallMask = 0;
        }
        else
        {
            wallMask = 1 << wallLayer;
        }

        BuildIndicator();
    }

    // 플레이어 자식이 아니라 씬 루트에 있어서 플레이어가 파괴돼도 같이 안 지워진다. 직접 정리한다.
    private void OnDestroy()
    {
        if (indicatorRoot != null) Destroy(indicatorRoot.gameObject);
    }

    private void LateUpdate()
    {
        // 일시정지/도움말/포기/결과 화면은 전부 timeScale=0으로 멈춘다. 씬에 StageManager가 없으면
        // 사망해도 timeScale이 1로 남으므로 체력도 같이 본다(MobileControlsUI와 같은 규칙).
        // 총(원거리 무기)을 들었을 때만 레이저를 켠다 — 근접·맨손은 사거리 판정이 짧은 원이라
        // 긴 직선을 그리면 오해를 준다.
        bool visible = Time.timeScale > 0f && !StageManager.IsGameOver
                       && player.GetHealth() > 0f && player.HasRangedWeapon;

        if (laserLine.enabled != visible) laserLine.enabled = visible;
        if (!visible)
        {
            if (dotRenderer.enabled) dotRenderer.enabled = false;
            return;
        }

        Vector2 aimDir = player.GetAimDirection();
        Vector3 origin = player.GetAimOrigin();
        Vector2 start = (Vector2)origin + aimDir * startOffset;

        // Physics2DSettings.m_QueriesHitTriggers가 켜져 있어 마스크를 반드시 넘긴다 — 안 그러면
        // 총알/픽업 트리거에 레이저가 먼저 맞아 코앞에서 끊긴다.
        RaycastHit2D hit = Physics2D.Raycast(start, aimDir, maxLaserDistance, wallMask);
        bool hitWall = hit.collider != null;
        Vector2 end = hitWall ? hit.point : start + aimDir * maxLaserDistance;

        laserLine.SetPosition(0, new Vector3(start.x, start.y, origin.z));
        laserLine.SetPosition(1, new Vector3(end.x, end.y, origin.z));

        // 광점은 벽에 닿았을 때만 찍는다 — 허공에 뜬 점은 거리감을 헷갈리게 한다.
        if (dotRenderer.enabled != hitWall) dotRenderer.enabled = hitWall;
        if (hitWall) dotTransform.position = new Vector3(end.x, end.y, origin.z);
    }

    // 씬 루트에 "AimLaser"(레이저 선) + 그 자식 "Dot"(벽에 찍히는 광점)을 만든다.
    private void BuildIndicator()
    {
        var rootGO = new GameObject("AimLaser");
        indicatorRoot = rootGO.transform;

        laserLine = rootGO.AddComponent<LineRenderer>();
        laserLine.useWorldSpace = true;            // 매 프레임 월드 좌표를 직접 넣는다
        laserLine.positionCount = 2;
        laserLine.alignment = LineAlignment.View;  // 2D 탑다운이라 리본이 항상 카메라를 향하게
        laserLine.textureMode = LineTextureMode.Stretch;
        laserLine.numCapVertices = 0;
        laserLine.numCornerVertices = 0;
        laserLine.startWidth = laserWidth;
        laserLine.endWidth = laserWidth;
        laserLine.startColor = laserColor;
        laserLine.endColor = laserColor;           // 클래식 레이저는 굵기·밝기가 끝까지 균일하다
        laserLine.sharedMaterial = UnlitSpriteMaterial;
        laserLine.shadowCastingMode = ShadowCastingMode.Off;
        laserLine.receiveShadows = false;
        laserLine.allowOcclusionWhenDynamic = false;

        var dotGO = new GameObject("Dot");
        dotGO.transform.SetParent(indicatorRoot, false);
        dotTransform = dotGO.transform;
        dotTransform.localScale = Vector3.one * dotScale;

        dotRenderer = dotGO.AddComponent<SpriteRenderer>();
        dotRenderer.sprite = DotSprite;
        dotRenderer.color = dotColor;
        dotRenderer.sharedMaterial = UnlitSpriteMaterial;
        dotRenderer.enabled = false; // 첫 LateUpdate가 벽 판정을 하기 전까지는 꺼둔다.

        ApplySorting();
    }

    // 플레이어 SortingGroup의 레이어/순서를 읽어 그 위에 얹는다. sortingOrder는 씬마다 오버라이드될
    // 수 있어(프리팹 0, MapBuildScene 5) 상수로 박지 않고 런타임에 읽는다.
    private void ApplySorting()
    {
        if (sortingGroup == null) return;

        laserLine.sortingLayerID = sortingGroup.sortingLayerID;
        laserLine.sortingOrder = sortingGroup.sortingOrder + LaserSortingOffset;
        dotRenderer.sortingLayerID = sortingGroup.sortingLayerID;
        dotRenderer.sortingOrder = sortingGroup.sortingOrder + DotSortingOffset;
    }

    private static Material unlitSpriteMaterial;

    /// <summary>
    /// 레이저/광점 전용 unlit 머티리얼. LineRenderer는 머티리얼이 없으면 자홍색으로 나오고,
    /// 이 프로젝트의 SpriteRenderer 기본값은 URP 2D Lit(Renderer2D.asset의 m_DefaultMaterialType=0)
    /// 이라 2D 조명에 물든다. 레이저는 항상 같은 밝기여야 해서 unlit을 강제한다.
    /// 셰이더는 빌드에 확실히 포함되는 순서대로 찾는다.
    /// </summary>
    private static Material UnlitSpriteMaterial
    {
        get
        {
            if (unlitSpriteMaterial != null) return unlitSpriteMaterial;

            // 1순위: Renderer2D.asset의 m_DefaultUnlitMaterial이 참조 → 빌드에 항상 포함된다.
            // 2순위: GraphicsSettings의 m_SpritesDefaultMaterial(빌트인) → 에디터/빌트인 대비.
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");

            if (shader == null)
            {
                Debug.LogWarning("PlayerAimIndicator: unlit 스프라이트 셰이더를 못 찾았다. 레이저 색이 이상할 수 있다.");
                return null;
            }

            unlitSpriteMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
            return unlitSpriteMaterial;
        }
    }

    private static Sprite dotSprite;

    // 아트를 인스펙터로 끌어다 붙일 수 없고(README: 코드로 연결), Pixel Guns 2D의 크로스헤어 PNG는
    // Resources 폴더 밖이라 Resources.Load도 안 된다. 그래서 광점을 텍스처에 직접 그린다
    // (CrosshairUI.BuildCrosshairSprite / MobileControlsUI의 절차형 스프라이트와 같은 패턴).
    private static Sprite DotSprite
    {
        get
        {
            if (dotSprite == null) dotSprite = BuildDotSprite();
            return dotSprite;
        }
    }

    private static Sprite BuildDotSprite()
    {
        const int size = 16;
        const float radius = 5.5f;
        const float feather = 1.5f; // 가장자리를 살짝 흐려 계단 현상을 줄인다

        var center = new Vector2(size * 0.5f, size * 0.5f);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.DontSave;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude;
                float alpha = Mathf.Clamp01((radius - dist) / feather + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }
}
