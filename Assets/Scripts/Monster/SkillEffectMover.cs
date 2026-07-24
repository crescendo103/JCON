using System.Collections;
using UnityEngine;

// 스킬 이펙트가 총알처럼 날아가는 이동+파괴를 이펙트 자기 자신이 맡는다.
// 예전에는 이 코루틴을 발사한 몬스터(MonsterController)에서 돌렸는데, 몬스터가 이동 도중 죽으면
// Die()의 StopAllCoroutines()에 걸려 코루틴이 끊기고 이펙트가 파괴되지 못한 채 남는 문제가 있었다.
// 이펙트 자신에게 붙이면 발사자의 생존 여부와 무관하게 이동/파괴가 끝까지 진행된다.
public class SkillEffectMover : MonoBehaviour
{
    // 이동 로직이 어떤 이유로든 끝나지 못했을 때를 대비한 최대 생존 시간(안전장치).
    private const float MaxLifetime = 5f;

    public void Launch(Vector3 start, Vector3 end, float duration)
    {
        Destroy(gameObject, MaxLifetime);
        StartCoroutine(MoveRoutine(start, end, duration));
    }

    private IEnumerator MoveRoutine(Vector3 start, Vector3 end, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}
