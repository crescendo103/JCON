using UnityEngine;
using UnityEngine.UI;

public class StartUIChanger : MonoBehaviour
{
    [Header("이미지")]
    public Image targetImage;              // 교체될 이미지
    public Sprite[] tierSprites = new Sprite[4]; // 단계별 스프라이트 (낮은 단계 -> 높은 단계 순)

    [Header("조건 값")]
    public float[] thresholds = new float[4]; // 각 스프라이트로 바뀌는 기준값 (예: 0, 50, 100, 200)

    [Header("참조")]
    public float score;   // 외부에서 갱신해줄 스코어
    public float timeElapsed; // 외부에서 갱신해줄 시간

    private int currentTier = -1; // 현재 적용된 단계 (중복 교체 방지용)
    

    void UpdateSprite(float value)
    {
        int newTier = 0;

        // thresholds를 낮은 순서대로 확인, value가 넘는 가장 높은 단계를 찾음
        for (int i = 0; i < thresholds.Length; i++)
        {
            if (value >= thresholds[i])
                newTier = i;
        }

        // 단계가 바뀌었을 때만 스프라이트 교체 (매 프레임 불필요한 할당 방지)
        if (newTier != currentTier)
        {
            currentTier = newTier;
            targetImage.sprite = tierSprites[currentTier];
        }
    }

    // 외부에서 스코어를 올릴 때 호출
    public void AddScore(float amount)
    {
        score += amount;
    }
}