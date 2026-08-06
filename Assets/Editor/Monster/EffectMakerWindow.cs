using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;

// 스킬 이펙트 프리팹(SkillData.effectPrefab)을 만드는 창.
// Monster Maker와 동일하게 모션을 단일 클립 또는 8방향 Blend Tree로 구성할 수 있고,
// Player 태그 감지용 Collider2D + SkillEffectDamage를 함께 붙여 완성된 프리팹을 저장한다.
public class EffectMakerWindow : EditorWindow
{
    private enum AnimSourceMode { ExistingClip, GenerateFromSprites }
    private enum MotionMode { SingleClip, BlendTree8Way }
    private enum ColliderShape { Circle, Box }

    private string effectName = "";

    private MotionMode motionMode = MotionMode.SingleClip;
    private AnimSourceMode sourceMode = AnimSourceMode.ExistingClip;
    private AnimationClip singleClip;
    private SpriteClipSource singleSource = new SpriteClipSource();
    private AnimationClip[] dirClips = new AnimationClip[8];
    private SpriteClipSource[] dirSources = NewSourceArray();

    private ColliderShape colliderShape = ColliderShape.Circle;
    private float colliderRadius = 0.5f;
    private Vector2 colliderSize = Vector2.one * 0.5f;

    private int defaultDamage = 10;
    private string saveFolder = "Assets/Effects";
    private Vector2 scrollPos;

    private static SpriteClipSource[] NewSourceArray()
    {
        var arr = new SpriteClipSource[8];
        for (int i = 0; i < arr.Length; i++) arr[i] = new SpriteClipSource();
        return arr;
    }

    [MenuItem("Tools/Effect Prefab Maker")]
    public static void ShowWindow()
    {
        var window = GetWindow<EffectMakerWindow>("Effect Prefab Maker");
        window.minSize = new Vector2(380, 480);
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Effect Prefab Maker", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "SkillData의 effectPrefab에 연결할 이펙트 프리팹을 만듭니다. Player 태그를 감지하면 데미지를 전달하는 콜라이더/컴포넌트가 자동으로 붙습니다.",
            MessageType.Info);
        EditorGUILayout.Space();

        effectName = EditorGUILayout.TextField("이펙트 이름", effectName);

        EditorGUILayout.Space();
        DrawMotionUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("충돌 판정 (Player 감지)", EditorStyles.boldLabel);
        colliderShape = (ColliderShape)EditorGUILayout.EnumPopup("콜라이더 모양", colliderShape);
        if (colliderShape == ColliderShape.Circle)
            colliderRadius = EditorGUILayout.FloatField("반지름", colliderRadius);
        else
            colliderSize = EditorGUILayout.Vector2Field("크기", colliderSize);

        defaultDamage = EditorGUILayout.IntField("기본 피해량(테스트용)", defaultDamage);

        EditorGUILayout.Space();
        saveFolder = EditorGUILayout.TextField("저장 경로", saveFolder);

        EditorGUILayout.Space(12);

        GUI.enabled = !string.IsNullOrEmpty(effectName);
        if (GUILayout.Button("이펙트 프리팹 생성하기", GUILayout.Height(36)))
        {
            CreateEffectPrefab();
        }
        GUI.enabled = true;

        if (string.IsNullOrEmpty(effectName))
        {
            EditorGUILayout.HelpBox("이펙트 이름을 입력해야 생성할 수 있습니다.", MessageType.Warning);
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.EndScrollView();
    }

    // 모션 타입(단일 클립/8방향)과 소스 모드(기존 클립/스프라이트)를 선택. MonsterMakerWindow.DrawMotionSlot과 동일한 패턴.
    private void DrawMotionUI()
    {
        EditorGUILayout.LabelField("모션", EditorStyles.boldLabel);
        motionMode = (MotionMode)EditorGUILayout.EnumPopup("모션 타입", motionMode);
        sourceMode = (AnimSourceMode)GUILayout.Toolbar((int)sourceMode, new[] { "기존 클립 연결", "스프라이트로 생성" });

        if (motionMode == MotionMode.SingleClip)
        {
            if (sourceMode == AnimSourceMode.ExistingClip)
            {
                singleClip = (AnimationClip)EditorGUILayout.ObjectField("클립", singleClip, typeof(AnimationClip), false);
            }
            else
            {
                singleSource.DrawGUI("모션");
            }
        }
        else
        {
            EditorGUI.indentLevel++;

            if (sourceMode == AnimSourceMode.ExistingClip)
            {
                EditorGUILayout.HelpBox(
                    "최대 8방향까지 클립을 등록할 수 있습니다. 비워둔 방향은 Blend Tree에서 제외됩니다.",
                    MessageType.Info);

                for (int i = 0; i < dirClips.Length; i++)
                {
                    dirClips[i] = (AnimationClip)EditorGUILayout.ObjectField(
                        MonsterDirections.Labels[i], dirClips[i], typeof(AnimationClip), false);
                }
            }
            else
            {
                for (int i = 0; i < dirSources.Length; i++)
                {
                    dirSources[i].foldout = EditorGUILayout.Foldout(dirSources[i].foldout, MonsterDirections.Labels[i], true);
                    if (dirSources[i].foldout)
                    {
                        dirSources[i].DrawGUI(MonsterDirections.Labels[i]);
                    }
                }
            }

            EditorGUI.indentLevel--;
        }
    }

    private void CreateEffectPrefab()
    {
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
            AssetDatabase.Refresh();
        }

        GenerateClipsFromSprites();

        AnimatorController controller = BuildAnimatorController(saveFolder);
        GameObject prefab = BuildPrefab(controller, saveFolder);

        EditorGUIUtility.PingObject(prefab);
        Selection.activeObject = prefab;

        Debug.Log($"이펙트 프리팹 '{effectName}' 생성 완료 → {AssetDatabase.GetAssetPath(prefab)}");

        // 다음 이펙트를 위해 입력 필드 초기화
        effectName = "";
        motionMode = MotionMode.SingleClip;
        singleClip = null;
        singleSource = new SpriteClipSource();
        dirClips = new AnimationClip[8];
        dirSources = NewSourceArray();
        defaultDamage = 10;
    }

    // 스프라이트로 생성 모드일 때만 실제 .anim 클립을 만들어 singleClip/dirClips에 채운다.
    private void GenerateClipsFromSprites()
    {
        if (sourceMode != AnimSourceMode.GenerateFromSprites) return;

        string animFolder = $"Assets/Animations/Effects/{effectName}";
        if (!AssetDatabase.IsValidFolder(animFolder))
        {
            Directory.CreateDirectory(animFolder);
            AssetDatabase.Refresh();
        }

        if (motionMode == MotionMode.SingleClip)
        {
            if (singleSource.HasAnySprite())
                singleClip = SpriteClipBuilder.BuildClip(singleSource.sprites, singleSource.fps, singleSource.loop, $"{animFolder}/motion.anim");
        }
        else
        {
            for (int i = 0; i < dirSources.Length; i++)
            {
                if (!dirSources[i].HasAnySprite()) continue;
                string path = $"{animFolder}/motion_{MonsterDirections.FileSuffixes[i]}.anim";
                dirClips[i] = SpriteClipBuilder.BuildClip(dirSources[i].sprites, dirSources[i].fps, dirSources[i].loop, path);
            }
        }

        AssetDatabase.SaveAssets();
    }

    // 단일 클립이면 그대로 재생하는 상태 하나, 8방향이면 FaceX/FaceY로 구동되는
    // FreeformDirectional2D Blend Tree를 기본 상태로 갖는 AnimatorController를 만든다.
    // 유효한 클립이 하나도 없으면 컨트롤러를 만들지 않고 null을 반환한다.
    private AnimatorController BuildAnimatorController(string folder)
    {
        if (motionMode == MotionMode.SingleClip)
        {
            if (singleClip == null) return null;

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{effectName}_Controller.controller");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var state = controller.layers[0].stateMachine.AddState("Play");
            state.motion = singleClip;
            return controller;
        }

        // BlendTree8Way
        if (!dirClips.Any(c => c != null)) return null;

        string blendPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{effectName}_Controller.controller");
        var blendController = AnimatorController.CreateAnimatorControllerAtPath(blendPath);
        blendController.AddParameter(MonsterController.ParamFaceX, AnimatorControllerParameterType.Float);
        blendController.AddParameter(MonsterController.ParamFaceY, AnimatorControllerParameterType.Float);

        blendController.CreateBlendTreeInController("Motion", out BlendTree tree);
        tree.blendType = BlendTreeType.FreeformDirectional2D;
        tree.blendParameter = MonsterController.ParamFaceX;
        tree.blendParameterY = MonsterController.ParamFaceY;

        for (int i = 0; i < dirClips.Length; i++)
        {
            if (dirClips[i] != null)
            {
                tree.AddChild(dirClips[i], MonsterDirections.Vectors[i]);
            }
        }

        return blendController;
    }

    // 이펙트 프리팹: SpriteRenderer + (Animator, 모션이 있을 때만) + Collider2D(Trigger) + SkillEffectDamage
    // + (SkillEffectFacing, 8방향 모드일 때만)
    private GameObject BuildPrefab(AnimatorController controller, string folder)
    {
        var go = new GameObject(effectName);
        go.AddComponent<SpriteRenderer>();

        if (controller != null)
        {
            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            if (motionMode == MotionMode.BlendTree8Way)
            {
                go.AddComponent<SkillEffectFacing>();
            }
        }

        if (colliderShape == ColliderShape.Circle)
        {
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = colliderRadius;
            col.isTrigger = true;
        }
        else
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = colliderSize;
            col.isTrigger = true;
        }

        var dmg = go.AddComponent<SkillEffectDamage>();
        dmg.damage = defaultDamage;

        string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{effectName}.prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        return prefab;
    }
}
