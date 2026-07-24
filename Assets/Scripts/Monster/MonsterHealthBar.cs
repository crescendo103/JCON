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

        PositionAndScaleCentered(bgTransform, backgroundSprite, width, height, 0f, offsetY);
        ApplyFillRatio(lastRatio);
    }

    // current/max 비율만큼 채움을 갱신하고 바를 켠 뒤, visibleDuration 후 다시 숨긴다.
    // 연속으로 호출되면(연속 피격) 숨김 타이머가 매번 새로 시작된다.
    public void Show(int current, int max)
    {
        BuildBarIfNeeded();

        float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        ApplyFillRatio(ratio);

        bgRenderer.enabled = true;
        fillRenderer.enabled = true;

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private void ApplyFillRatio(float ratio)
    {
        lastRatio = ratio;
        float barLeft = -width * 0.5f;
        PositionAndScaleLeftAligned(fillTransform, fillRenderer.sprite, width, height, ratio, barLeft, offsetY);
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
    private static void PositionAndScaleCentered(Transform t, Sprite sprite, float targetWidth, float targetHeight,
        float targetCenterX, float targetCenterY)
    {
        if (sprite == null) return;

        Bounds b = sprite.bounds;
        float nativeW = b.size.x > 0f ? b.size.x : 1f;
        float nativeH = b.size.y > 0f ? b.size.y : 1f;

        float scaleX = targetWidth / nativeW;
        float scaleY = targetHeight / nativeH;

        t.localScale = new Vector3(scaleX, scaleY, 1f);
        t.localPosition = new Vector3(targetCenterX - b.center.x * scaleX, targetCenterY - b.center.y * scaleY, 0f);
    }

    // sprite의 실제 피벗/크기와 무관하게, 왼쪽 끝이 targetLeftX에 고정된 채로
    // widthRatio(0~1)만큼만 targetWidth x targetHeight 크기 안에서 오른쪽으로 채워지도록 계산한다.
    private static void PositionAndScaleLeftAligned(Transform t, Sprite sprite, float targetWidth, float targetHeight,
        float widthRatio, float targetLeftX, float targetCenterY)
    {
        if (sprite == null) return;

        Bounds b = sprite.bounds;
        float nativeW = b.size.x > 0f ? b.size.x : 1f;
        float nativeH = b.size.y > 0f ? b.size.y : 1f;

        float scaleX = (targetWidth / nativeW) * widthRatio;
        float scaleY = targetHeight / nativeH;

        t.localScale = new Vector3(scaleX, scaleY, 1f);
        t.localPosition = new Vector3(targetLeftX - b.min.x * scaleX, targetCenterY - b.center.y * scaleY, 0f);
    }
}
