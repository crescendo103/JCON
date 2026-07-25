using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 배너 Image와 그 자식(Text 등) 전체를 CanvasGroup.alpha로 묶어서 0→1로 페이드인했다가,
// visibleDuration만큼 보여준 뒤 다시 1→0으로 페이드아웃하고 GameObject 자체를 Destroy하는
// 일회성 배너 연출 컴포넌트. CanvasGroup을 쓰기 때문에 이 오브젝트의 Image뿐 아니라 자식으로
// 붙은 Text(TMP) 등 모든 Graphic이 같은 alpha로 함께 페이드된다(개별적으로 색을 맞출 필요 없음).
// 특정 몬스터/매니저에 종속되지 않는 재사용 컴포넌트이며, 빈 RectTransform에 붙이고
// bannerSprite만 지정하면(선택) 나머지는 알아서 처리한다.
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class BannerRevealUI : MonoBehaviour
{
    [Header("스프라이트")]
    public Sprite bannerSprite;

    [Header("페이드 인/아웃")]
    public float revealDuration = 0.6f;
    // 페이드인이 끝난 뒤 완전히 보이는 상태로 유지되는 시간(이 시간이 지나면 자동으로 페이드아웃 시작).
    public float visibleDuration = 2f;
    public float hideDuration = 0.6f;
    public bool useUnscaledTime = false;
    // 선형(0,0)-(1,1) 기본값이면 등속, 필요하면 이즈 아웃 등으로 바꿔서 느낌을 조절할 수 있다.
    public AnimationCurve easeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("재생 시점")]
    public bool playOnEnable = true;

    private CanvasGroup canvasGroup;
    private Coroutine lifecycleRoutine;

    void Awake()
    {
        var image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (bannerSprite != null)
        {
            image.sprite = bannerSprite;
            image.type = Image.Type.Sliced;
        }

        // PlayReveal()이 아직 호출되기 전(재생 지연, playOnEnable=false 등)에도 배너/자식 텍스트가
        // 기본 alpha 1(불투명)로 남아 뒤의 UI를 가리지 않도록, 시작 상태를 항상 투명으로 맞춰둔다.
        canvasGroup.alpha = 0f;
    }

    void OnEnable()
    {
        if (playOnEnable) PlayReveal();
    }

    // 0→1 페이드인 → visibleDuration 대기 → 1→0 페이드아웃 → Destroy(gameObject) 순으로 재생한다.
    // 재생 중 다시 호출되면 처음부터 다시 시작한다.
    public void PlayReveal(System.Action onComplete = null)
    {
        if (lifecycleRoutine != null) StopCoroutine(lifecycleRoutine);
        lifecycleRoutine = StartCoroutine(LifecycleRoutine(onComplete));
    }

    // 애니메이션 없이 즉시 완전히 드러난 상태로 만든다(자동 소멸 타이머는 시작하지 않음).
    public void ShowInstant()
    {
        if (lifecycleRoutine != null) StopCoroutine(lifecycleRoutine);
        lifecycleRoutine = null;
        canvasGroup.alpha = 1f;
    }

    // 재생 전 상태(완전히 투명)로 되돌린다.
    public void ResetHidden()
    {
        if (lifecycleRoutine != null) StopCoroutine(lifecycleRoutine);
        lifecycleRoutine = null;
        canvasGroup.alpha = 0f;
    }

    private IEnumerator LifecycleRoutine(System.Action onComplete)
    {
        yield return Fade(0f, 1f, revealDuration);

        float waited = 0f;
        while (waited < visibleDuration)
        {
            waited += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        yield return Fade(1f, 0f, hideDuration);

        lifecycleRoutine = null;
        onComplete?.Invoke();
        Destroy(gameObject);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        canvasGroup.alpha = from;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            canvasGroup.alpha = Mathf.Lerp(from, to, easeCurve.Evaluate(t));
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
