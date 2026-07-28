/// <summary>무기 종류 (최대 6종 동시 보유).</summary>
public enum WeaponType
{
    MagicMissile,
    Whip,
    Bible,
    Garlic,
    Knife,
    Lightning
}

/// <summary>패시브 아이템 종류 (최대 6종 동시 보유).</summary>
public enum PassiveType
{
    Spinach,        // 데미지 증가
    Armor,          // 피해 감소
    Wings,          // 이동속도 증가
    EmptyTome,      // 쿨다운 감소 (공허의 서)
    Candelabrador,  // 범위 증가 (촛대)
    Magnet          // 픽업 반경 증가
}

/// <summary>경험치 젬 등급.</summary>
public enum GemGrade
{
    Small,
    Medium,
    Large,
    Huge
}

/// <summary>적 등급.</summary>
public enum EnemyTier
{
    Basic,
    Elite,
    Boss,
    Reaper
}
