using UnityEngine;

/// <summary>
/// 모든 AI 패턴의 부모 클래스.
/// 이 클래스를 상속받는 새 클래스를 추가하면 자동으로 Monster Maker의
/// "AI 패턴" 드롭다운에 나타납니다 (리플렉션으로 자동 탐색).
///
/// 서브클래스는 각자 이름과 같은 별도 .cs 파일에 둬야 한다(AggressiveAI.cs, RangedKiterAI.cs,
/// TileChaserAI.cs, DfsChaserAI.cs, DijkstraChaserAI.cs, PassiveAI.cs). 한 파일에 여러 클래스를
/// 몰아넣으면 MonsterMakerWindow.GetOrCreateAIAsset()이 CreateInstance(Type)으로 즉석 생성해
/// AssetDatabase.CreateAsset으로 저장할 때 파일명과 이름이 다른 클래스의 MonoScript 참조가
/// 제대로 안 잡혀서(m_Script: {fileID: 0}, Missing Script) 저장되는 문제가 있었다 — git pull/빌드가
/// 참조를 끊는 게 아니라 생성되는 순간부터 이미 깨져서 저장된 것이었다.
/// </summary>
public abstract class MonsterAIBehavior : ScriptableObject
{
    public abstract void Execute(MonsterController monster);
}
