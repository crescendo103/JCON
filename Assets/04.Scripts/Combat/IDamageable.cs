using UnityEngine;

/// <summary>데미지를 받을 수 있는 대상 공통 인터페이스 (Enemy, PlayerHealth가 구현).</summary>
public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(float amount, Vector2 sourcePosition);
}
