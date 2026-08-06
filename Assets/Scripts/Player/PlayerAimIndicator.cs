using UnityEngine;
using UnityEngine.Rendering;

// 플레이어가 실제로 조준하고 있는 방향에 월드 공간 조준점(링)과 조준선을 그린다.
// 방향은 GamePlayerController.GetAimDirection()을 매 프레임 그대로 폴링한다(이 프로젝트에는
// UI 이벤트 규약이 없다 — PlayerHealthBarUI/AmmoBarUI와 같은 방식). 발사할 때 쓰는 값과
// 똑같은 값이라 PC 마우스든 모바일 공격 조이스틱이든 화면과 탄도가 항상 일치한다.
//
// 렌더러를 플레이어 자식이 아니라 씬 루트에 따로 만드는 이유: 플레이어 루트의 SortingGroup
// 안에 들어가면 sortingLayerID가 그룹 내부 순서로만 쓰이고 그룹 밖에서는 무의미해진다
// (SpawnMuzzleEffect()의 주석과 완전히 같은 이유).
[RequireComponent(typeof(GamePlayerController))]
public class PlayerAimIndicator : MonoBehaviour
{
    [Header("배치")]
    [Tooltip("발사 원점에서 조준점까지의 거리(월드 유닛). 무기별 사거리(라이플 15, 저격총 30, " +
             "전기톱 0.7)는 화면 밖이거나 몸통 안이라 못 쓴다. 카메라가 직교 size 4(화면 세로 " +
             "8유닛)라 1.6이면 항상 화면 안에 들어온다")]
    [SerializeField] private float aimDistance = 1.6f;
    [Tooltip("조준선이 캐릭터 몸통을 덮지 않도록 발사 원점에서 앞으로 띄우는 거리")]
    [SerializeField] private float lineStartOffset = 0.35f;

    [Header("모양")]
    [SerializeField] private float lineWidth = 0.045f;
    [Tooltip("플레이어 쪽 끝은 옅게, 조준점 쪽 끝은 진하게 이어진다")]
    [SerializeField] private Color lineStartColor = new Color(1f, 1f, 1f, 0.10f);
    [SerializeField] private Color lineEndColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color reticleColor = new Color(1f, 1f, 1f, 0.75f);
    [SerializeField] private float reticleScale = 1f;

    // 총구 이펙트가 sortingOrder + 1을 이미 쓰므로(SpawnMuzzleEffect) 그 위로 얹는다.
    private const int LineSortingOffset = 2;
    private const int ReticleSortingOffset = 3;

    private GamePlayerController player;
    private SortingGroup sortingGroup;
    private Transform indicatorRoot;
    private Transform reticleTransform;
    private LineRenderer aimLine;
    private SpriteRenderer reticleRenderer;

    private void Awake()
    {
        player = GetComponent<GamePlayerController>();
        // GamePlayerController.Awake()가 AddComponent로 이걸 붙이면 이 Awake가 그 자리에서 바로
        // 실행되므로, 그쪽 필드에 기대지 않고 SortingGroup을 직접 찾는다.
        sortingGroup = GetComponent<SortingGroup>();
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
        bool visible = Time.timeScale > 0f && !StageManager.IsGameOver && player.GetHealth() > 0f;

        if (aimLine.enabled != visible) aimLine.enabled = visible;
        if (reticleRenderer.enabled != visible) reticleRenderer.enabled = visible;
        if (!visible) return;

        Vector2 aimDir = player.GetAimDirection();
        Vector3 origin = player.GetAimOrigin();
        Vector3 tip = origin + (Vector3)(aimDir * aimDistance);

        // 몸통을 덮지 않게 조금 앞에서 시작하되, 오프셋이 조준 거리보다 크면 선이 뒤집히므로
        // 그럴 땐 그냥 원점에서 시작한다.
        Vector3 lineStart = lineStartOffset < aimDistance
            ? origin + (Vector3)(aimDir * lineStartOffset)
            : origin;

        aimLine.SetPosition(0, lineStart);
        aimLine.SetPosition(1, tip);
        reticleTransform.position = tip;
    }

    // 씬 루트에 "AimIndicator"(조준선) + 그 자식 "Reticle"(조준점)을 만든다.
    private void BuildIndicator()
    {
        var rootGO = new GameObject("AimIndicator");
        indicatorRoot = rootGO.transform;

        aimLine = rootGO.AddComponent<LineRenderer>();
        aimLine.useWorldSpace = true;            // 매 프레임 월드 좌표를 직접 넣는다
        aimLine.positionCount = 2;
        aimLine.alignment = LineAlignment.View;  // 2D 탑다운이라 리본이 항상 카메라를 향하게
        aimLine.textureMode = LineTextureMode.Stretch;
        aimLine.numCapVertices = 0;
        aimLine.numCornerVertices = 0;
        aimLine.startWidth = lineWidth;
        aimLine.endWidth = lineWidth;
        aimLine.startColor = lineStartColor;
        aimLine.endColor = lineEndColor;
        aimLine.sharedMaterial = UnlitSpriteMaterial;
        aimLine.shadowCastingMode = ShadowCastingMode.Off;
        aimLine.receiveShadows = false;
        aimLine.allowOcclusionWhenDynamic = false;

        var reticleGO = new GameObject("Reticle");
        reticleGO.transform.SetParent(indicatorRoot, false);
        reticleTransform = reticleGO.transform;
        reticleTransform.localScale = Vector3.one * reticleScale;

        reticleRenderer = reticleGO.AddComponent<SpriteRenderer>();
        reticleRenderer.sprite = ReticleSprite;
        reticleRenderer.color = reticleColor;
        reticleRenderer.sharedMaterial = UnlitSpriteMaterial;

        ApplySorting();
    }

    // 플레이어 SortingGroup의 레이어/순서를 읽어 그 위에 얹는다. sortingOrder는 씬마다 오버라이드될
    // 수 있어(프리팹 0, MapBuildScene 5) 상수로 박지 않고 런타임에 읽는다.
    private void ApplySorting()
    {
        if (sortingGroup == null) return;

        aimLine.sortingLayerID = sortingGroup.sortingLayerID;
        aimLine.sortingOrder = sortingGroup.sortingOrder + LineSortingOffset;
        reticleRenderer.sortingLayerID = sortingGroup.sortingLayerID;
        reticleRenderer.sortingOrder = sortingGroup.sortingOrder + ReticleSortingOffset;
    }

    private static Material unlitSpriteMaterial;

    /// <summary>
    /// 조준점/조준선 전용 unlit 머티리얼. LineRenderer는 머티리얼이 없으면 자홍색으로 나오고,
    /// 이 프로젝트의 SpriteRenderer 기본값은 URP 2D Lit(Renderer2D.asset의 m_DefaultMaterialType=0)
    /// 이라 2D 조명에 물든다. HUD 성격이라 항상 같은 밝기여야 해서 unlit을 강제한다.
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
                Debug.LogWarning("PlayerAimIndicator: unlit 스프라이트 셰이더를 못 찾았다. 조준점 색이 이상할 수 있다.");
                return null;
            }

            unlitSpriteMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
            return unlitSpriteMaterial;
        }
    }

    private static Sprite reticleSprite;

    // 아트를 인스펙터로 끌어다 붙일 수 없고(README: 코드로 연결), Pixel Guns 2D의 크로스헤어 PNG는
    // Resources 폴더 밖이라 Resources.Load도 안 된다. 그래서 링 + 십자 눈금을 텍스처에 직접 그린다
    // (CrosshairUI.BuildCrosshairSprite / MobileControlsUI의 절차형 스프라이트와 같은 패턴).
    private static Sprite ReticleSprite
    {
        get
        {
            if (reticleSprite == null) reticleSprite = BuildReticleSprite();
            return reticleSprite;
        }
    }

    private static Sprite BuildReticleSprite()
    {
        const int size = 32;
        const float ringRadius = 10.5f;
        const float ringThickness = 2f;
        const float feather = 1.2f;      // 가장자리를 살짝 흐려 계단 현상을 줄인다
        const float tickInner = 12.5f;
        const float tickOuter = 15.5f;
        const float tickHalfWidth = 1.1f;

        var center = new Vector2(size * 0.5f, size * 0.5f);
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.DontSave;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float dist = p.magnitude;

                float ring = Mathf.Clamp01((ringThickness * 0.5f - Mathf.Abs(dist - ringRadius)) / feather + 0.5f);
                // 링 바깥으로 상하좌우 네 방향에만 짧게 뻗는 눈금.
                bool inTickBand = dist >= tickInner && dist <= tickOuter;
                float tick = inTickBand && (Mathf.Abs(p.x) <= tickHalfWidth || Mathf.Abs(p.y) <= tickHalfWidth) ? 1f : 0f;

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(ring, tick)));
            }
        }

        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }
}
