using System.Collections.Generic;
using UnityEngine;

/// <summary>현재 활성화된 모든 적을 등록/추적. 무기의 타겟팅(최근접/랜덤)에 사용된다.</summary>
public static class EnemyTracker
{
    public static readonly List<Enemy> Active = new List<Enemy>();

    public static void Register(Enemy e)
    {
        if (!Active.Contains(e)) Active.Add(e);
    }

    public static void Unregister(Enemy e)
    {
        Active.Remove(e);
    }

    public static Enemy FindNearest(Vector2 origin)
    {
        Enemy best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < Active.Count; i++)
        {
            var e = Active[i];
            if (e == null || !e.isActiveAndEnabled) continue;

            float d = ((Vector2)e.transform.position - origin).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }
        return best;
    }

    public static List<Enemy> FindNearestMultiple(Vector2 origin, int count)
    {
        var list = new List<Enemy>(Active);
        list.RemoveAll(e => e == null || !e.isActiveAndEnabled);
        list.Sort((a, b) =>
            ((Vector2)a.transform.position - origin).sqrMagnitude
            .CompareTo(((Vector2)b.transform.position - origin).sqrMagnitude));

        if (list.Count > count) list.RemoveRange(count, list.Count - count);
        return list;
    }

    public static List<Enemy> FindRandomMultiple(int count)
    {
        var list = new List<Enemy>(Active);
        list.RemoveAll(e => e == null || !e.isActiveAndEnabled);

        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }

        if (list.Count > count) list.RemoveRange(count, list.Count - count);
        return list;
    }
}
