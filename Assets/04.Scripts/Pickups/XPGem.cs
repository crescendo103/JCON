using UnityEngine;

/// <summary>경험치 젬. 자석 반경에 들어오면 플레이어를 향해 날아가 흡수된다.</summary>
public class XPGem : MonoBehaviour
{
    public int xpValue = 1;
    public float flySpeed = 8f;

    Transform target;
    bool attracting;

    void OnEnable()
    {
        attracting = false;
        target = null;
    }

    public void SetValue(int value)
    {
        xpValue = value;
    }

    public void StartAttracting(Transform playerTransform)
    {
        attracting = true;
        target = playerTransform;
    }

    void Update()
    {
        if (!attracting || target == null) return;

        transform.position = Vector3.MoveTowards(transform.position, target.position, flySpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.15f)
        {
            LevelSystem.Instance?.AddExperience(xpValue);

            if (PoolManager.Instance != null) PoolManager.Instance.Return(gameObject);
            else Destroy(gameObject);
        }
    }
}
