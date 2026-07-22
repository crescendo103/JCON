using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;
using System.Collections.Generic;
using System.IO;



public class MonsterMakerWindow : EditorWindow
{
    // Move/Attack 모션을 단일 클립으로 만들지, 8방향 Blend Tree로 만들지 선택
    private enum MotionMode { SingleClip, BlendTree8Way }

    // 8방향 순서 및 2D Freeform Directional 좌표 (N, NE, E, SE, S, SW, W, NW)
    private static readonly string[] DirLabels =
    {
        "N (위)", "NE (오른쪽 위)", "E (오른쪽)", "SE (오른쪽 아래)",
        "S (아래)", "SW (왼쪽 아래)", "W (왼쪽)", "NW (왼쪽 위)"
    };
    private static readonly Vector2[] DirVectors =
    {
        new Vector2(0f, 1f), new Vector2(0.7071f, 0.7071f), new Vector2(1f, 0f), new Vector2(0.7071f, -0.7071f),
        new Vector2(0f, -1f), new Vector2(-0.7071f, -0.7071f), new Vector2(-1f, 0f), new Vector2(-0.7071f, 0.7071f)
    };

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

    private Sprite icon;
    private AnimationClip idleClip;

    private MotionMode moveMode = MotionMode.SingleClip;
    private AnimationClip moveClip;
    private AnimationClip[] moveDirClips = new AnimationClip[8];

    private MotionMode attackMode = MotionMode.SingleClip;
    private AnimationClip attackClip;
    private AnimationClip[] attackDirClips = new AnimationClip[8];

    private MotionMode hitMode = MotionMode.SingleClip;
    private AnimationClip hitClip;
    private AnimationClip[] hitDirClips = new AnimationClip[8];

    private AnimationClip deathClip;
    private string saveFolder = "Assets/Monsters";

    private Vector2 scrollPos;

    [MenuItem("Tools/Monster Maker")]
    public static void ShowWindow()
    {
        var window = GetWindow<MonsterMakerWindow>("Monster Maker");
        window.minSize = new Vector2(380, 520);
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
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Monster Maker", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ── 기본 정보 ──────────────────────────
        monsterName = EditorGUILayout.TextField("몬스터 이름", monsterName);
        icon = (Sprite)EditorGUILayout.ObjectField("아이콘", icon, typeof(Sprite), false);

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
        EditorGUILayout.Space(4);

        // Move: 단일 클립 또는 8방향 Blend Tree(이동 방향 블렌드) 중 선택
        DrawMotionField("Move", ref moveMode, ref moveClip, moveDirClips);

        // Attack: 단일 클립 또는 8방향 Blend Tree(이동 방향을 재사용하는 공격 방향 블렌드) 중 선택
        DrawMotionField("Attack", ref attackMode, ref attackClip, attackDirClips);
        if (attackMode == MotionMode.BlendTree8Way)
        {
            EditorGUILayout.HelpBox(
                "공격 Blend Tree는 이동 방향(마지막으로 바라본 방향)을 그대로 재사용합니다. " +
                "MonsterController가 FaceX/FaceY 파라미터를 자동으로 갱신합니다.",
                MessageType.Info);
        }

        // Hit: 단일 클립 또는 8방향 Blend Tree(이동 방향을 재사용하는 피격 방향 블렌드) 중 선택
        DrawMotionField("Hit", ref hitMode, ref hitClip, hitDirClips);
        if (hitMode == MotionMode.BlendTree8Way)
        {
            EditorGUILayout.HelpBox(
                "피격 Blend Tree도 이동 방향(마지막으로 바라본 방향)을 그대로 재사용합니다. " +
                "MonsterController가 FaceX/FaceY 파라미터를 자동으로 갱신합니다.",
                MessageType.Info);
        }

        deathClip = (AnimationClip)EditorGUILayout.ObjectField("Death", deathClip, typeof(AnimationClip), false);

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Assets/Animations/[몬스터이름] 폴더에서 자동으로 채우기 (단일 클립만)"))
        {
            if (string.IsNullOrEmpty(monsterName))
            {
                EditorUtility.DisplayDialog("알림", "몬스터 이름을 먼저 입력해주세요.", "확인");
            }
            else
            {
                AutoDetectAnimations(monsterName);
            }
        }
        EditorGUILayout.HelpBox(
            "자동 채우기는 AnimationClip만 찾습니다. Move/Attack이 8방향 Blend Tree 모드라면 " +
            "각 방향 클립을 직접 드래그하여 연결해주세요.",
            MessageType.None);

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

        EditorGUILayout.Space(12);
        EditorGUILayout.EndScrollView();
    }

    // Move/Attack 공용 UI: 단일 클립 ObjectField 하나 또는 8방향 ObjectField 8개를 그린다.
    private void DrawMotionField(string title, ref MotionMode mode, ref AnimationClip singleClip, AnimationClip[] dirClips)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        mode = (MotionMode)EditorGUILayout.EnumPopup("모션 타입", mode);

        if (mode == MotionMode.SingleClip)
        {
            singleClip = (AnimationClip)EditorGUILayout.ObjectField("클립", singleClip, typeof(AnimationClip), false);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "최대 8방향까지 클립을 등록할 수 있습니다. 비워둔 방향은 Blend Tree에서 제외됩니다.",
                MessageType.Info);

            EditorGUI.indentLevel++;
            for (int i = 0; i < dirClips.Length; i++)
            {
                dirClips[i] = (AnimationClip)EditorGUILayout.ObjectField(DirLabels[i], dirClips[i], typeof(AnimationClip), false);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);
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
        data.icon = icon;
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

        // AnimatorController(+ 필요 시 8방향 Blend Tree)를 먼저 생성하고,
        // 실제 각 상태에 쓰인 Motion(단일 클립 또는 만들어진 BlendTree)을 그대로 데이터에 기록한다.
        AnimatorController controller = BuildAnimatorController(saveFolder,
            out Motion moveMotionResult, out Motion attackMotionResult, out Motion hitMotionResult);

        var animSet = new MonsterAnimationSet
        {
            idle = idleClip,
            move = moveMotionResult,
            attack = attackMotionResult,
            hit = hitMotionResult,
            death = deathClip
        };
        data.animations = animSet;

        GameObject prefab = BuildPrefab(controller, data, saveFolder);
        data.prefab = prefab;

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

    // ── AnimatorController / BlendTree 생성 ──────────────────────────

    // Idle/Move/Attack/Hit/Death 상태와 그 사이 전이를 갖춘 AnimatorController를 생성한다.
    // Move/Attack/Hit이 8방향 모드면 2D Freeform Directional Blend Tree로, 아니면 단일 클립 상태로 만든다.
    private AnimatorController BuildAnimatorController(string folder,
        out Motion moveMotionResult, out Motion attackMotionResult, out Motion hitMotionResult)
    {
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{monsterName}_Controller.controller");
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        controller.AddParameter(MonsterController.ParamMoveX, AnimatorControllerParameterType.Float);
        controller.AddParameter(MonsterController.ParamMoveY, AnimatorControllerParameterType.Float);
        controller.AddParameter(MonsterController.ParamFaceX, AnimatorControllerParameterType.Float);
        controller.AddParameter(MonsterController.ParamFaceY, AnimatorControllerParameterType.Float);
        controller.AddParameter(MonsterController.ParamSpeed, AnimatorControllerParameterType.Float);
        controller.AddParameter(MonsterController.ParamAttack, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(MonsterController.ParamHit, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(MonsterController.ParamDeath, AnimatorControllerParameterType.Trigger);

        var rootSM = controller.layers[0].stateMachine;

        AnimatorState idleState = null;
        if (idleClip != null)
        {
            idleState = rootSM.AddState("Idle");
            idleState.motion = idleClip;
            rootSM.defaultState = idleState;
        }

        AnimatorState moveState = CreateMotionState(controller, rootSM, "Move", moveMode, moveClip, moveDirClips,
            MonsterController.ParamMoveX, MonsterController.ParamMoveY);
        AnimatorState attackState = CreateMotionState(controller, rootSM, "Attack", attackMode, attackClip, attackDirClips,
            MonsterController.ParamFaceX, MonsterController.ParamFaceY);

        AnimatorState hitState = CreateMotionState(controller, rootSM, "Hit", hitMode, hitClip, hitDirClips,
            MonsterController.ParamFaceX, MonsterController.ParamFaceY);

        moveMotionResult = moveState != null ? moveState.motion : null;
        attackMotionResult = attackState != null ? attackState.motion : null;
        hitMotionResult = hitState != null ? hitState.motion : null;

        AnimatorState deathState = null;
        if (deathClip != null)
        {
            deathState = rootSM.AddState("Death");
            deathState.motion = deathClip;
        }

        // Idle <-> Move : 이동 속도 기준
        if (idleState != null && moveState != null)
        {
            var toMove = idleState.AddTransition(moveState);
            toMove.hasExitTime = false;
            toMove.duration = 0.1f;
            toMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, MonsterController.ParamSpeed);

            var toIdle = moveState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.1f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, MonsterController.ParamSpeed);
        }

        // AnyState -> Attack/Hit/Death : 트리거 기준
        AddAnyStateTransition(rootSM, attackState, MonsterController.ParamAttack);
        AddAnyStateTransition(rootSM, hitState, MonsterController.ParamHit);
        AddAnyStateTransition(rootSM, deathState, MonsterController.ParamDeath);

        // Attack/Hit 재생이 끝나면 Idle로 복귀 (Death는 복귀 없음)
        AddExitTimeReturn(attackState, idleState);
        AddExitTimeReturn(hitState, idleState);

        return controller;
    }

    // 단일 클립 모드면 상태에 클립을 그대로 물리고, 8방향 모드면 2D Freeform Directional Blend Tree를 만들어 상태에 연결한다.
    // 유효한 클립이 하나도 없으면 상태 자체를 만들지 않고 null을 반환한다.
    private AnimatorState CreateMotionState(AnimatorController controller, AnimatorStateMachine rootSM, string stateName,
        MotionMode mode, AnimationClip singleClip, AnimationClip[] dirClips, string paramX, string paramY)
    {
        if (mode == MotionMode.SingleClip)
        {
            if (singleClip == null) return null;

            var state = rootSM.AddState(stateName);
            state.motion = singleClip;
            return state;
        }

        // BlendTree8Way
        if (!dirClips.Any(c => c != null)) return null;

        controller.CreateBlendTreeInController(stateName, out BlendTree tree);
        tree.blendType = BlendTreeType.FreeformDirectional2D;
        tree.blendParameter = paramX;
        tree.blendParameterY = paramY;

        for (int i = 0; i < dirClips.Length; i++)
        {
            if (dirClips[i] != null)
            {
                tree.AddChild(dirClips[i], DirVectors[i]);
            }
        }

        // CreateBlendTreeInController가 layer 0 스테이트머신에 stateName으로 상태를 자동 추가한다.
        return rootSM.states.FirstOrDefault(s => s.state.name == stateName).state;
    }

    private void AddAnyStateTransition(AnimatorStateMachine sm, AnimatorState target, string triggerParam)
    {
        if (target == null) return;

        var t = sm.AddAnyStateTransition(target);
        t.hasExitTime = false;
        t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0f, triggerParam);
    }

    private void AddExitTimeReturn(AnimatorState from, AnimatorState to)
    {
        if (from == null || to == null) return;

        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = 0.9f;
        t.duration = 0.1f;
    }

    // ── 프리팹 생성 ──────────────────────────

    // 2D 몬스터 프리팹: SpriteRenderer + Rigidbody2D + CircleCollider2D + Animator + MonsterController
    private GameObject BuildPrefab(AnimatorController controller, MonsterData data, string folder)
    {
        var go = new GameObject(monsterName);

        var sr = go.AddComponent<SpriteRenderer>();
        if (icon != null) sr.sprite = icon;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        go.AddComponent<CircleCollider2D>();

        var animatorComp = go.AddComponent<Animator>();
        animatorComp.runtimeAnimatorController = controller;

        var monsterController = go.AddComponent<MonsterController>();
        monsterController.data = data;

        string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{monsterName}.prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        return prefab;
    }

    // Assets/Animations/[몬스터이름]/ 폴더 안에서 이름 규칙으로 클립을 찾아 연결.
    // 규칙: idle.anim, move.anim, attack.anim, hit.anim, death.anim
    // 주의: 이 자동 감지는 AnimationClip만 찾는다. Move/Attack/Hit이 8방향 Blend Tree 모드라면
    // 이 함수로는 채워지지 않으므로 방향별로 수동으로 드래그해서 연결해야 한다.
    private void AutoDetectAnimations(string name)
    {
        string folder = $"Assets/Animations/{name}";

        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"애니메이션 폴더를 찾을 수 없습니다: {folder} (건너뜀)");
            return;
        }

        idleClip = FindClipInFolder(folder, "idle");
        deathClip = FindClipInFolder(folder, "death");

        if (moveMode == MotionMode.SingleClip)
            moveClip = FindClipInFolder(folder, "move");

        if (attackMode == MotionMode.SingleClip)
            attackClip = FindClipInFolder(folder, "attack");

        if (hitMode == MotionMode.SingleClip)
            hitClip = FindClipInFolder(folder, "hit");
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
