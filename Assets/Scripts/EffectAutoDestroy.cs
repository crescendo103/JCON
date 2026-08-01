using UnityEngine;

// 애니메이션이 한 바퀴 끝나면(normalizedTime >= 1) 자기 자신을 파괴한다.
// 루프 애니메이션이어도 normalizedTime은 1을 넘어 계속 증가하므로 그대로 동작한다.
[RequireComponent(typeof(Animator))]
public class EffectAutoDestroy : MonoBehaviour
{
    private Animator anim;
    private Transform followTarget;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    // target을 따로 부모로 삼지 않고 위치만 매 프레임 따라간다. 무기 비주얼처럼 스케일이 축소된
    // 오브젝트 밑에 실제로 부모로 넣으면 이펙트 크기까지 같이 줄어들어버리기 때문.
    public void Follow(Transform target)
    {
        followTarget = target;
    }

    void Update()
    {
        if (followTarget != null) transform.position = followTarget.position;

        if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
