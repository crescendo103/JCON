using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 보스 몬스터 전용 체력바. 몬스터 머리 위에 붙는 MonsterHealthBar와 달리, 화면 하단에 화면 가로
// 크기만큼 늘어난 UI 바를 띄운다. 킹좀비처럼 보스 프리팹에 이 컴포넌트를 직접 붙여두면
// MonsterHealth가 Awake()에서 이걸 감지해 MonsterHealthBar 대신 이 바를 사용한다.
// 피격 시 채움 바가 flashColor로 번쩍이는 연출은 원래 MonsterHealthBar에 있던 것을 그대로 옮겨왔다
// (일반 몬스터에는 더 이상 없음, 보스 전용).
public class BossHealthBar : MonoBehaviour
{
    [Header("스프라이트")]
    // MonsterHealthBar와 동일하게, 지정한 이미지를 색 보정 없이 그대로 사용한다. 두 필드를 비워두면
    // 아무것도 그려지지 않는다(단색 채움으로 대체하지 않음).
    public Sprite backgroundSprite;
    public Sprite fillSprite;

    [Header("배치")]
    public float bottomMargin = 24f;
    public float barHeight = 28f;
    public float sideMargin = 0f;

    [Header("채움 바 안쪽 여백")]
    // 채움 바를 배경보다 상하좌우로 이만큼씩 안쪽으로 줄여서 배경 테두리가 보이게 한다.
    public float fillPaddingHorizontal = 4f;
    public float fillPaddingVertical = 4f;

    [Header("피격 플래시")]
    // 피격(Show 호출) 시 채움 바가 이 색으로 번쩍였다가 원래 색으로 돌아온다.
    public Color flashColor = Color.white;
    public float flashDuration = 0.15f;

    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");

    private bool built;
    private GameObject canvasGO;
    private RectTransform fillRect;
    private Image fillImage;
    private Material fillMaterial;
    private Coroutine flashRoutine;

    void Awake()
    {
        BuildBarIfNeeded();
    }

    void OnDestroy()
    {
        if (canvasGO != null) Destroy(canvasGO);
    }

    private void BuildBarIfNeeded()
    {
        if (built) return;
        built = true;

        canvasGO = new GameObject("BossHealthBarCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.sprite = backgroundSprite;
        // 9-slice: 스프라이트에 Border가 설정돼 있으면 테두리/모서리는 원래 크기를 유지하고
        // 가운데만 늘어난다(Sprite Editor에서 Border를 지정해야 실제로 적용됨).
        bgImage.type = Image.Type.Sliced;
        bgImage.color = Color.white;

        var bgRect = bgImage.rectTransform;
        bgRect.anchorMin = new Vector2(0f, 0f);
        bgRect.anchorMax = new Vector2(1f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.anchoredPosition = new Vector2(0f, bottomMargin);
        bgRect.sizeDelta = new Vector2(-sideMargin * 2f, barHeight);

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        fillImage = fillGO.AddComponent<Image>();
        fillImage.sprite = fillSprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = Color.white;

        fillRect = fillImage.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        // offsetMin/Max는 각 앵커 지점에서부터의 고정 픽셀 여백이라, 이후 Show()에서 anchorMax.x만
        // 비율로 바꿔도 이 여백은 그대로 유지된다(채움 오른쪽 끝이 항상 비율 지점보다 안쪽에 그려짐).
        fillRect.offsetMin = new Vector2(fillPaddingHorizontal, fillPaddingVertical);
        fillRect.offsetMax = new Vector2(-fillPaddingHorizontal, -fillPaddingVertical);

        Shader flashShader = Shader.Find("Custom/HealthBarFlash");
        if (flashShader != null)
        {
            fillMaterial = new Material(flashShader);
            fillMaterial.SetColor(FlashColorID, flashColor);
            fillMaterial.SetFloat(FlashAmountID, 0f);
            fillImage.material = fillMaterial;
        }

        canvasGO.SetActive(false);
    }

    // current/max 비율만큼 채움을 갱신하고 바를 켠 뒤, 피격 플래시를 재생한다.
    // current가 0 이하(사망)이면 바를 즉시 숨긴다.
    public void Show(int current, int max)
    {
        BuildBarIfNeeded();

        if (current <= 0)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = null;
            canvasGO.SetActive(false);
            return;
        }

        canvasGO.SetActive(true);

        float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        fillRect.anchorMax = new Vector2(ratio, 1f);

        if (fillMaterial != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }
    }

    // 채움 바를 flashColor로 즉시 전환한 뒤 flashDuration에 걸쳐 원래 색으로 되돌린다.
    private IEnumerator FlashRoutine()
    {
        fillMaterial.SetColor(FlashColorID, flashColor);
        fillMaterial.SetFloat(FlashAmountID, 1f);

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float amount = 1f - Mathf.Clamp01(elapsed / flashDuration);
            fillMaterial.SetFloat(FlashAmountID, amount);
            yield return null;
        }

        fillMaterial.SetFloat(FlashAmountID, 0f);
        flashRoutine = null;
    }
}
