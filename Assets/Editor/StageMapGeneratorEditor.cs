using UnityEditor;
using UnityEngine;

/// <summary>
/// StageMapGenerator 인스펙터에 "콜라이더 기준으로 자동 계산" 버튼을 추가.
/// 버튼을 누르면 buildingPrefabs의 Collider2D를 기준으로
/// buildingSizes / buildingColliderOffsets를 자동으로 채워준다.
/// </summary>
[CustomEditor(typeof(StageMapGenerator))]
public class StageMapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StageMapGenerator generator = (StageMapGenerator)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("건물 Footprint 자동 계산", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "buildingPrefabs 각 프리팹의 Collider2D(트리거 제외)를 기준으로\n" +
            "buildingSizes / buildingColliderOffsets를 자동 계산해서 채웁니다.\n" +
            "셀 크기는 groundTilemap의 Cell Size를 사용합니다 (없으면 1x1로 가정).",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(generator.buildingPrefabs == null || generator.buildingPrefabs.Length == 0))
        {
            if (GUILayout.Button("콜라이더 기준으로 자동 계산"))
            {
                BuildingFootprintCalculator.CalculateAndApply(generator);
            }
        }
    }
}
