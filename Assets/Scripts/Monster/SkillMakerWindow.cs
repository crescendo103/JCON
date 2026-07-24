using UnityEngine;
using UnityEditor;
using System.IO;

// Monster Maker와 별개로 SkillData 애셋만 독립적으로 만드는 창.
// 연출(애니메이션 등)은 effectPrefab 안에서 스스로 처리하며, 유지 시간도 effectPrefab의 애니메이션
// 클립 길이(한 사이클)로 런타임에 자동 결정되므로(MonsterController.SpawnSkillEffect 참고) 여기서는
// 별도로 지속시간을 입력받지 않는다.
public class SkillMakerWindow : EditorWindow
{
    private string skillName = "";
    private string description = "";
    private int damage = 10;
    private float cooldown = 1f;
    private SkillType type = SkillType.Melee;
    private GameObject effectPrefab;
    private AudioClip sfx;
    private float effectScale = 1f;

    private string saveFolder = "Assets/Monsters/Skills";
    private Vector2 scrollPos;

    [MenuItem("Tools/Skill Maker")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillMakerWindow>("Skill Maker");
        window.minSize = new Vector2(360, 460);
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Skill Maker", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        skillName = EditorGUILayout.TextField("스킬 이름", skillName);
        EditorGUILayout.LabelField("설명");
        description = EditorGUILayout.TextArea(description, GUILayout.Height(40));
        damage = EditorGUILayout.IntField("피해량", damage);
        cooldown = EditorGUILayout.FloatField("쿨다운(초)", cooldown);
        type = (SkillType)EditorGUILayout.EnumPopup("타입", type);
        effectPrefab = (GameObject)EditorGUILayout.ObjectField("이펙트 프리팹", effectPrefab, typeof(GameObject), false);
        if (GUILayout.Button("Effect Prefab Maker 열기 (새 이펙트 만들기)"))
        {
            EffectMakerWindow.ShowWindow();
        }
        sfx = (AudioClip)EditorGUILayout.ObjectField("사운드", sfx, typeof(AudioClip), false);
        effectScale = EditorGUILayout.FloatField("이펙트 크기 배율", effectScale);

        EditorGUILayout.Space();
        saveFolder = EditorGUILayout.TextField("저장 경로", saveFolder);

        EditorGUILayout.Space(12);

        GUI.enabled = !string.IsNullOrEmpty(skillName);
        if (GUILayout.Button("스킬 생성하기", GUILayout.Height(36)))
        {
            CreateSkill();
        }
        GUI.enabled = true;

        if (string.IsNullOrEmpty(skillName))
        {
            EditorGUILayout.HelpBox("스킬 이름을 입력해야 생성할 수 있습니다.", MessageType.Warning);
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.EndScrollView();
    }

    private void CreateSkill()
    {
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
            AssetDatabase.Refresh();
        }

        var skill = CreateInstance<SkillData>();
        skill.skillName = skillName;
        skill.description = description;
        skill.damage = damage;
        skill.cooldown = cooldown;
        skill.type = type;
        skill.effectPrefab = effectPrefab;
        skill.sfx = sfx;
        skill.effectScale = effectScale;

        string path = AssetDatabase.GenerateUniqueAssetPath($"{saveFolder}/{skillName}.asset");
        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(skill);
        Selection.activeObject = skill;

        Debug.Log($"스킬 '{skillName}' 생성 완료 → {path}");

        // 다음 스킬을 위해 입력 필드 초기화
        skillName = "";
        description = "";
        damage = 10;
        cooldown = 1f;
        type = SkillType.Melee;
        effectPrefab = null;
        sfx = null;
        effectScale = 1f;
    }
}
