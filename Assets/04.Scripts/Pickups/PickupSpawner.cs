using UnityEngine;

/// <summary>적 처치 시 등급에 맞는 경험치 젬 프리팹을 풀에서 꺼내 배치한다.</summary>
public class PickupSpawner : MonoBehaviour
{
    public static PickupSpawner Instance { get; private set; }

    public GameObject smallGemPrefab;
    public GameObject mediumGemPrefab;
    public GameObject largeGemPrefab;
    public GameObject hugeGemPrefab;

    public int smallValue = 1;
    public int mediumValue = 5;
    public int largeValue = 25;
    public int hugeValue = 100;

    void Awake()
    {
        Instance = this;
    }

    public void Spawn(GemGrade grade, Vector3 position)
    {
        GameObject prefab;
        int value;

        switch (grade)
        {
            case GemGrade.Medium: prefab = mediumGemPrefab; value = mediumValue; break;
            case GemGrade.Large: prefab = largeGemPrefab; value = largeValue; break;
            case GemGrade.Huge: prefab = hugeGemPrefab; value = hugeValue; break;
            default: prefab = smallGemPrefab; value = smallValue; break;
        }

        if (prefab == null) return;

        var go = PoolManager.Instance.Get(prefab, position, Quaternion.identity);
        var gem = go.GetComponent<XPGem>();
        gem.SetValue(value);
    }
}
