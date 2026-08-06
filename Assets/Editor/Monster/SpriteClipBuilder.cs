using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

// 스프라이트 목록으로부터 AnimationClip을 만든다. 프로젝트의 기존 손으로 만든 클립들과 동일하게,
// 루트 오브젝트(SpriteRenderer)의 m_Sprite를 프레임마다 바꾸는 단순한 PPtr 커브 하나만 사용한다.
// Monster Maker가 이 유틸을 이용해, 씬에 임시 오브젝트를 만들고 Animation 창에서 프레임을 찍는
// 수작업 없이도 .anim을 바로 생성할 수 있게 해준다.
public static class SpriteClipBuilder
{
    // sprites: 순서대로 재생할 프레임. fps: 초당 프레임 수. loop: 반복 재생 여부.
    // assetPath: 저장할 경로(같은 경로에 파일이 있으면 고유한 이름으로 자동 변경된다).
    // 유효한 스프라이트가 하나도 없으면 null을 반환한다(호출 쪽에서 기존 값을 유지하도록).
    public static AnimationClip BuildClip(IList<Sprite> sprites, float fps, bool loop, string assetPath)
    {
        var validSprites = sprites?.Where(s => s != null).ToList();
        if (validSprites == null || validSprites.Count == 0) return null;
        if (fps <= 0f) fps = 1f;

        var clip = new AnimationClip { frameRate = fps };

        var keyframes = new ObjectReferenceKeyframe[validSprites.Count];
        for (int i = 0; i < validSprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / fps,
                value = validSprites[i]
            };
        }

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
        AssetDatabase.CreateAsset(clip, uniquePath);
        return clip;
    }
}
