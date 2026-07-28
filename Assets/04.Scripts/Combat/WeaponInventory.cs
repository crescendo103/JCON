using System.Collections.Generic;
using UnityEngine;

/// <summary>플레이어가 보유한 무기/패시브를 관리. 무기는 자식 GameObject로 동적 생성되어 자동 Tick된다.</summary>
public class WeaponInventory : MonoBehaviour
{
    public const int MaxWeapons = 6;
    public const int MaxPassives = 6;

    public class PassiveEntry
    {
        public PassiveData data;
        public int level;
    }

    readonly Dictionary<WeaponType, WeaponBase> activeWeapons = new Dictionary<WeaponType, WeaponBase>();
    readonly Dictionary<PassiveType, PassiveEntry> activePassives = new Dictionary<PassiveType, PassiveEntry>();

    public IReadOnlyDictionary<WeaponType, WeaponBase> ActiveWeapons => activeWeapons;
    public IReadOnlyDictionary<PassiveType, PassiveEntry> ActivePassives => activePassives;

    public bool HasWeapon(WeaponType t) => activeWeapons.ContainsKey(t);
    public bool HasPassive(PassiveType t) => activePassives.ContainsKey(t);
    public int WeaponCount => activeWeapons.Count;
    public int PassiveCount => activePassives.Count;
    public int GetWeaponLevel(WeaponType t) => activeWeapons.TryGetValue(t, out var w) ? w.Level : 0;
    public int GetPassiveLevel(PassiveType t) => activePassives.TryGetValue(t, out var e) ? e.level : 0;

    public void AddWeapon(WeaponData data)
    {
        if (data == null || activeWeapons.ContainsKey(data.type) || activeWeapons.Count >= MaxWeapons) return;

        var go = new GameObject($"Weapon_{data.type}");
        go.transform.SetParent(transform, false);

        WeaponBase comp = null;
        switch (data.type)
        {
            case WeaponType.MagicMissile: comp = go.AddComponent<MagicMissileWeapon>(); break;
            case WeaponType.Whip: comp = go.AddComponent<WhipWeapon>(); break;
            case WeaponType.Bible: comp = go.AddComponent<BibleWeapon>(); break;
            case WeaponType.Garlic: comp = go.AddComponent<GarlicWeapon>(); break;
            case WeaponType.Knife: comp = go.AddComponent<KnifeWeapon>(); break;
            case WeaponType.Lightning: comp = go.AddComponent<LightningWeapon>(); break;
        }

        if (comp == null)
        {
            Destroy(go);
            return;
        }

        comp.Initialize(data);
        activeWeapons[data.type] = comp;
    }

    public void LevelUpWeapon(WeaponType t)
    {
        if (activeWeapons.TryGetValue(t, out var w))
        {
            w.SetLevel(w.Level + 1);
        }
    }

    public void AddOrLevelUpPassive(PassiveData data)
    {
        if (data == null) return;

        bool exists = activePassives.TryGetValue(data.type, out var entry);
        if (!exists && activePassives.Count >= MaxPassives) return;

        int newLevel = exists ? entry.level + 1 : 1;
        activePassives[data.type] = new PassiveEntry { data = data, level = newLevel };
        ApplyPassiveEffect(data, newLevel);
    }

    void ApplyPassiveEffect(PassiveData data, int level)
    {
        if (data.levels == null || data.levels.Length == 0) return;

        float value = data.levels[Mathf.Clamp(level - 1, 0, data.levels.Length - 1)];
        var stats = GameManager.Instance.PlayerStats;
        var health = GameManager.Instance.PlayerHealth;

        switch (data.type)
        {
            case PassiveType.Spinach: stats.SetDamageBonusPercent(value); break;
            case PassiveType.Armor: health.SetArmorReductionPercent(value); break;
            case PassiveType.Wings: stats.SetSpeedBonusPercent(value); break;
            case PassiveType.EmptyTome: stats.SetCooldownReductionPercent(value); break;
            case PassiveType.Candelabrador: stats.SetAreaBonusPercent(value); break;
            case PassiveType.Magnet: stats.SetPickupBonusPercent(value); break;
        }
    }
}
