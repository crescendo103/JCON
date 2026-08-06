using UnityEngine;

// 8방향 Blend Tree를 쓰는 이펙트 프리팹에 붙어, 스폰한 쪽이 알려주는 방향을 Animator의
// FaceX/FaceY에 반영한다. 몬스터의 lastFacingDir를 그대로 넘겨받아 블렌드 방향을 맞추는 용도.
public class SkillEffectFacing : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetFacing(Vector2 dir)
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) return;

        animator.SetFloat(MonsterController.ParamFaceX, dir.x);
        animator.SetFloat(MonsterController.ParamFaceY, dir.y);

        // "오른쪽" 방향용 원본 스프라이트 프레임이 없어서(VomitEffect_Controller의 Blend Tree가
        // right 자리에도 motion_left.anim을 그대로 재사용하도록 되어 있다), 오른쪽을 향할 때는
        // 왼쪽 프레임을 좌우 반전해서 오른쪽처럼 보이게 한다.
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.flipX = dir.x > 0f;
    }
}
