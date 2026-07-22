using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class MonsterMakerWindow : EditorWindow
{
    private string monsterName = "";
    private int maxHP = 150;
    private int attack = 25;
    private int defense = 10;
    private float speed = 4f;

    private List<System.Type> aiTypes = new List<System.Type>();
    private string[] aiTypeNames;
    private int selectedAIIndex = 0;

    private List<SkillData> allSkillsInProject = new List<SkillData>();
    private List<SkillData> selectedSkills = new List<SkillData>();
    private int skillPickerIndex = 0;

    private AnimationClip idleClip;
    private AnimationClip moveClip;
    private AnimationClip attackClip;
    private AnimationClip hitClip;
    private AnimationClip deathClip;
    private string saveFolder = "Assets/Monsters";

    [MenuItem("Tools/Monster Maker")]
    public static void ShowWindow()
    {
        var window = GetWindow<MonsterMakerWindow>("Monster Maker");
        window.minSize = new Vector2(380, 500);
    }

    private void OnEnable()
    {
        RefreshAITypes();
        RefreshSkillList();
    }

    // MonsterAIBehavior를 상속받은 모든 클래스를 리플렉션으로 자동 탐색.
    // 새 AI 클래스를 추가하면 이 창을 다시 열 때 자동으로 목록에 나타남.
    private void RefreshAITypes()
    {
        aiTypes = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsSubclassOf(typeof(MonsterAIBehavior)) && !t.IsAbstract)
            .ToList();

        aiTypeNames = aiTypes.Select(t => ObjectNames.NicifyVariableName(t.Name)).ToArray();
    }

    // 프로젝트 내 모든 SkillData 애셋을 찾아 스킬 선택 드롭다운에 사용.
    private void RefreshSkillList()
    {
        allSkillsInProject.Clear();
        string[] guids = AssetDatabase.FindAssets("t:SkillData");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill != null) allSkillsInProject.Add(skill);
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("Monster Maker", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ── 기본 정보 ──────────────────────────
        monsterName = EditorGUILayout.TextField("몬스터 이름", monsterName);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("스탯", EditorStyles.boldLabel);
        maxHP = EditorGUILayout.IntSlider("HP", maxHP, 10, 999);
        attack = EditorGUILayout.IntSlider("공격력", attack, 1, 200);
        defense = EditorGUILayout.IntSlider("방어력", defense, 0, 200);
        speed = EditorGUILayout.Slider("이동속도", speed, 0.5f, 15f);

        // ── AI 패턴 ──────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("AI 패턴", EditorStyles.boldLabel);
        if (aiTypeNames.Length > 0)
        {
            selectedAIIndex = EditorGUILayout.Popup("패턴 선택", selectedAIIndex, aiTypeNames);
        }
        else
        {
            EditorGUILayout.HelpBox("MonsterAIBehavior를 상속받은 클래스가 없습니다.", MessageType.Warning);
        }

        // ── 스킬 ──────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("스킬", EditorStyles.boldLabel);

        for (int i = 0; i < selectedSkills.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(selectedSkills[i].skillName);
            if (GUILayout.Button("x", GUILayout.Width(24)))
            {
                selectedSkills.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        if (allSkillsInProject.Count > 0)
        {
            string[] skillNames = allSkillsInProject.Select(s => s.skillName).ToArray();
            skillPickerIndex = EditorGUILayout.Popup(skillPickerIndex, skillNames);
            if (GUILayout.Button("+ 스킬 추가", GUILayout.Width(90)))
            {
                var picked = allSkillsInProject[skillPickerIndex];
                if (!selectedSkills.Contains(picked))
                    selectedSkills.Add(picked);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("프로젝트에 SkillData 애셋이 없습니다. 먼저 만들어주세요.", MessageType.Info);
        }
        EditorGUILayout.EndHorizontal();

        // ── 애니메이션 ──────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("애니메이션", EditorStyles.boldLabel);

        idleClip = (AnimationClip)EditorGUILayout.ObjectField("Idle", idleClip, typeof(AnimationClip), false);
        moveClip = (AnimationClip)EditorGUILayout.ObjectField("Move", moveClip, typeof(AnimationClip), false);
        attackClip = (AnimationClip)EditorGUILayout.ObjectField("Attack", attackClip, typeof(AnimationClip), false);
        hitClip = (AnimationClip)EditorGUILayout.ObjectField("Hit", hitClip, typeof(AnimationClip), false);
        deathClip = (AnimationClip)EditorGUILayout.ObjectField("Death", deathClip, typeof(AnimationClip), false);

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Assets/Animations/[몬스터이름] 폴더에서 자동으로 채우기"))
        {
            if (string.IsNullOrEmpty(monsterName))
            {
                EditorUtility.DisplayDialog("알림", "몬스터 이름을 먼저 입력해주세요.", "확인");
            }
            else
            {
                var detected = AutoDetectAnimations(monsterName);
                idleClip = detected.idle;
                moveClip = detected.move;
                attackClip = detected.attack;
                hitClip = detected.hit;
                deathClip = detected.death;
            }
        }

        // ── 저장 경로 ──────────────────────────
        EditorGUILayout.Space();
        saveFolder = EditorGUILayout.TextField("저장 경로", saveFolder);

        EditorGUILayout.Space(12);

        GUI.enabled = !string.IsNullOrEmpty(monsterName);
        if (GUILayout.Button("몬스터 생성하기", GUILayout.Height(36)))
        {
            CreateMonster();
        }
        GUI.enabled = true;

        if (string.IsNullOrEmpty(monsterName))
        {
            EditorGUILayout.HelpBox("몬스터 이름을 입력해야 생성할 수 있습니다.", MessageType.Warning);
        }
    }

    private void CreateMonster()
    {
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
            AssetDatabase.Refresh();
        }

        MonsterData data = CreateInstance<MonsterData>();
        data.monsterName = monsterName;
        data.maxHP = maxHP;
        data.attack = attack;
        data.defense = defense;
        data.speed = speed;
        data.skills = selectedSkills.ToArray();

        // AI 애셋: 없으면 자동 생성해서 연결, 있으면 기존 것 재사용
        if (aiTypes.Count > 0)
        {
            var chosenType = aiTypes[selectedAIIndex];
            data.aiBehavior = GetOrCreateAIAsset(chosenType);
        }

        var animSet = new MonsterAnimationSet
        {
            idle = idleClip,
            move = moveClip,
            attack = attackClip,
            hit = hitClip,
            death = deathClip
        };
        data.animations = animSet;

        string assetPath = $"{saveFolder}/{monsterName}.asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
        AssetDatabase.CreateAsset(data, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(data);
        Selection.activeObject = data;

        Debug.Log($"몬스터 '{monsterName}' 생성 완료 → {assetPath}");
    }

    // 같은 AI 타입의 공용 애셋이 이미 있으면 재사용, 없으면 새로 만듦.
    // (AI는 로직만 담긴 재사용 가능한 자산이라 몬스터마다 새로 만들 필요 없음)
    private MonsterAIBehavior GetOrCreateAIAsset(System.Type aiType)
    {
        string folder = "Assets/Monsters/AI";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        string[] guids = AssetDatabase.FindAssets($"t:{aiType.Name}");
        if (guids.Length > 0)
        {
            string existingPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<MonsterAIBehavior>(existingPath);
        }

        var newAI = CreateInstance(aiType) as MonsterAIBehavior;
        string path = $"{folder}/{aiType.Name}.asset";
        AssetDatabase.CreateAsset(newAI, path);
        AssetDatabase.SaveAssets();
        return newAI;
    }

    // Assets/Animations/[몬스터이름]/ 폴더 안에서 이름 규칙으로 클립을 찾아 연결.
    // 규칙: idle.anim, move.anim, attack.anim, hit.anim, death.anim
    private MonsterAnimationSet AutoDetectAnimations(string name)
    {
        var set = new MonsterAnimationSet();
        string folder = $"Assets/Animations/{name}";

        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"애니메이션 폴더를 찾을 수 없습니다: {folder} (건너뜀)");
            return set;
        }

        set.idle = FindClipInFolder(folder, "idle");
        set.move = FindClipInFolder(folder, "move");
        set.attack = FindClipInFolder(folder, "attack");
        set.hit = FindClipInFolder(folder, "hit");
        set.death = FindClipInFolder(folder, "death");

        return set;
    }

    private AnimationClip FindClipInFolder(string folder, string keyword)
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).ToLower().Contains(keyword))
            {
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            }
        }
        return null;
    }
}
