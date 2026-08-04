using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 원거리 무기(GameWeaponData, category == Ranged)의 위력 관련 수치를 한 화면에서 비교/편집하는 툴.
/// damage/cooldown 숫자만 봐서는 실제 위력을 알 수 없다 — 몬스터 피격 무적시간(damageType별로
/// 0.25~1.0초, MonsterData.cs 기본값)보다 쿨다운이 짧으면 일부 사격이 데미지 없이 그냥 낭비된다.
/// 이 창은 그 실효 DPS/낭비 여부를 그 자리에서 계산해서 보여줘, damage만 올렸다가 실제로는
/// 아무것도 안 바뀌는(과거 샷건이 그랬던) 실수를 막는다.
/// EnemyDataEditorWindow.cs(Assets/04.Scripts/Editor)와 동일한 관례(SerializedObject +
/// PropertyField로 즉시 편집, ApplyModifiedProperties 후 SetDirty)를 따른다.
/// </summary>
public class RangedWeaponBalanceWindow : EditorWindow
{
    // MonsterData.cs의 기본 KnockbackSetting 배열과 실제 몬스터 6종(.asset) 전부가 공통으로 쓰는 값.
    // 몬스터별로 이 값을 다르게 오버라이드하면 아래 실효 DPS 계산이 실제와 어긋날 수 있다(창에도 안내함).
    private const float LightInvincibility = 0.3f;
    private const float NormalInvincibility = 0.25f;
    private const float HeavyInvincibility = 1f;

    private List<GameWeaponData> weapons = new List<GameWeaponData>();
    private List<MonsterData> monsters = new List<MonsterData>();
    private GameWeaponData selected;
    private Vector2 scroll;

    [MenuItem("VampireSurvivor/원거리 무기 밸런스")]
    public static void Open()
    {
        var window = GetWindow<RangedWeaponBalanceWindow>("원거리 무기 밸런스");
        window.minSize = new Vector2(560, 420);
        window.RefreshList();
    }

    private void OnEnable()
    {
        RefreshList();
    }

    private void RefreshList()
    {
        weapons.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:GameWeaponData"))
        {
            var weapon = AssetDatabase.LoadAssetAtPath<GameWeaponData>(AssetDatabase.GUIDToAssetPath(guid));
            if (weapon != null && weapon.category == WeaponCategory.Ranged) weapons.Add(weapon);
        }
        weapons = weapons.OrderBy(w => w.name).ToList();

        monsters.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:MonsterData"))
        {
            var monster = AssetDatabase.LoadAssetAtPath<MonsterData>(AssetDatabase.GUIDToAssetPath(guid));
            if (monster != null) monsters.Add(monster);
        }
        monsters = monsters.OrderBy(m => m.maxHP).ToList();
    }

    private static float GetInvincibility(DamageType type)
    {
        switch (type)
        {
            case DamageType.Light: return LightInvincibility;
            case DamageType.Heavy: return HeavyInvincibility;
            default: return NormalInvincibility;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("원거리 무기 밸런스", EditorStyles.boldLabel);
        if (GUILayout.Button("새로고침", GUILayout.Width(70))) RefreshList();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "실효 DPS는 몬스터 피격 무적시간(damageType별 기본값 Light 0.3s / Normal 0.25s / Heavy 1.0s)을 " +
            "고려한 값입니다. 쿨다운이 이 값보다 짧으면 일부 사격이 데미지 없이 낭비됩니다(⚠로 표시). " +
            "몬스터마다 무적시간을 다르게 설정해두면 실제 결과는 여기 계산과 달라질 수 있습니다.",
            MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawTableHeader();
        foreach (var weapon in weapons)
        {
            if (weapon != null) DrawWeaponRow(weapon);
        }

        if (weapons.Count == 0)
        {
            EditorGUILayout.HelpBox("원거리(Ranged) GameWeaponData 에셋을 찾지 못했습니다.", MessageType.Warning);
        }

        EditorGUILayout.Space();
        if (selected != null) DrawDetailPanel(selected);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (GUILayout.Button("모두 저장")) AssetDatabase.SaveAssets();
    }

    private void DrawTableHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("무기", GUILayout.Width(90));
        GUILayout.Label("Damage", GUILayout.Width(55));
        GUILayout.Label("Cooldown", GUILayout.Width(55));
        GUILayout.Label("Max Ammo", GUILayout.Width(55));
        GUILayout.Label("실효 간격", GUILayout.Width(65));
        GUILayout.Label("실효 DPS", GUILayout.Width(65));
        GUILayout.Label("상태", GUILayout.Width(30));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawWeaponRow(GameWeaponData weapon)
    {
        var so = new SerializedObject(weapon);
        so.Update();

        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        bool isSelected = selected == weapon;
        if (GUILayout.Toggle(isSelected, weapon.name, "Button", GUILayout.Width(90)))
        {
            if (!isSelected) selected = weapon;
        }

        EditorGUILayout.PropertyField(so.FindProperty("damage"), GUIContent.none, GUILayout.Width(55));
        EditorGUILayout.PropertyField(so.FindProperty("cooldown"), GUIContent.none, GUILayout.Width(55));
        EditorGUILayout.PropertyField(so.FindProperty("maxAmmo"), GUIContent.none, GUILayout.Width(55));

        if (so.ApplyModifiedProperties()) EditorUtility.SetDirty(weapon);

        float invincibility = GetInvincibility(weapon.damageType);
        float effectiveInterval = Mathf.Max(weapon.cooldown, invincibility);
        float effectiveDps = effectiveInterval > 0f ? weapon.damage / effectiveInterval : 0f;
        bool wasteful = weapon.cooldown < invincibility - 0.0001f;

        GUILayout.Label($"{effectiveInterval:0.00}s", GUILayout.Width(65));
        GUILayout.Label($"{effectiveDps:0.0}", GUILayout.Width(65));
        GUILayout.Label(wasteful ? "⚠" : "✔", GUILayout.Width(30));

        EditorGUILayout.EndHorizontal();

        if (wasteful)
        {
            EditorGUILayout.HelpBox(
                $"쿨다운({weapon.cooldown:0.00}s)이 {weapon.damageType} 무적시간({invincibility:0.00}s)보다 " +
                "짧아 일부 사격이 데미지 없이 낭비됩니다. 쿨다운을 무적시간 이상으로 올리세요.",
                MessageType.Warning);
        }
    }

    private void DrawDetailPanel(GameWeaponData weapon)
    {
        EditorGUILayout.LabelField($"{weapon.name} 상세", EditorStyles.boldLabel);

        var so = new SerializedObject(weapon);
        so.Update();
        EditorGUILayout.PropertyField(so.FindProperty("damageType"));
        EditorGUILayout.PropertyField(so.FindProperty("pelletCount"));
        EditorGUILayout.PropertyField(so.FindProperty("spreadAngleDeg"));
        EditorGUILayout.PropertyField(so.FindProperty("pierceCount"));
        EditorGUILayout.PropertyField(so.FindProperty("projectileSpeed"));
        EditorGUILayout.PropertyField(so.FindProperty("projectileMaxDistance"));
        EditorGUILayout.PropertyField(so.FindProperty("knockbackDistance"));
        if (so.ApplyModifiedProperties()) EditorUtility.SetDirty(weapon);

        EditorGUILayout.Space();

        int totalDamage = weapon.damage * weapon.maxAmmo;
        EditorGUILayout.LabelField("총 화력 (단일 대상, 탄약 전량 소진 시)", totalDamage.ToString());

        if (weapon.pelletCount > 1)
        {
            int totalAoe = weapon.damage * weapon.pelletCount * weapon.maxAmmo;
            EditorGUILayout.LabelField("광역 최대 총 화력 (매 발 전 펠릿 명중 시)", totalAoe.ToString());
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("몬스터별 필요 탄수", EditorStyles.boldLabel);

        foreach (var monster in monsters)
        {
            if (monster == null) continue;

            int shots = weapon.damage > 0 ? Mathf.CeilToInt((float)monster.maxHP / weapon.damage) : 0;
            bool enough = weapon.maxAmmo <= 0 || shots <= weapon.maxAmmo; // maxAmmo 0 이하는 무제한 취급
            string mark = enough ? "✔" : "✘ (탄약 부족)";

            EditorGUILayout.LabelField($"{monster.monsterName} (HP {monster.maxHP})", $"{shots}발 {mark}");
        }
    }
}
