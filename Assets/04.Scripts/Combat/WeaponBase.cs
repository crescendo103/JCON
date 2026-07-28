using UnityEngine;

/// <summary>
/// 모든 자동 무기의 베이스. 쿨다운 타이머와 레벨/데이터 관리를 공통 처리하고,
/// 실제 발동 로직(Fire)만 하위 클래스가 구현한다. Bible/Garlic처럼 상시 지속형 무기는
/// Update를 오버라이드해 쿨다운 방식 대신 자체 로직을 사용한다.
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponData data;
    protected int level = 1;
    protected float cooldownTimer;

    public int Level => level;
    public WeaponData Data => data;

    protected Transform OwnerTransform => GameManager.Instance.Player.transform;
    protected PlayerStats OwnerStats => GameManager.Instance.PlayerStats;

    protected WeaponLevelStats Stats => data.levels[Mathf.Clamp(level - 1, 0, data.levels.Length - 1)];

    public void Initialize(WeaponData weaponData)
    {
        data = weaponData;
        level = 1;
        cooldownTimer = 0f;
        OnInitialize();
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Clamp(newLevel, 1, data.maxLevel);
        OnLevelChanged();
    }

    protected virtual void OnInitialize() { }
    protected virtual void OnLevelChanged() { }

    protected virtual void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            float cd = Stats.cooldown * (OwnerStats != null ? OwnerStats.cooldownMultiplier : 1f);
            cooldownTimer = Mathf.Max(0.05f, cd);
            Fire();
        }
    }

    protected abstract void Fire();

    public float ComputeDamage() => Stats.damage * (OwnerStats != null ? OwnerStats.damageMultiplier : 1f);
    public float ComputeAreaMultiplier() => (OwnerStats != null ? OwnerStats.areaMultiplier : 1f);
}
