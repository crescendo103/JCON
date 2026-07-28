using System.Collections.Generic;
using UnityEngine;

/// <summary>현재 보유 상태를 기반으로 유효한 레벨업 카드 후보를 생성한다.</summary>
public class UpgradePool : MonoBehaviour
{
    public WeaponData[] allWeapons;
    public PassiveData[] allPassives;

    public List<UpgradeChoice> GetChoices(int count)
    {
        var inv = GameManager.Instance.Weapons;
        var candidates = new List<UpgradeChoice>();

        foreach (var w in allWeapons)
        {
            if (w == null) continue;
            if (inv.HasWeapon(w.type))
            {
                int lvl = inv.GetWeaponLevel(w.type);
                if (lvl < w.maxLevel) candidates.Add(UpgradeChoice.WeaponLevelUp(w, lvl));
            }
            else if (inv.WeaponCount < WeaponInventory.MaxWeapons)
            {
                candidates.Add(UpgradeChoice.NewWeapon(w));
            }
        }

        foreach (var p in allPassives)
        {
            if (p == null) continue;
            if (inv.HasPassive(p.type))
            {
                int lvl = inv.GetPassiveLevel(p.type);
                if (lvl < p.maxLevel) candidates.Add(UpgradeChoice.PassiveLevelUp(p, lvl));
            }
            else if (inv.PassiveCount < WeaponInventory.MaxPassives)
            {
                candidates.Add(UpgradeChoice.NewPassive(p));
            }
        }

        Shuffle(candidates);

        if (candidates.Count > count) candidates.RemoveRange(count, candidates.Count - count);
        return candidates;
    }

    static void Shuffle(List<UpgradeChoice> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
