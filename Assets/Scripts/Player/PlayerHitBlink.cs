using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 피격 순간 캐릭터(Model 하위 스프라이트 전부 + 장착 무기 비주얼)를 잠깐 켜짐/꺼짐으로 깜빡이는 연출.
// PlayerHitVignette와 같은 패턴(연출 값 필드 + 재시작 가능한 코루틴 + OnDisable 복원)을 따른다.
public class PlayerHitBlink : MonoBehaviour
{
    [Header("연출 값")]
    [Tooltip("깜빡임이 지속되는 총 시간(초)")]
    public float blinkDuration = 0.4f;
    [Tooltip("보이기/숨기기가 한 번 뒤집히는 간격(초). 작을수록 빠르게 깜빡인다")]
    public float blinkInterval = 0.06f;
    [Tooltip("숨겨진 순간의 알파값. 0이면 완전히 사라지고, 0.3 정도면 반투명하게만 흐려진다")]
    public float hiddenAlpha = 0f;

    private Transform model;
    private Transform weaponSocket;
    private Coroutine blinkRoutine;
    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    private readonly List<float> originalAlphas = new List<float>();

    private void Awake()
    {
        model = transform.Find("Model");
        weaponSocket = transform.Find("WeaponMuzzle");
    }

    // 연속 피격 시 이전 깜빡임을 끊고 처음부터 다시 시작한다.
    public void PlayHitBlink()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            RestoreAlphas(); // 숨겨진 채로 멈춰 있던 렌더러를 원래 알파로 되돌린 뒤 다시 시작
        }

        blinkRoutine = StartCoroutine(Blink());
    }

    // 장착 무기 비주얼은 무기를 바꿀 때마다 WeaponMuzzle 아래에 새로 스폰/파괴되므로
    // 캐시해두지 않고 매번 다시 수집한다.
    private void CollectRenderers()
    {
        renderers.Clear();
        originalAlphas.Clear();

        if (model != null) renderers.AddRange(model.GetComponentsInChildren<SpriteRenderer>(true));
        if (weaponSocket != null) renderers.AddRange(weaponSocket.GetComponentsInChildren<SpriteRenderer>(true));

        foreach (var r in renderers)
        {
            originalAlphas.Add(r.color.a);
        }
    }

    private void RestoreAlphas()
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] == null) continue; // 깜빡이는 도중 무기 교체 등으로 파괴됐을 수 있다
            Color c = renderers[i].color;
            c.a = originalAlphas[i];
            renderers[i].color = c;
        }
    }

    private IEnumerator Blink()
    {
        CollectRenderers();

        // 사망 시 Time.timeScale이 0이 되어도 깜빡임이 알파 0인 채로 멈추지 않도록 unscaled 시간을 쓴다
        // (PlayerHitVignette.Flash()와 동일한 이유).
        float elapsed = 0f;
        bool hidden = false;

        while (elapsed < blinkDuration)
        {
            hidden = !hidden;
            SetAlpha(hidden ? hiddenAlpha : -1f); // -1f는 "원래 알파로 복원" 표시

            float interval = Mathf.Max(blinkInterval, 0.0001f);
            for (float t = 0f; t < interval && elapsed < blinkDuration; t += Time.unscaledDeltaTime)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        RestoreAlphas();
        blinkRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] == null) continue;
            Color c = renderers[i].color;
            c.a = alpha >= 0f ? alpha : originalAlphas[i];
            renderers[i].color = c;
        }
    }

    // 비활성/파괴 시 캐릭터가 투명한 채로 남지 않게 되돌린다.
    private void OnDisable()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
        RestoreAlphas();
    }
}
