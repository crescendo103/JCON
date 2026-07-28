using UnityEngine;

/// <summary>무기 1개 레벨의 스탯. extra는 무기별로 의미가 다르다 (예: Bible=회전속도 deg/s).</summary>
[System.Serializable]
public class WeaponLevelStats
{
    public float damage = 10f;
    public float cooldown = 1f;
    public int projectileCount = 1;
    public float area = 1f;
    public int pierce = 0;
    public float extra = 0f;
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "VampireSurvivor/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public WeaponType type;
    public Sprite icon;
    public Color placeholderColor = Color.white;
    [TextArea] public string description;
    public int maxLevel = 8;
    public WeaponLevelStats[] levels;

    [Tooltip("MagicMissile/Knife/Lightning 등 투사체/이펙트 프리팹. Whip/Bible/Garlic은 비워둘 수 있음.")]
    public GameObject projectilePrefab;
}
