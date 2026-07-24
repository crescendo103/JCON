using System.Collections;
using UnityEngine;

// 몬스터 머리 위 체력바. 평소에는 숨겨져 있다가 피격(MonsterHealth.ApplyDamage) 시 나타나서
// visibleDuration초 동안 유지된 뒤 자동으로 사라진다. 연속 피격 시 타이머는 매번 갱신된다.
// fillSprite/backgroundSprite에 지정한 이미지를 색 보정 없이 그대로 사용한다(원본 스프라이트의
// 피벗과 무관하게 width/height 크기로 맞춰진다). 두 필드를 비워두면 아무것도 그려지지 않는다.
// MonsterHealth가 Awake()에서 GetComponent/AddComponent로 자동으로 붙여주므로
// 기존 몬스터 프리팹을 수정하지 않아도 동작한다.
public class MonsterHealthBar : MonoBehaviour
{
    [Header("스프라이트")]
    public Sprite fillSprite;
    public Sprite backgroundSprite;

    [Header("크기/위치")]
    public float offsetY = 0.16f;
    public float width = 0.3f;
    public float height = 0.05f;

    [Header("표시 시간")]
    public float visibleDuration = 1.5f;

    [Header("정렬")]
    // 몬스터 본체 SpriteRenderer보다 위에 그려지도록 더하는 오프셋 (배경/채움 순으로 +0, +1 사용)
    public int sortingOrderOffset = 10;

    private bool built;
    private Transform bgTransform;
    private Transform fillTransform;
    private SpriteRenderer bgRenderer;
    private SpriteRenderer fillRenderer;
    private Coroutine hideRoutine;
    private float lastRatio = 1f;

    void Awake()
    {
        BuildBarIfNeeded();
    }

    private void BuildBarIfNeeded()
    {
        if (built) return;
        built = true;

        var parentRenderer = GetComponent<SpriteRenderer>();
        int baseOrder = parentRenderer != null ? parentRenderer.sortingOrder : 0;
        int layerID = parentRenderer != null ? parentRenderer.sortingLayerID : 0;

        var bgGO = new GameObject("HealthBarBackground");
        bgGO.transform.SetParent(transform, false);
        bgTransform = bgGO.transform;
        bgRenderer = bgGO.AddComponent<SpriteRenderer>();
        bgRenderer.sortingLayerID = layerID;
        bgRenderer.sortingOrder = baseOrder + sortingOrderOffset;
        bgRenderer.enabled = false;

        var fillGO = new GameObject("HealthBarFill");
        fillGO.transform.SetParent(transform, false);
        fillTransform = fillGO.transform;
        fillRenderer = fillGO.AddComponent<SpriteRenderer>();
        fillRenderer.sortingLayerID = layerID;
        fillRenderer.sortingOrder = baseOrder + sortingOrderOffset + 1;
        fillRenderer.enabled = false;

        ApplySprites();
    }

    // fillSprite/backgroundSprite를 그대로 렌더러에 반영하고 크기를 맞춘다.
    private void ApplySprites()
    {
        bgRenderer.sprite = backgroundSprite;
        fillRenderer.sprite = fillSprite;

        PositionAndScaleCentered(bgTransform, backgroundSprite, width, height, 0f, offsetY, GetInverseParentScale());
        ApplyFillRatio(lastRatio);
    }

    // current/max 비율만큼 채움을 갱신하고 바를 켠 뒤, visibleDuration 후 다시 숨긴다.
    // 연속으로 호출되면(연속 피격) 숨김 타이머가 매번 새로 시작된다.
    // current가 0 이하(사망)이면 타이머를 기다리지 않고 즉시 숨긴다.
    public void Show(int current, int max)
    {
        BuildBarIfNeeded();

        float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        ApplyFillRatio(ratio);

        if (hideRoutine != null) StopCoroutine(hideRoutine);

        if (current <= 0)
        {
            hideRoutine = null;
            bgRenderer.enabled = false;
            fillRenderer.enabled = false;
            return;
        }

        bgRenderer.enabled = true;
        fillRenderer.enabled = true;

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private void ApplyFillRatio(float ratio)
    {
        lastRatio = ratio;
        float barLeft = -width * 0.5f;
        PositionAndScaleLeftAligned(fillTransform, fillRenderer.sprite, width, height, ratio, barLeft, offsetY, GetInverseParentScale());
    }

    // 몬스터 프리팹마다 루트 오브젝트의 스케일이 달라도(스프라이트 원본 크기 보정용으로 서로 다르게
    // 스케일돼 있음) 체력바는 항상 같은 월드 크기로 보이도록, 부모(몬스터 루트) 스케일의 역수를 구해
    // 체력바 자식 트랜스폼의 로컬 스케일/위치에 곱해 상쇄한다.
    private Vector2 GetInverseParentScale()
    {
        Vector3 s = transform.lossyScale;
        float sx = Mathf.Approximately(s.x, 0f) ? 1f : 1f / s.x;
        float sy = Mathf.Approximately(s.y, 0f) ? 1f : 1f / s.y;
        return new Vector2(sx, sy);
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(visibleDuration);

        bgRenderer.enabled = false;
        fillRenderer.enabled = false;
        hideRoutine = null;
    }

    // sprite의 실제 피벗/크기와 무관하게, 지정한 targetWidth x targetHeight 크기로
    // (targetCenterX, targetCenterY)를 중심으로 렌더링되도록 스케일/위치를 계산한다.
    // targetCenterX(가로 정렬)는 바 내부 레이아웃이라 inverseParentScale로 상쇄해 항상 같은 월드
    // 크기/정렬을 유지하지만, targetCenterY(offsetY, 머리 위로 띄우는 높이)는 몬스터 스케일에 비례해야
    // 큰 몬스터일수록 머리 위로 충분히 떠서 몸통에 파묻히지 않으므로 일부러 상쇄하지 않는다.
    private static void PositionAndScaleCentered(Transform t, Sprite sprite, float targetWidth, float targetHeight,
        float targetCenterX, float targetCenterY, Vector2 inverseParentScale)
    {
        if (sprite == null) return;

        Bounds b = sprite.bounds;
        float nativeW = b.size.x > 0f ? b.size.x : 1f;
        float nativeH = b.size.y > 0f ? b.size.y : 1f;

        float baseScaleX = targetWidth / nativeW;
        float baseScaleY = targetHeight / nativeH;

        t.localScale = new Vector3(baseScaleX * inverseParentScale.x, baseScaleY * inverseParentScale.y, 1f);
        t.localPosition = new Vector3(
            (targetCenterX - b.center.x * baseScaleX) * inverseParentScale.x,
            targetCenterY - b.center.y * baseScaleY * inverseParentScale.y,
            0f);
    }

    // sprite의 실제 피벗/크기와 무관하게, 왼쪽 끝이 targetLeftX에 고정된 채로
    // widthRatio(0~1)만큼만 targetWidth x targetHeight 크기 안에서 오른쪽으로 채워지도록 계산한다.
    // X축(바 내부 가로 정렬)만 inverseParentScale로 상쇄하고, Y축 offsetY는 PositionAndScaleCentered와
    // 동일한 이유로 상쇄하지 않는다(몬스터 스케일에 비례해 머리 위로 떠 있어야 함).
    private static void PositionAndScaleLeftAligned(Transform t, Sprite sprite, float targetWidth, float targetHeight,
        float widthRatio, float targetLeftX, float targetCenterY, Vector2 inverseParentScale)
    {
        if (sprite == null) return;

        Bounds b = sprite.bounds;
        float nativeW = b.size.x > 0f ? b.size.x : 1f;
        float nativeH = b.size.y > 0f ? b.size.y : 1f;

        float baseScaleX = (targetWidth / nativeW) * widthRatio;
        float baseScaleY = targetHeight / nativeH;

        t.localScale = new Vector3(baseScaleX * inverseParentScale.x, baseScaleY * inverseParentScale.y, 1f);
        t.localPosition = new Vector3(
            (targetLeftX - b.min.x * baseScaleX) * inverseParentScale.x,
            targetCenterY - b.center.y * baseScaleY * inverseParentScale.y,
            0f);
    }
}
