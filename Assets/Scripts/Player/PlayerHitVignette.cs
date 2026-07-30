using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 피격 순간 화면 가장자리를 빨갛게 훅 물들이고 부드럽게 걷어내는 연출.
// 같은 오브젝트의 Volume(isGlobal)에 붙은 프로파일의 Vignette를 코드로 직접 흔든다.
[RequireComponent(typeof(Volume))]
public class PlayerHitVignette : MonoBehaviour
{
    [Header("연출 값")]
    public Color hitColor = new Color(0.85f, 0f, 0f, 1f);
    [Tooltip("플래시 최대 세기. 1에 가까울수록 화면이 거의 다 덮인다")]
    public float maxIntensity = 0.75f;
    [Tooltip("낮을수록 가장자리 경계가 또렷해져서 더 강하게 보인다")]
    public float smoothness = 0.25f;
    public bool rounded = true;
    [Tooltip("0 -> 최대까지 차오르는 시간")]
    public float attackTime = 0.08f;
    [Tooltip("최대 세기를 유지하는 시간")]
    public float holdTime = 0.1f;
    [Tooltip("최대 -> 0으로 걷히는 시간")]
    public float fadeTime = 0.4f;

    private Vignette vignette;
    private Coroutine flashRoutine;

    private void Awake()
    {
        // volume.profile은 sharedProfile의 런타임 복제본을 돌려주므로 값을 바꿔도
        // .asset이 더럽혀지지 않는다. sharedProfile을 직접 만지면 에디터에서 영구 변경된다.
        var profile = GetComponent<Volume>().profile;
        if (!profile.TryGet(out vignette)) vignette = profile.Add<Vignette>(true);

        vignette.active = true;
        vignette.color.overrideState = true;
        vignette.intensity.overrideState = true;
        vignette.smoothness.overrideState = true;
        vignette.rounded.overrideState = true;
        vignette.color.value = hitColor;
        vignette.smoothness.value = smoothness;
        vignette.rounded.value = rounded;
        vignette.intensity.value = 0f;   // 평소엔 완전히 꺼둔다
    }

    // 연속 피격 시 이전 플래시를 끊고 처음부터 다시 시작한다.
    public void PlayHitFlash()
    {
        if (vignette == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        vignette.color.value = hitColor;

        // 사망 시 결과 캔버스가 뜨며 timeScale이 0이 되어도 연출이 멈추지 않도록 unscaled 시간을 쓴다.
        float up = Mathf.Max(attackTime, 0.0001f);
        for (float t = 0f; t < up; t += Time.unscaledDeltaTime)
        {
            vignette.intensity.value = Mathf.Lerp(0f, maxIntensity, t / up);
            yield return null;
        }
        vignette.intensity.value = maxIntensity;

        // 최대 세기에서 잠깐 멈춰줘야 눈에 확 띈다 (바로 페이드되면 흐릿하게 스쳐지나가는 느낌만 남는다).
        float hold = Mathf.Max(holdTime, 0f);
        for (float t = 0f; t < hold; t += Time.unscaledDeltaTime) yield return null;

        float down = Mathf.Max(fadeTime, 0.0001f);
        for (float t = 0f; t < down; t += Time.unscaledDeltaTime)
        {
            vignette.intensity.value = Mathf.Lerp(maxIntensity, 0f, t / down);
            yield return null;
        }

        vignette.intensity.value = 0f;
        flashRoutine = null;
    }

    // 비활성/파괴 시 비네트가 화면에 남지 않게 되돌린다.
    private void OnDisable()
    {
        flashRoutine = null;
        if (vignette != null) vignette.intensity.value = 0f;
    }
}
