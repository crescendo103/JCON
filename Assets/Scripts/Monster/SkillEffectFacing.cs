using UnityEngine;

// 8방향 Blend Tree를 쓰는 이펙트 프리팹에 붙어, 스폰한 쪽이 알려주는 방향을 Animator의
// FaceX/FaceY에 반영한다. 몬스터의 lastFacingDir를 그대로 넘겨받아 블렌드 방향을 맞추는 용도.
public class SkillEffectFacing : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SetFacing(Vector2 dir)
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) return;

        animator.SetFloat(MonsterController.ParamFaceX, dir.x);
        animator.SetFloat(MonsterController.ParamFaceY, dir.y);
    }
}
