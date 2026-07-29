using System.Collections;
using UnityEngine;

// 필드에 스폰되는 미스테리 상자 픽업. 어떤 무기가 들었는지 겉모습으로는 알 수 없도록 항상 같은
// 상자 모습이며, 화면 위에서 낙하산을 타고 착지 지점으로 떨어진 뒤 플레이어가 트리거로 닿으면
// 무기를 지급하고 깨지는 연출 후 사라진다.
[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    public GameWeaponData weapon;

    [Header("상자 비주얼")]
    [SerializeField] private Sprite boxSprite;
    [SerializeField] private Sprite brokenBoxSprite;
    [Tooltip("상자 크기(월드 유닛 배율). 원본 14x14 @ PPU16 = 0.875유닛이라 0.7이면 약 0.61유닛")]
    [SerializeField] private float boxScale = 0.7f;
    [Tooltip("공중에 떠 있는 동안의 정렬 순서. 플레이어 자식 스프라이트 최대치(4)보다 커야 앞을 지나간다")]
    [SerializeField] private int fallingSortingOrder = 6;
    [Tooltip("착지 후 정렬 순서. 맵 배경 스프라이트(sortingOrder 0)보다 높아야 가려지지 않는다")]
    [SerializeField] private int groundedSortingOrder = 2;

    [Header("낙하산")]
    [Tooltip("낙하 중에만 보이는 낙하산 그래픽. 비워두면 절차형(코드로 그린) 낙하산을 사용한다")]
    [SerializeField] private Sprite parachuteSpriteOverride;
    [Tooltip("상자 기준 낙하산 위치(상자의 로컬 좌표계라 boxScale에 비례해서 크기가 맞춰진다)")]
    [SerializeField] private Vector3 parachuteOffset = new Vector3(0f, 1f, 0f);
    [Tooltip("착지 시 낙하산이 위로 떠오르며 사라지는 데 걸리는 시간(초)")]
    [SerializeField] private float parachuteReleaseDuration = 0.3f;

    [Header("낙하")]
    [Tooltip("착지 지점 기준 시작 높이(월드 유닛). 카메라 세로 시야가 10유닛이라 6이면 화면 밖에서 시작")]
    [SerializeField] private float fallHeight = 6f;
    [SerializeField] private float fallDuration = 2.5f;
    [Tooltip("낙하 진행 커브. 앞이 급하고 뒤가 완만한 형태(감속)라 낙하산처럼 사뿐히 내려앉는 느낌을 준다")]
    [SerializeField] private AnimationCurve fallCurve =
        new AnimationCurve(new Keyframe(0f, 0f, 0f, 1.5f), new Keyframe(1f, 1f, 0.2f, 0.2f));

    [Header("그림자")]
    [SerializeField] private Vector2 shadowSize = new Vector2(0.5f, 0.18f);
    [SerializeField] private float shadowMaxAlpha = 0.35f;

    [Header("착지/파괴 연출")]
    [SerializeField] private float squashDuration = 0.07f;
    [SerializeField] private float recoverDuration = 0.13f;
    [SerializeField] private float breakDuration = 0.25f;

    [Tooltip("스케일과 무관하게 유지할 픽업 판정 반경(월드 기준)")]
    [SerializeField] private float pickupRadius = 0.4f;

    // 절차형 낙하산 스프라이트 캐시. 픽업마다 새로 그리지 않도록 최초 1회만 만들어 공유한다.
    private static Sprite cachedParachuteSprite;

    // 루트 트랜스폼은 스포너가 정한 착지 위치에 항상 고정된다(콜라이더/그림자 기준점).
    // 낙하·스쿼시는 전부 자식(Box)의 로컬 트랜스폼에만 적용해 콜라이더 판정 반경과 무관하게 만든다.
    // 낙하산(Parachute)은 Box의 자식이라 낙하 중에는 상자와 같이 움직이고, 착지 시 따로 분리된다.
    private bool built;
    private Transform boxTransform;
    private Transform shadowTransform;
    private Transform parachuteTransform;
    private SpriteRenderer boxRenderer;
    private SpriteRenderer shadowRenderer;
    private SpriteRenderer parachuteRenderer;

    private bool isFalling;
    private bool collected;

#if UNITY_EDITOR
    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }
#endif

    private void Awake()
    {
        BuildVisualsIfNeeded();

        // 기존 루트 SpriteRenderer(예전엔 무기별 스프라이트를 여기 표시했음)는 더 이상 쓰지 않는다.
        // 상자 표시는 자식(Box)이 전담하므로 꺼둔다.
        var rootRenderer = GetComponent<SpriteRenderer>();
        if (rootRenderer != null) rootRenderer.enabled = false;

        // 루트 스케일은 항상 1로 고정되므로 스케일 역보정이 필요 없다(기존의 pickupRadius/scale 해킹 제거).
        var circleCol = GetComponent<CircleCollider2D>();
        if (circleCol != null) circleCol.radius = pickupRadius;

        // Instantiate 도중 이미 실행되므로, 상자가 바닥에 한 프레임이라도 먼저 보이지 않도록
        // 공중 시작 상태를 여기서 즉시 세팅한다.
        isFalling = true;
        boxTransform.localPosition = new Vector3(0f, fallHeight, 0f);
        boxTransform.localScale = Vector3.one * boxScale;
        boxRenderer.sortingOrder = fallingSortingOrder;
        parachuteRenderer.sortingOrder = fallingSortingOrder + 1;
        shadowTransform.localScale = new Vector3(shadowSize.x, shadowSize.y, 1f) * 0.4f;
        SetShadowAlpha(0.12f);
    }

    private void Start()
    {
        // 스포너가 Instantiate 직후 Setup(weapon)을 호출하므로, weapon이 세팅된 뒤인 Start에서
        // 낙하를 시작한다. 씬에 손으로 미리 배치해둔 픽업도 Setup 없이 자동으로 낙하한다.
        StartCoroutine(FallRoutine());
    }

    // 스포너가 Instantiate 직후 무기를 지정할 때 사용. 미스테리 상자라 겉모습은 무기 종류와
    // 무관하므로 데이터만 저장하고 시각적으로는 아무것도 갱신하지 않는다.
    public void Setup(GameWeaponData w)
    {
        weapon = w;
    }

    // MonsterHealthBar.BuildBarIfNeeded()와 동일한 패턴: 그림자/상자/낙하산 자식을 런타임에 만들어
    // 프리팹에 자식 노드를 직접 추가하지 않아도 되게 한다.
    private void BuildVisualsIfNeeded()
    {
        if (built) return;
        built = true;

        var rootRenderer = GetComponent<SpriteRenderer>();
        int layerID = rootRenderer != null ? rootRenderer.sortingLayerID : 0;

        var shadowGO = new GameObject("Shadow");
        shadowGO.transform.SetParent(transform, false);
        shadowTransform = shadowGO.transform;
        shadowRenderer = shadowGO.AddComponent<SpriteRenderer>();
        shadowRenderer.sortingLayerID = layerID;
        shadowRenderer.sortingOrder = groundedSortingOrder - 1;
        shadowRenderer.sprite = WeaponVisuals.Placeholder;
        shadowRenderer.color = new Color(0f, 0f, 0f, 0f);

        var boxGO = new GameObject("Box");
        boxGO.transform.SetParent(transform, false);
        boxTransform = boxGO.transform;
        boxRenderer = boxGO.AddComponent<SpriteRenderer>();
        boxRenderer.sortingLayerID = layerID;
        boxRenderer.sprite = boxSprite;

        // Box의 자식으로 붙여 낙하 중엔 상자와 같이 움직이게 한다(별도 추적 코드 불필요).
        var parachuteGO = new GameObject("Parachute");
        parachuteGO.transform.SetParent(boxTransform, false);
        parachuteTransform = parachuteGO.transform;
        parachuteTransform.localPosition = parachuteOffset;
        parachuteRenderer = parachuteGO.AddComponent<SpriteRenderer>();
        parachuteRenderer.sortingLayerID = layerID;
        parachuteRenderer.sprite = parachuteSpriteOverride != null ? parachuteSpriteOverride : GetParachuteSprite();
    }

    private void SetShadowAlpha(float alpha)
    {
        Color c = shadowRenderer.color;
        shadowRenderer.color = new Color(c.r, c.g, c.b, alpha);
    }

    private IEnumerator FallRoutine()
    {
        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = fallDuration > 0f ? Mathf.Clamp01(elapsed / fallDuration) : 1f;
            float eased = fallCurve.Evaluate(t);

            boxTransform.localPosition = new Vector3(0f, Mathf.Lerp(fallHeight, 0f, eased), 0f);

            // 상자가 가까워질수록 그림자가 커지고 진해진다 — 낙하 지점을 미리 읽을 수 있게 한다.
            shadowTransform.localScale = new Vector3(shadowSize.x, shadowSize.y, 1f) * Mathf.Lerp(0.4f, 1f, eased);
            SetShadowAlpha(Mathf.Lerp(0.12f, shadowMaxAlpha, eased));

            yield return null;
        }

        boxTransform.localPosition = Vector3.zero;
        shadowTransform.localScale = new Vector3(shadowSize.x, shadowSize.y, 1f);
        SetShadowAlpha(shadowMaxAlpha);

        // 착지: 이제부터 획득 가능하고, 정렬도 기존 픽업과 같은 바닥 클러터 순서로 내린다.
        isFalling = false;
        boxRenderer.sortingOrder = groundedSortingOrder;

        // 낙하산은 착지와 동시에 분리되어 떠오르며 사라진다. LandRoutine(상자 스쿼시)과 동시에
        // 진행돼야 하므로 yield하지 않고 별도 코루틴으로 띄운다(fire-and-forget).
        StartCoroutine(ReleaseParachuteRoutine());
        yield return LandRoutine();
    }

    // 착지 순간 납작하게 눌렸다가(스쿼시) 살짝 튀어 오르며 원래 크기로 돌아온다.
    // 자식(Box)의 로컬 스케일만 바꾸므로 루트 콜라이더 판정 반경에는 영향이 없다.
    private IEnumerator LandRoutine()
    {
        Vector3 normal = Vector3.one * boxScale;
        Vector3 squashed = new Vector3(normal.x * 1.25f, normal.y * 0.7f, normal.z);

        float elapsed = 0f;
        while (elapsed < squashDuration)
        {
            elapsed += Time.deltaTime;
            float t = squashDuration > 0f ? Mathf.Clamp01(elapsed / squashDuration) : 1f;
            boxTransform.localScale = Vector3.Lerp(normal, squashed, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < recoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = recoverDuration > 0f ? Mathf.Clamp01(elapsed / recoverDuration) : 1f;
            boxTransform.localScale = Vector3.Lerp(squashed, normal, t);
            yield return null;
        }

        boxTransform.localScale = normal;
    }

    // 낙하산이 위로 살짝 떠오르며 페이드아웃되어 상자에서 분리되는 느낌을 준다.
    private IEnumerator ReleaseParachuteRoutine()
    {
        Vector3 startPos = parachuteTransform.localPosition;
        Vector3 endPos = startPos + Vector3.up * 0.5f;
        Color startColor = parachuteRenderer.color;

        float elapsed = 0f;
        while (elapsed < parachuteReleaseDuration)
        {
            elapsed += Time.deltaTime;
            float t = parachuteReleaseDuration > 0f ? Mathf.Clamp01(elapsed / parachuteReleaseDuration) : 1f;

            parachuteTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            parachuteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));

            yield return null;
        }

        parachuteTransform.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 낙하 중(공중)에는 못 줍는다. MonsterController.TakeDamage의 isInvincible/isDead 조기리턴과 동일한 방식.
        if (isFalling || collected) return;
        if (!other.CompareTag("Player")) return;

        var pc = other.GetComponent<GamePlayerController>();
        if (pc == null || weapon == null) return;

        collected = true;
        pc.PickupWeapon(weapon);
        StartCoroutine(BreakRoutine());
    }

    // 무기 지급은 이미 끝난 뒤 재생되는 연출이라, 깨진 스프라이트가 없어도 안전하게 즉시 Destroy로 수렴한다.
    private IEnumerator BreakRoutine()
    {
        shadowRenderer.enabled = false;

        if (brokenBoxSprite == null || breakDuration <= 0f)
        {
            Destroy(gameObject);
            yield break;
        }

        boxRenderer.sprite = brokenBoxSprite;
        Vector3 startScale = boxTransform.localScale;
        Vector3 endScale = startScale * 1.15f;
        Color startColor = boxRenderer.color;

        float elapsed = 0f;
        while (elapsed < breakDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / breakDuration);

            boxTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            boxRenderer.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));

            yield return null;
        }

        Destroy(gameObject);
    }

    private static Sprite GetParachuteSprite()
    {
        if (cachedParachuteSprite == null) cachedParachuteSprite = BuildParachuteSprite();
        return cachedParachuteSprite;
    }

    // 20x14 텍스처에 낙하산(돔형 캐노피 + 로프)을 직접 그려 스프라이트로 감싼다.
    // CrosshairUI.BuildCrosshairSprite()/WeaponVisuals.Placeholder와 동일한 절차형 스프라이트 패턴이며,
    // 프로젝트 전체에 낙하산 아트가 없어서 직접 그린다. 실제 아트가 생기면 parachuteSpriteOverride에
    // 지정해 코드 수정 없이 바로 교체할 수 있다.
    private static Sprite BuildParachuteSprite()
    {
        const int w = 20;
        const int h = 14;
        const int domeBottom = 6; // 0~domeBottom-1: 로프, domeBottom~h-1: 캐노피 돔
        int domeTop = h - 1;
        int cx = w / 2;
        float domeHalfWidth = cx - 1;

        var clear = new Color(0f, 0f, 0f, 0f);
        var stripeA = new Color(0.85f, 0.15f, 0.1f, 1f);
        var stripeB = new Color(0.95f, 0.95f, 0.9f, 1f);
        var outline = Color.black;
        var rope = new Color(0.15f, 0.15f, 0.15f, 1f);

        var tex = new Texture2D(w, h);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = clear;

                if (y >= domeBottom)
                {
                    // 반원 돔: 정상에 가까울수록(위로 갈수록) 좁아진다.
                    float ny = (float)(y - domeBottom) / (domeTop - domeBottom);
                    float halfWidth = Mathf.Sqrt(Mathf.Max(0f, 1f - ny * ny)) * domeHalfWidth;
                    int left = Mathf.RoundToInt(cx - halfWidth);
                    int right = Mathf.RoundToInt(cx + halfWidth);

                    if (x >= left && x <= right)
                    {
                        bool isEdge = x == left || x == right || y == domeTop;
                        int stripeIndex = ((x - left) / 3) % 2;
                        c = isEdge ? outline : (stripeIndex == 0 ? stripeA : stripeB);
                    }
                }
                else
                {
                    // 로프: 하단 중앙(상자 연결점)에서 캐노피 가장자리로 벌어지며 올라간다.
                    float s = domeBottom > 1 ? (float)y / (domeBottom - 1) : 1f;
                    int leftRopeX = Mathf.RoundToInt(Mathf.Lerp(cx, 2, s));
                    int rightRopeX = Mathf.RoundToInt(Mathf.Lerp(cx, w - 3, s));

                    if (x == leftRopeX || x == rightRopeX) c = rope;
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        tex.hideFlags = HideFlags.DontSave;

        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }
}
