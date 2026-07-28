using UnityEngine;

public enum UpgradeKind
{
    NewWeapon,
    WeaponLevelUp,
    NewPassive,
    PassiveLevelUp
}

/// <summary>레벨업 카드 1장에 대응하는 런타임 데이터 (ScriptableObject가 아닌 일반 클래스 - 매번 새로 생성/조합됨).</summary>
public class UpgradeChoice
{
    public UpgradeKind kind;
    public WeaponData weapon;
    public PassiveData passive;
    public int currentLevel;

    public string Title => weapon != null ? weapon.weaponName : (passive != null ? passive.passiveName : "?");
    public Sprite Icon => weapon != null ? weapon.icon : (passive != null ? passive.icon : null);
    public Color IconColor => weapon != null ? weapon.placeholderColor : (passive != null ? passive.placeholderColor : Color.white);
    public string Description => weapon != null ? weapon.description : (passive != null ? passive.description : string.Empty);

    public string LevelText =>
        (kind == UpgradeKind.NewWeapon || kind == UpgradeKind.NewPassive)
            ? "NEW"
            : $"Lv {currentLevel} -> {currentLevel + 1}";

    public static UpgradeChoice NewWeapon(WeaponData w) => new UpgradeChoice { kind = UpgradeKind.NewWeapon, weapon = w, currentLevel = 0 };
    public static UpgradeChoice WeaponLevelUp(WeaponData w, int lvl) => new UpgradeChoice { kind = UpgradeKind.WeaponLevelUp, weapon = w, currentLevel = lvl };
    public static UpgradeChoice NewPassive(PassiveData p) => new UpgradeChoice { kind = UpgradeKind.NewPassive, passive = p, currentLevel = 0 };
    public static UpgradeChoice PassiveLevelUp(PassiveData p, int lvl) => new UpgradeChoice { kind = UpgradeKind.PassiveLevelUp, passive = p, currentLevel = lvl };
}
