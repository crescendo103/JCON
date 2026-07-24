using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using ReorderableList = UnityEditorInternal.ReorderableList;
using System.Linq;
using System.Collections.Generic;
using System.IO;



public class MonsterMakerWindow : EditorWindow
{
    // Move/Attack 모션을 단일 클립으로 만들지, 8방향 Blend Tree로 만들지 선택
    private enum MotionMode { SingleClip, BlendTree8Way }

    // 8방향 라벨/좌표/파일 접미사는 MonsterDirections(공통 상수)를 사용한다.
    private static readonly string[] DirLabels = MonsterDirections.Labels;
    private static readonly Vector2[] DirVectors = MonsterDirections.Vectors;

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

    // 애니메이션마다 기존 클립을 연결해서 쓸지, 메이커 안에서 스프라이트로 직접 만들지 각각 선택.
    private enum AnimSourceMode { ExistingClips, GenerateFromSprites }
    private AnimSourceMode idleSourceMode = AnimSourceMode.ExistingClips;
    private AnimSourceMode moveSourceMode = AnimSourceMode.ExistingClips;
    private AnimSourceMode attackSourceMode = AnimSourceMode.ExistingClips;
    private AnimSourceMode hitSourceMode = AnimSourceMode.ExistingClips;
    private AnimSourceMode deathSourceMode = AnimSourceMode.ExistingClips;

    private SpriteClipSource idleSource = new SpriteClipSource();
    private SpriteClipSource moveSource = new SpriteClipSource();
    private SpriteClipSource[] moveDirSources = NewSourceArray();
    private SpriteClipSource attackSource = new SpriteClipSource();
    private SpriteClipSource[] attackDirSources = NewSourceArray();
    private SpriteClipSource hitSource = new SpriteClipSource();
    private SpriteClipSource[] hitDirSources = NewSourceArray();
    private SpriteClipSource deathSource = new SpriteClipSource();

    private static SpriteClipSource[] NewSourceArray()
    {
        var arr = new SpriteClipSource[8];
        for (int i = 0; i < arr.Length; i++) arr[i] = new SpriteClipSource();
        return arr;
    }

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
                EditorGUILayout.EndHorizontal();
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
            EditorGUILayout.HelpBox("프로젝트에 SkillData 애셋이 없습니다. Skill Maker로 먼저 만들어주세요.", MessageType.Info);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Skill Maker 열기 (새 스킬 만들기)"))
        {
            SkillMakerWindow.ShowWindow();
        }
        if (GUILayout.Button("스킬 목록 새로고침"))
        {
            RefreshSkillList();
        }

        // ── 애니메이션 ──────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("애니메이션", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "애니메이션마다 기존 클립을 연결할지, 스프라이트로 새로 만들지 각각 선택할 수 있습니다.",
            MessageType.None);
        EditorGUILayout.Space(4);

        DrawAnimClipSlot("Idle", ref idleSourceMode, ref idleClip, idleSource);

        // Move: 단일 클립 또는 8방향 Blend Tree(이동 방향 블렌드) 중 선택
        DrawMotionSlot("Move", ref moveSourceMode, ref moveMode, ref moveClip, moveDirClips, moveSource, moveDirSources);

        // Attack: 단일 클립 또는 8방향 Blend Tree(이동 방향을 재사용하는 공격 방향 블렌드) 중 선택
        DrawMotionSlot("Attack", ref attackSourceMode, ref attackMode, ref attackClip, attackDirClips, attackSource, attackDirSources);
        if (attackMode == MotionMode.BlendTree8Way)
        {
            EditorGUILayout.HelpBox(
                "공격 Blend Tree는 이동 방향(마지막으로 바라본 방향)을 그대로 재사용합니다. " +
                "MonsterController가 FaceX/FaceY 파라미터를 자동으로 갱신합니다.",
                MessageType.Info);
        }

        // Hit: 단일 클립 또는 8방향 Blend Tree(이동 방향을 재사용하는 피격 방향 블렌드) 중 선택
        DrawMotionSlot("Hit", ref hitSourceMode, ref hitMode, ref hitClip, hitDirClips, hitSource, hitDirSources);
        if (hitMode == MotionMode.BlendTree8Way)
        {
            EditorGUILayout.HelpBox(
                "피격 Blend Tree도 이동 방향(마지막으로 바라본 방향)을 그대로 재사용합니다. " +
                "MonsterController가 FaceX/FaceY 파라미터를 자동으로 갱신합니다.",
                MessageType.Info);
        }

        DrawAnimClipSlot("Death", ref deathSourceMode, ref deathClip, deathSource);

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Assets/Animations/[몬스터이름] 폴더에서 자동으로 채우기 (기존 클립 연결 모드 + 단일 클립만)"))
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
            "자동 채우기는 '기존 클립 연결' 모드로 설정된 항목의 AnimationClip만 찾습니다. " +
            "Move/Attack이 8방향 Blend Tree 모드라면 각 방향 클립을 직접 드래그하여 연결해주세요.",
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

    // Idle/Death 공용 UI: 이 슬롯만의 소스 모드(기존 클립 연결 / 스프라이트로 생성) 토글 + 그에 맞는 필드.
    private void DrawAnimClipSlot(string title, ref AnimSourceMode sourceMode, ref AnimationClip clip, SpriteClipSource source)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        sourceMode = (AnimSourceMode)GUILayout.Toolbar((int)sourceMode, new[] { "기존 클립 연결", "스프라이트로 생성" });

        if (sourceMode == AnimSourceMode.ExistingClips)
        {
            clip = (AnimationClip)EditorGUILayout.ObjectField("클립", clip, typeof(AnimationClip), false);
        }
        else
        {
            source.DrawGUI(title);
        }

        EditorGUILayout.Space(4);
    }

    // Move/Attack/Hit 공용 UI: 모션 타입(단일 클립/8방향)과 소스 모드(기존 클립/스프라이트)를 각각 선택.
    // 단일 클립이면 필드/스프라이트 목록 하나, 8방향이면 방향별 필드(또는 방향별 폴드아웃 스프라이트 목록).
    private void DrawMotionSlot(string title, ref AnimSourceMode sourceMode, ref MotionMode motionMode,
        ref AnimationClip singleClip, AnimationClip[] dirClips, SpriteClipSource singleSource, SpriteClipSource[] dirSources)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        motionMode = (MotionMode)EditorGUILayout.EnumPopup("모션 타입", motionMode);
        sourceMode = (AnimSourceMode)GUILayout.Toolbar((int)sourceMode, new[] { "기존 클립 연결", "스프라이트로 생성" });

        if (motionMode == MotionMode.SingleClip)
        {
            if (sourceMode == AnimSourceMode.ExistingClips)
            {
                singleClip = (AnimationClip)EditorGUILayout.ObjectField("클립", singleClip, typeof(AnimationClip), false);
            }
            else
            {
                singleSource.DrawGUI(title);
            }
        }
        else
        {
            EditorGUI.indentLevel++;

            if (sourceMode == AnimSourceMode.ExistingClips)
            {
                EditorGUILayout.HelpBox(
                    "최대 8방향까지 클립을 등록할 수 있습니다. 비워둔 방향은 Blend Tree에서 제외됩니다.",
                    MessageType.Info);

                for (int i = 0; i < dirClips.Length; i++)
                {
                    dirClips[i] = (AnimationClip)EditorGUILayout.ObjectField(DirLabels[i], dirClips[i], typeof(AnimationClip), false);
                }
            }
            else
            {
                for (int i = 0; i < dirSources.Length; i++)
                {
                    dirSources[i].foldout = EditorGUILayout.Foldout(dirSources[i].foldout, DirLabels[i], true);
                    if (dirSources[i].foldout)
                    {
                        dirSources[i].DrawGUI(DirLabels[i]);
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);
    }

    // 방향별 폴더 접미사 (DirLabels/DirVectors와 같은 순서: N, NE, E, SE, S, SW, W, NW)
    private static readonly string[] DirFileSuffixes = MonsterDirections.FileSuffixes;

    // 스프라이트로 생성 모드로 설정된 슬롯만 실제 .anim 클립을 만들어 idleClip/moveClip 등 기존
    // 필드에 채워 넣는다. 기존 클립 연결 모드인 슬롯은 손대지 않는다(이미 필드에 든 값을 그대로 사용).
    // 이렇게 해두면 이후 BuildAnimatorController 등 기존 파이프라인은 손댈 필요가 없다.
    private void GenerateClipsFromSprites()
    {
        bool anySpriteSlot =
            idleSourceMode == AnimSourceMode.GenerateFromSprites ||
            deathSourceMode == AnimSourceMode.GenerateFromSprites ||
            moveSourceMode == AnimSourceMode.GenerateFromSprites ||
            attackSourceMode == AnimSourceMode.GenerateFromSprites ||
            hitSourceMode == AnimSourceMode.GenerateFromSprites;
        if (!anySpriteSlot) return;

        string animFolder = $"Assets/Animations/{monsterName}";
        if (!AssetDatabase.IsValidFolder(animFolder))
        {
            Directory.CreateDirectory(animFolder);
            AssetDatabase.Refresh();
        }

        if (idleSourceMode == AnimSourceMode.GenerateFromSprites)
            idleClip = BuildClipOrKeep(idleSource, $"{animFolder}/idle.anim", idleClip);

        if (deathSourceMode == AnimSourceMode.GenerateFromSprites)
            deathClip = BuildClipOrKeep(deathSource, $"{animFolder}/death.anim", deathClip);

        if (moveSourceMode == AnimSourceMode.GenerateFromSprites)
        {
            if (moveMode == MotionMode.SingleClip)
                moveClip = BuildClipOrKeep(moveSource, $"{animFolder}/move.anim", moveClip);
            else
                BuildDirClips(moveDirSources, moveDirClips, animFolder, "move");
        }

        if (attackSourceMode == AnimSourceMode.GenerateFromSprites)
        {
            if (attackMode == MotionMode.SingleClip)
                attackClip = BuildClipOrKeep(attackSource, $"{animFolder}/attack.anim", attackClip);
            else
                BuildDirClips(attackDirSources, attackDirClips, animFolder, "attack");
        }

        if (hitSourceMode == AnimSourceMode.GenerateFromSprites)
        {
            if (hitMode == MotionMode.SingleClip)
                hitClip = BuildClipOrKeep(hitSource, $"{animFolder}/hit.anim", hitClip);
            else
                BuildDirClips(hitDirSources, hitDirClips, animFolder, "hit");
        }

        AssetDatabase.SaveAssets();
    }

    // source에 스프라이트가 하나도 없으면 기존 값(fallback)을 그대로 둔다.
    private AnimationClip BuildClipOrKeep(SpriteClipSource source, string path, AnimationClip fallback)
    {
        if (!source.HasAnySprite()) return fallback;
        var clip = SpriteClipBuilder.BuildClip(source.sprites, source.fps, source.loop, path);
        return clip != null ? clip : fallback;
    }

    private void BuildDirClips(SpriteClipSource[] sources, AnimationClip[] targetClips, string animFolder, string prefix)
    {
        for (int i = 0; i < sources.Length; i++)
        {
            if (!sources[i].HasAnySprite()) continue;
            string path = $"{animFolder}/{prefix}_{DirFileSuffixes[i]}.anim";
            targetClips[i] = SpriteClipBuilder.BuildClip(sources[i].sprites, sources[i].fps, sources[i].loop, path);
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

        // 스프라이트로 생성 모드로 설정된 애니메이션 슬롯이 있으면, 아래 BuildAnimatorController가
        // 쓰는 idleClip/moveClip 등 기존 필드를 여기서 실제 생성한 클립으로 채워 넣는다.
        GenerateClipsFromSprites();

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

        // Idle <-> Move : 이동 속도 기준. hasExitTime을 켜서 재생 중인 애니메이션이
        // 끝나기 전(90% 지점 전)에는 다른 상태로 끊기지 않도록 한다.
        if (idleState != null && moveState != null)
        {
            var toMove = idleState.AddTransition(moveState);
            toMove.hasExitTime = true;
            toMove.exitTime = 0.9f;
            toMove.duration = 0.1f;
            toMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, MonsterController.ParamSpeed);

            var toIdle = moveState.AddTransition(idleState);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 0.9f;
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

    // hasExitTime을 켜서, 현재 재생 중인 상태(Attack/Hit 등)가 끝나기 전(90% 지점 전)에는
    // 다른 트리거가 와도 끊고 끼어들지 못하게 한다 (예: Attack 재생 중 Hit이 들어와도 대기).
    private void AddAnyStateTransition(AnimatorStateMachine sm, AnimatorState target, string triggerParam)
    {
        if (target == null) return;

        var t = sm.AddAnyStateTransition(target);
        t.hasExitTime = true;
        t.exitTime = 0.9f;
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

        // 체력 관리(MonsterHealth)와 체력바(MonsterHealthBar)도 MonsterController와 마찬가지로
        // 생성 시점에 미리 연결해, 디자이너가 프리팹에서 바로 체력바 스프라이트 등을 커스터마이징할 수 있게 한다.
        go.AddComponent<MonsterHealth>();
        go.AddComponent<MonsterHealthBar>();

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

        if (idleSourceMode == AnimSourceMode.ExistingClips)
            idleClip = FindClipInFolder(folder, "idle");

        if (deathSourceMode == AnimSourceMode.ExistingClips)
            deathClip = FindClipInFolder(folder, "death");

        if (moveSourceMode == AnimSourceMode.ExistingClips && moveMode == MotionMode.SingleClip)
            moveClip = FindClipInFolder(folder, "move");

        if (attackSourceMode == AnimSourceMode.ExistingClips && attackMode == MotionMode.SingleClip)
            attackClip = FindClipInFolder(folder, "attack");

        if (hitSourceMode == AnimSourceMode.ExistingClips && hitMode == MotionMode.SingleClip)
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

// 애니메이션 슬롯 하나(Idle/Move/Attack/Hit/Death 또는 스킬)에 대한 스프라이트 목록 + fps + loop 편집 UI.
// 씬에 임시 오브젝트를 만들고 Animation 창에서 프레임을 찍는 대신, 여기서 스프라이트를 직접 넣고
// 순서를 드래그로 바꾸거나 fps/반복 여부를 조정한 뒤 SpriteClipBuilder로 .anim을 생성한다.
[System.Serializable]
public class SpriteClipSource
{
    public List<Sprite> sprites = new List<Sprite>();
    public float fps = 10f;
    public bool loop = true;
    public bool foldout; // 8방향 모드에서 방향별 목록을 접었다 펼 때 사용

    [System.NonSerialized] private ReorderableList list;

    public bool HasAnySprite() => sprites != null && sprites.Any(s => s != null);

    public void DrawGUI(string title)
    {
        DrawDropArea();

        if (list == null)
        {
            list = new ReorderableList(sprites, typeof(Sprite), true, false, true, true);
            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.y += 1;
                rect.height = EditorGUIUtility.singleLineHeight;
                sprites[index] = (Sprite)EditorGUI.ObjectField(rect, sprites[index], typeof(Sprite), false);
            };
            list.onAddCallback = l => l.list.Add(null);
        }

        list.DoLayoutList();

        EditorGUILayout.BeginHorizontal();

        // 라벨("fps")이 기본 labelWidth(보통 150px 안팎)를 그대로 먹어버리면
        // GUILayout.Width로 준 전체 폭 안에서 정작 숫자 입력칸이 남지 않아 거의 안 보이게 눌린다.
        // 이 필드에서만 라벨 폭을 좁게 고정해서 입력칸 공간을 확보한다.
        float prevLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 28f;
        fps = Mathf.Max(0.1f, EditorGUILayout.FloatField("fps", fps, GUILayout.Width(90)));
        EditorGUIUtility.labelWidth = prevLabelWidth;

        loop = EditorGUILayout.ToggleLeft("반복 재생(loop)", loop, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    // 스프라이트(여러 개 동시 선택)나 멀티 스프라이트 텍스처를 한 번에 드래그&드롭해 목록에 추가.
    private void DrawDropArea()
    {
        Rect dropRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "스프라이트를 여기로 드래그하면 추가됩니다 (여러 개 동시 가능)", EditorStyles.helpBox);

        Event evt = Event.current;
        if (!dropRect.Contains(evt.mousePosition)) return;
        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

        if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
                else if (obj is Texture2D texture)
                {
                    // 스프라이트시트(멀티 스프라이트)를 드래그하면 안의 모든 서브 스프라이트를 추가.
                    string path = AssetDatabase.GetAssetPath(texture);
                    var subSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>();
                    sprites.AddRange(subSprites);
                }
            }
        }

        evt.Use();
    }
}
