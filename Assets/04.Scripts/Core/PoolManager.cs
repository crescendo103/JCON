using System.Collections.Generic;
using UnityEngine;

/// <summary>풀에서 나온 오브젝트에 원본 프리팹 참조를 남겨, 반환 시 어느 풀로 돌아가야 하는지 알 수 있게 한다.</summary>
public class PooledObject : MonoBehaviour
{
    public GameObject sourcePrefab;
}

/// <summary>
/// 프리팹별 오브젝트 풀을 관리하는 단일 매니저.
/// 적/투사체/픽업 모두 이 매니저를 통해 Get/Return 하며, 런타임 Instantiate/Destroy를 하지 않는다.
/// (기술 아키텍처 문서의 EnemyPool/ProjectilePool/PickupPool 역할을 하나의 범용 풀로 통합 구현)
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    readonly Dictionary<GameObject, Transform> poolRoots = new Dictionary<GameObject, Transform>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
            var root = new GameObject($"Pool_{prefab.name}").transform;
            root.SetParent(transform, false);
            poolRoots[prefab] = root;
        }

        GameObject obj;
        while (queue.Count > 0)
        {
            obj = queue.Dequeue();
            if (obj == null) continue; // 씬 전환 등으로 파괴된 경우 스킵
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        obj = Instantiate(prefab, position, rotation, poolRoots[prefab]);
        var link = obj.GetComponent<PooledObject>();
        if (link == null) link = obj.AddComponent<PooledObject>();
        link.sourcePrefab = prefab;
        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null) return;
        var link = obj.GetComponent<PooledObject>();
        if (link == null || link.sourcePrefab == null || !poolRoots.ContainsKey(link.sourcePrefab))
        {
            Destroy(obj);
            return;
        }
        obj.SetActive(false);
        obj.transform.SetParent(poolRoots[link.sourcePrefab], false);
        pools[link.sourcePrefab].Enqueue(obj);
    }
}
