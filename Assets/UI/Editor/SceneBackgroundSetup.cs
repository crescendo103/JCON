using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SceneBackgroundSetup
{
    private const string SpriteAPath = "Assets/UI/scenebackground.png";
    private const string SpriteBPath = "Assets/UI/scenebackground (1).png";
    private const float TileWidth = 13.76f;    // 1376px / PPU 100
    private const float LeftMargin = 20f;

    [MenuItem("Tools/Setup Scrolling Background")]
    public static void Setup()
    {
        if (GameObject.Find("BG_Root") != null)
        {
            Debug.LogWarning("BG_Root가 이미 씬에 있습니다. 중복 생성을 막기 위해 중단합니다.");
            return;
        }

        var spriteA = LoadSprite(SpriteAPath);
        var spriteB = LoadSprite(SpriteBPath);
        if (spriteA == null || spriteB == null)
        {
            Debug.LogError("스프라이트를 찾을 수 없습니다. scenebackground.png / scenebackground (1).png 경로를 확인하세요.");
            return;
        }

        var root = new GameObject("BG_Root");
        Undo.RegisterCreatedObjectUndo(root, "Create BG_Root");

        var tileA = CreateTile("scenebackground", spriteA, 0f, root.transform);
        var tileB = CreateTile("scenebackground (1)", spriteB, TileWidth, root.transform);

        // 서로를 리셋 대상으로 참조: 왼쪽으로 카메라 밖까지 나가면 상대 타일의 시작 위치로 순간이동
        tileA.GetComponent<SceneMover>().resetTarget = tileB.transform;
        tileB.GetComponent<SceneMover>().resetTarget = tileA.transform;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log("배경 타일 2장 생성 및 SceneMover 세팅 완료 (BG_Root/scenebackground, scenebackground (1))");
    }

    private static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        // Multiple 모드로 임포트된 텍스처는 서브 애셋에서 Sprite를 찾아야 한다
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault();
    }

    private static GameObject CreateTile(string name, Sprite sprite, float x, Transform parent)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, 0f, 0f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        var mover = go.AddComponent<SceneMover>();
        mover.moveSpeed = 2f;
        mover.leftMargin = LeftMargin;

        return go;
    }
}
