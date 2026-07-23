using UnityEngine;

// 스킬 이펙트 프리팹에 동적으로 붙어 Player 태그를 감지하면 데미지를 전달한다.
// 이펙트 프리팹에 Collider2D(Is Trigger 체크)가 있어야 감지된다.
// 플레이어 체력 시스템이 아직 없어 지금은 로그만 남기고, 나중에 체력 시스템이 생기면 여기서 연결한다.
public class SkillEffectDamage : MonoBehaviour
{
    public int damage;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"Player hit for {damage}");
        // TODO: 플레이어 체력 시스템 연결. 예: other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
    }
}
