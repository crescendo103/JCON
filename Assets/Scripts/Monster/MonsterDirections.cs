using UnityEngine;

// 8방향 순서 및 2D Freeform Directional 좌표 (N, NE, E, SE, S, SW, W, NW).
// MonsterMakerWindow와 SkillMakerWindow가 공통으로 사용하는 단일 출처.
public static class MonsterDirections
{
    public static readonly string[] Labels =
    {
        "N (위)", "NE (오른쪽 위)", "E (오른쪽)", "SE (오른쪽 아래)",
        "S (아래)", "SW (왼쪽 아래)", "W (왼쪽)", "NW (왼쪽 위)"
    };

    public static readonly Vector2[] Vectors =
    {
        new Vector2(0f, 1f), new Vector2(0.7071f, 0.7071f), new Vector2(1f, 0f), new Vector2(0.7071f, -0.7071f),
        new Vector2(0f, -1f), new Vector2(-0.7071f, -0.7071f), new Vector2(-1f, 0f), new Vector2(-0.7071f, 0.7071f)
    };

    // 방향별 파일명 접미사 (Labels/Vectors와 같은 순서)
    public static readonly string[] FileSuffixes =
    {
        "up", "up_right", "right", "down_right", "down", "down_left", "left", "up_left"
    };
}
