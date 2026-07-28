using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "VampireSurvivor/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public Sprite sprite;
    public Color placeholderColor = Color.white;
    public float visualScale = 1f;

    public float baseHealth = 20f;
    public float contactDamage = 8f;
    public float moveSpeed = 1.2f;
    public float contactInterval = 0.7f;

    public GemGrade gemGrade = GemGrade.Small;
    public EnemyTier tier = EnemyTier.Basic;
}
