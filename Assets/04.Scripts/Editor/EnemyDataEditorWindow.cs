using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>프로젝트 내 모든 EnemyData 에셋을 한 화면에서 검색/편집/생성/복제/삭제하는 툴.</summary>
public class EnemyDataEditorWindow : EditorWindow
{
    const string DefaultAssetFolder = "Assets/05.Data/Enemies";

    List<EnemyData> enemies = new List<EnemyData>();
    Vector2 listScroll;
    Vector2 detailScroll;
    EnemyData selected;
    Editor cachedEditor;

    string searchText = "";
    int tierFilterIndex; // 0 = 전체
    string[] tierOptions;

    [MenuItem("VampireSurvivor/Enemy Data Editor")]
    public static void Open()
    {
        var window = GetWindow<EnemyDataEditorWindow>("Enemy Data Editor");
        window.minSize = new Vector2(680, 420);
        window.RefreshList();
    }

    void OnEnable()
    {
        tierOptions = new[] { "전체" }.Concat(Enum.GetNames(typeof(EnemyTier))).ToArray();
        RefreshList();
    }

    void OnDisable()
    {
        if (cachedEditor != null) DestroyImmediate(cachedEditor);
    }

    void RefreshList()
    {
        enemies.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:EnemyData"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data != null) enemies.Add(data);
        }
        enemies = enemies.OrderBy(e => e.tier).ThenBy(e => e.enemyName).ToList();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawListPanel();
        DrawDetailPanel();
        EditorGUILayout.EndHorizontal();
    }

    void DrawListPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(260));
        EditorGUILayout.LabelField("적 목록", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
        if (GUILayout.Button("새로고침", GUILayout.Width(64))) RefreshList();
        EditorGUILayout.EndHorizontal();

        tierFilterIndex = EditorGUILayout.Popup("등급 필터", tierFilterIndex, tierOptions);

        listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            if (tierFilterIndex > 0 && (int)enemy.tier != tierFilterIndex - 1) continue;
            if (!string.IsNullOrEmpty(searchText) &&
                enemy.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0 &&
                (enemy.enemyName == null || enemy.enemyName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0))
                continue;

            bool isSelected = enemy == selected;
            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = TierColor(enemy.tier);
            string label = $"{enemy.enemyName}  [{enemy.tier}]";
            if (GUILayout.Toggle(isSelected, label, "Button"))
            {
                if (!isSelected) selected = enemy;
            }
            GUI.backgroundColor = prevColor;
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (GUILayout.Button("+ 새 적 데이터 생성"))
        {
            CreateNewEnemyData();
        }

        using (new EditorGUI.DisabledScope(selected == null))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("복제")) DuplicateSelected();
            if (GUILayout.Button("삭제")) DeleteSelected();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    static Color TierColor(EnemyTier tier)
    {
        switch (tier)
        {
            case EnemyTier.Elite: return new Color(0.65f, 0.85f, 1f);
            case EnemyTier.Boss: return new Color(1f, 0.7f, 0.4f);
            case EnemyTier.Reaper: return new Color(1f, 0.45f, 0.45f);
            default: return Color.white;
        }
    }

    void DrawDetailPanel()
    {
        EditorGUILayout.BeginVertical();

        if (selected == null)
        {
            EditorGUILayout.HelpBox("왼쪽 목록에서 적 데이터를 선택하거나 새로 생성하세요.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        Editor.CreateCachedEditor(selected, null, ref cachedEditor);
        var so = cachedEditor.serializedObject;
        so.Update();

        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

        EditorGUILayout.LabelField(selected.name, EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawSection("기본 정보", () =>
        {
            EditorGUILayout.PropertyField(so.FindProperty("enemyName"));
            EditorGUILayout.PropertyField(so.FindProperty("tier"));
            EditorGUILayout.PropertyField(so.FindProperty("gemGrade"));
        });

        DrawSection("비주얼", () =>
        {
            var spriteProp = so.FindProperty("sprite");
            EditorGUILayout.PropertyField(spriteProp);
            if (spriteProp.objectReferenceValue != null)
            {
                var preview = AssetPreview.GetAssetPreview(spriteProp.objectReferenceValue);
                if (preview != null) GUILayout.Label(preview, GUILayout.Width(64), GUILayout.Height(64));
            }
            EditorGUILayout.PropertyField(so.FindProperty("placeholderColor"));
            EditorGUILayout.PropertyField(so.FindProperty("visualScale"));
        });

        DrawSection("전투 스탯", () =>
        {
            EditorGUILayout.PropertyField(so.FindProperty("baseHealth"));
            EditorGUILayout.PropertyField(so.FindProperty("contactDamage"));
            EditorGUILayout.PropertyField(so.FindProperty("moveSpeed"));
            EditorGUILayout.PropertyField(so.FindProperty("contactInterval"));
        });

        if (so.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(selected);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("프로젝트 창에서 위치 보기(Ping)"))
        {
            EditorGUIUtility.PingObject(selected);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    static void DrawSection(string title, Action drawer)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        drawer();
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }

    void CreateNewEnemyData()
    {
        EnsureFolderExists(DefaultAssetFolder);

        var asset = ScriptableObject.CreateInstance<EnemyData>();
        asset.enemyName = "New Enemy";

        string path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultAssetFolder}/Enemy_New.asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RefreshList();
        selected = asset;
        EditorGUIUtility.PingObject(asset);
    }

    void DuplicateSelected()
    {
        if (selected == null) return;

        string sourcePath = AssetDatabase.GetAssetPath(selected);
        string newPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);
        AssetDatabase.CopyAsset(sourcePath, newPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RefreshList();
        selected = AssetDatabase.LoadAssetAtPath<EnemyData>(newPath);
        EditorGUIUtility.PingObject(selected);
    }

    void DeleteSelected()
    {
        if (selected == null) return;

        string path = AssetDatabase.GetAssetPath(selected);
        if (EditorUtility.DisplayDialog("적 데이터 삭제", $"'{selected.name}'을(를) 삭제하시겠습니까?\n{path}", "삭제", "취소"))
        {
            AssetDatabase.DeleteAsset(path);
            selected = null;
            RefreshList();
        }
    }

    static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        var parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
