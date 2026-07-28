using UnityEngine;

/// <summary>번개 낙뢰 등 순간 이펙트용: 활성화 후 일정 시간 뒤 풀로 자동 반환.</summary>
public class TimedReturn : MonoBehaviour
{
    float timer;
    bool active;

    public void Activate(float duration)
    {
        timer = duration;
        active = true;
    }

    void OnEnable()
    {
        active = false;
    }

    void Update()
    {
        if (!active) return;
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            active = false;
            if (PoolManager.Instance != null) PoolManager.Instance.Return(gameObject);
            else Destroy(gameObject);
        }
    }
}
