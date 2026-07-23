using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// 스킬 이펙트 프리팹에 동적으로 붙어 AnimationClip 하나를 재생한다.
// Animator 상태머신을 미리 만들어두지 않아도 Playables API로 임의의 클립을 바로 재생할 수 있다.
public class SkillEffectAnimationRunner : MonoBehaviour
{
    private PlayableGraph graph;

    public void Play(AnimationClip clip)
    {
        var animator = GetComponent<Animator>();
        if (animator == null) animator = gameObject.AddComponent<Animator>();

        graph = PlayableGraph.Create($"{clip.name}_SkillEffect");
        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        var output = AnimationPlayableOutput.Create(graph, "SkillEffectOutput", animator);
        output.SetSourcePlayable(clipPlayable);
        graph.Play();
    }

    void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
