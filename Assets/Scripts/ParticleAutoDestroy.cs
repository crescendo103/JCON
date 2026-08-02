using UnityEngine;

// 파티클(과 그 자식들)이 전부 재생을 끝내면 자기 자신을 파괴한다. ParticleSystem 프리팹은
// EffectAutoDestroy가 요구하는 Animator가 없는 경우가 대부분이라 별도로 둔다.
public class ParticleAutoDestroy : MonoBehaviour
{
    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        // includeChildren: true라 자식 파티클 시스템까지 전부 끝나야 파괴된다.
        if (ps != null && !ps.IsAlive(true))
        {
            Destroy(gameObject);
        }
    }
}
