using UnityEngine;

[CreateAssetMenu(fileName = "PassiveData", menuName = "VampireSurvivor/Passive Data")]
public class PassiveData : ScriptableObject
{
    public string passiveName;
    public PassiveType type;
    public Sprite icon;
    public Color placeholderColor = Color.white;
    [TextArea] public string description;
    public int maxLevel = 8;

    [Tooltip("레벨별 누적(총량) 보너스 값. %가 필요한 스탯은 % 그대로 입력 (예: 10 = +10%).")]
    public float[] levels;
}
