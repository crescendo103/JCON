using UnityEngine;

// 스킬 이펙트 프리팹에 동적으로 붙어 Player 태그를 감지하면 데미지를 전달한다.
// 이펙트 프리팹에 Collider2D(Is Trigger 체크)가 있어야 감지된다.
public class SkillEffectDamage : MonoBehaviour
{
    public int damage;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        other.GetComponent<PlayerController>()?.Hit(damage);
    }
}
