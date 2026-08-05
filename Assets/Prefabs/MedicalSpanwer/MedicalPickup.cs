using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 필드에 스폰되는 구급상자 픽업. WeaponPickup과 동일한 낙하산 연출(공중에서 낙하산을 타고
// 착지 지점으로 떨어진 뒤 스쿼시/파괴 연출)을 그대로 쓰고, 이미지와 픽업 효과만 다르다.
// 무기 대신 플레이어 체력을 회복시킨다.
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(AudioSource))]
public class MedicalPickup : MonoBehaviour
{
    [Tooltip("획득 시 회복시킬 체력량")]
    public float healAmount = 30f;

    [Header("상자 비주얼")]
    [SerializeField] private Sprite boxSprite;
    [SerializeField] private Sprite brokenBoxSprite;
    [Tooltip("상자 크기(월드 유닛 배율)")]
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

    [Header("스포트라이트")]
    [Tooltip("낙하 중 상자와 함께 내려오는 2D 스포트라이트 색상")]
    [SerializeField] private Color spotlightColor = Color.white;
    [Tooltip("스포트라이트 밝기")]
    [SerializeField] private float spotlightIntensity = 2f;
    [Tooltip("스포트라이트가 완전히 밝은 안쪽 반경")]
    [SerializeField] private float spotlightInnerRadius = 0.2f;
    [Tooltip("스포트라이트가 어두워지며 사라지는 바깥 반경")]
    [SerializeField] private float spotlightOuterRadius = 1.5f;
    [Tooltip("상자 기준 스포트라이트 위치(로컬 좌표)")]
    [SerializeField] private Vector3 spotlightOffset = Vector3.zero;

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

    [Header("착지 이펙트")]
    [Tooltip("착지 순간 재생되는 먼지/충격 파티클. 비워두면 재생하지 않는다")]
    [SerializeField] private GameObject landingEffectPrefab;
    [Tooltip("landingEffectPrefab이 대개 3D 스케일 에셋이라 그대로 쓰면 너무 커서, 여기서 축소 배율을 곱한다")]
    [SerializeField] private float landingEffectScale = 0.3f;

    [Header("체력 회복 이펙트")]
    [Tooltip("체력 회복 시 플레이어 위치에서 재생되는 파티클. 비워두면 재생하지 않는다")]
    [SerializeField] private GameObject healEffectPrefab;
    [Tooltip("healEffectPrefab이 대개 3D 스케일 에셋이라 그대로 쓰면 너무 커서, 여기서 축소 배율을 곱한다")]
    [SerializeField] private float healEffectScale = 0.25f;

    [Header("사운드")]
    [Tooltip("체력 회복 시 재생되는 사운드. 상자가 획득 직후 곧바로 파괴되므로 자체 AudioSource 대신 PlayClipAtPoint로 재생한다")]
    [SerializeField] private AudioClip pickupSfx;
    [Tooltip("1을 넘기면 원본 클립보다 더 크게 증폭된다. 값이 너무 크면 소리가 찢어질(clipping) 수 있다")]
    [Range(0f, 10f)]
    [SerializeField] private float pickupSfxVolume = 1f;
    [Tooltip("스폰되어 낙하하는 동안(착지 전까지) 반복 재생되는 사운드")]
    [SerializeField] private AudioClip fallingSfx;
    [Tooltip("1을 넘기면 원본 클립보다 더 크게 증폭된다")]
    [Range(0f, 3f)]
    [SerializeField] private float fallingSfxVolume = 1f;

    private AudioSource audioSource;

    // 절차형 낙하산 스프라이트 캐시. WeaponPickup과는 별개의 static 필드라 서로 간섭하지 않는다.
    private static Sprite cachedParachuteSprite;

    // 루트 트랜스폼은 스포너가 정한 착지 위치에 항상 고정된다(콜라이더/그림자 기준점).
    // 낙하·스쿼시는 전부 자식(Box)의 로컬 트랜스폼에만 적용해 콜라이더 판정 반경과 무관하게 만든다.
    // 낙하산(Parachute)은 Box의 자식이라 낙하 중에는 상자와 같이 움직이고, 착지 시 따로 분리된다.
    private bool built;
    private int sortingLayerID;
    private Transform boxTransform;
    private Transform shadowTransform;
    private Transform parachuteTransform;
    private Transform spotlightTransform;
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

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        var rootRenderer = GetComponent<SpriteRenderer>();
        if (rootRenderer != null) rootRenderer.enabled = false;

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
        StartCoroutine(FallRoutine());
    }

    // MonsterHealthBar.BuildBarIfNeeded()와 동일한 패턴: 그림자/상자/낙하산 자식을 런타임에 만들어
    // 프리팹에 자식 노드를 직접 추가하지 않아도 되게 한다.
    private void BuildVisualsIfNeeded()
    {
        if (built) return;
        built = true;

        var rootRenderer = GetComponent<SpriteRenderer>();
        sortingLayerID = rootRenderer != null ? rootRenderer.sortingLayerID : 0;

        var shadowGO = new GameObject("Shadow");
        shadowGO.transform.SetParent(transform, false);
        shadowTransform = shadowGO.transform;
        shadowRenderer = shadowGO.AddComponent<SpriteRenderer>();
        shadowRenderer.sortingLayerID = sortingLayerID;
        shadowRenderer.sortingOrder = groundedSortingOrder - 1;
        shadowRenderer.sprite = WeaponVisuals.Placeholder;
        shadowRenderer.color = new Color(0f, 0f, 0f, 0f);

        var boxGO = new GameObject("Box");
        boxGO.transform.SetParent(transform, false);
        boxTransform = boxGO.transform;
        boxRenderer = boxGO.AddComponent<SpriteRenderer>();
        boxRenderer.sortingLayerID = sortingLayerID;
        boxRenderer.sprite = boxSprite;

        // Box의 자식으로 붙여 낙하 중엔 상자와 같이 움직이게 한다(별도 추적 코드 불필요).
        var parachuteGO = new GameObject("Parachute");
        parachuteGO.transform.SetParent(boxTransform, false);
        parachuteTransform = parachuteGO.transform;
        parachuteTransform.localPosition = parachuteOffset;
        parachuteRenderer = parachuteGO.AddComponent<SpriteRenderer>();
        parachuteRenderer.sortingLayerID = sortingLayerID;
        parachuteRenderer.sprite = parachuteSpriteOverride != null ? parachuteSpriteOverride : GetParachuteSprite();

        // 스포트라이트도 Box의 자식이라, 별도 추적 코드 없이 낙하 애니메이션(FallRoutine)에 맞춰
        // 상자와 함께 내려온다.
        var spotlightGO = new GameObject("Spotlight");
        spotlightGO.transform.SetParent(boxTransform, false);
        spotlightTransform = spotlightGO.transform;
        spotlightTransform.localPosition = spotlightOffset;

        var spotlight = spotlightGO.AddComponent<Light2D>();
        spotlight.lightType = Light2D.LightType.Point;
        spotlight.color = spotlightColor;
        spotlight.intensity = spotlightIntensity;
        spotlight.pointLightInnerRadius = spotlightInnerRadius;
        spotlight.pointLightOuterRadius = spotlightOuterRadius;
    }

    private void SetShadowAlpha(float alpha)
    {
        Color c = shadowRenderer.color;
        shadowRenderer.color = new Color(c.r, c.g, c.b, alpha);
    }

    private IEnumerator FallRoutine()
    {
        if (fallingSfx != null)
        {
            audioSource.loop = false;
            audioSource.PlayOneShot(fallingSfx, fallingSfxVolume);
        }

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
        audioSource.Stop();
        SpawnLandingEffect();

        // 낙하산은 착지와 동시에 분리되어 떠오르며 사라진다. LandRoutine(상자 스쿼시)과 동시에
        // 진행돼야 하므로 yield하지 않고 별도 코루틴으로 띄운다(fire-and-forget).
        StartCoroutine(ReleaseParachuteRoutine());
        yield return LandRoutine();
    }

    // 착지 순간 먼지/충격 파티클을 재생한다. 프리팹은 대개 Animator가 없는 순수 ParticleSystem이라
    // ParticleAutoDestroy를 직접 붙여서 재생이 끝나면 알아서 없어지게 한다.
    private void SpawnLandingEffect()
    {
        if (landingEffectPrefab == null) return;

        var effect = Instantiate(landingEffectPrefab, transform.position, Quaternion.identity);
        effect.transform.localScale = Vector3.one * landingEffectScale;

        foreach (var r in effect.GetComponentsInChildren<ParticleSystemRenderer>())
        {
            r.sortingLayerID = sortingLayerID;
            r.sortingOrder = groundedSortingOrder + 1;
        }

        if (effect.GetComponent<ParticleAutoDestroy>() == null)
            effect.AddComponent<ParticleAutoDestroy>();
    }

    // 체력 회복 시 플레이어 위치에서 재생되는 회복 파티클.
    // 플레이어를 부모로 붙여서, 파티클이 재생되는 동안 플레이어가 움직여도 같이 따라간다.
    private void SpawnHealEffect(Transform player)
    {
        if (healEffectPrefab == null) return;

        var effect = Instantiate(healEffectPrefab, player.position, Quaternion.identity, player);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localScale = Vector3.one * healEffectScale;

        foreach (var r in effect.GetComponentsInChildren<ParticleSystemRenderer>())
        {
            r.sortingLayerID = sortingLayerID;
            r.sortingOrder = groundedSortingOrder + 1;
        }

        if (effect.GetComponent<ParticleAutoDestroy>() == null)
            effect.AddComponent<ParticleAutoDestroy>();
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
        if (pc == null) return;

        collected = true;
        pc.Heal(healAmount);
        SpawnHealEffect(other.transform);

        if (pickupSfx != null) PlayClipAtPointAmplified(pickupSfx, transform.position, pickupSfxVolume);

        StartCoroutine(BreakRoutine());
    }

    // 체력 회복은 이미 끝난 뒤 재생되는 연출이라, 깨진 스프라이트가 없어도 안전하게 즉시 Destroy로 수렴한다.
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

    // AudioSource.PlayClipAtPoint는 내부적으로 AudioSource.volume을 쓰는데, 이 속성은 1을 넘겨도
    // 엔진이 그대로 클램프해서 소리가 커지지 않는다. PlayOneShot의 volumeScale은 클램프되지 않아
    // 1 이상으로 실제 증폭되므로, 상자가 파괴된 뒤에도 재생을 마칠 임시 AudioSource를 직접 만들어
    // PlayOneShot으로 재생한다(PlayClipAtPoint 대체).
    private static void PlayClipAtPointAmplified(AudioClip clip, Vector3 position, float volumeScale)
    {
        var go = new GameObject("PickupSfx (temp)");
        go.transform.position = position;
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 1f;
        src.PlayOneShot(clip, volumeScale);
        Destroy(go, clip.length);
    }

    private static Sprite GetParachuteSprite()
    {
        if (cachedParachuteSprite == null) cachedParachuteSprite = BuildParachuteSprite();
        return cachedParachuteSprite;
    }

    // 20x14 텍스처에 낙하산(돔형 캐노피 + 로프)을 직접 그려 스프라이트로 감싼다.
    // WeaponPickup.BuildParachuteSprite()와 동일한 절차형 스프라이트 패턴.
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
