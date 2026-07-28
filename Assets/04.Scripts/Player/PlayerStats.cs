using UnityEngine;

/// <summary>패시브 아이템이 수정하는 플레이어 스탯 허브. 각 값은 "누적 절대 배율"로 세팅된다.</summary>
public class PlayerStats : MonoBehaviour
{
    public float baseMoveSpeed = 4f;
    public float basePickupRadius = 1.5f;

    public float moveSpeedMultiplier = 1f;
    public float pickupRadiusMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float cooldownMultiplier = 1f;
    public float areaMultiplier = 1f;

    public float MoveSpeed => baseMoveSpeed * moveSpeedMultiplier;
    public float PickupRadius => basePickupRadius * pickupRadiusMultiplier;

    // 패시브 레벨표 값(%)은 누적 총량이므로, 레벨업 때마다 절대값으로 덮어쓴다 (중복 가산 방지).
    public void SetDamageBonusPercent(float percent) => damageMultiplier = 1f + percent / 100f;
    public void SetSpeedBonusPercent(float percent) => moveSpeedMultiplier = 1f + percent / 100f;
    public void SetCooldownReductionPercent(float percent) => cooldownMultiplier = Mathf.Max(0.2f, 1f - percent / 100f);
    public void SetAreaBonusPercent(float percent) => areaMultiplier = 1f + percent / 100f;
    public void SetPickupBonusPercent(float percent) => pickupRadiusMultiplier = 1f + percent / 100f;
}
