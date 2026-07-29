using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 잡은 몬스터 수 / 잡아야 하는 몬스터 수를 "9/15" 형태로 표시한다.
/// ScoreUI/TimeUI와 같은 방식으로, 텍스트가 아니라 숫자 스프라이트 이미지를 갈아끼운다.
/// 자기 자신(zombiecountimg)이 배경 아이콘이고, 하위에 CurrentDigit/Slash/TotalDigit 세 이미지를 둔다.
/// StageManager가 처치 수가 바뀔 때마다 SetCount()를 호출해서 갱신한다.
/// </summary>
public class ZombieCountUI : MonoBehaviour
{
    [Header("숫자 스프라이트 0~9 (인덱스 = 숫자)")]
    public Sprite[] digitSprites = new Sprite[10];

    [Header("구분자( / ) 스프라이트")]
    [Tooltip("아직 지정된 슬래시 스프라이트가 없다면 여기에 직접 넣어준다")]
    public Sprite slashSprite;

    private Image currentDigitImage;
    private Image slashImage;
    private Image totalDigitImage;

    private void Awake()
    {
        // 인스펙터 드래그 연결 대신, 이름으로 찾아 코드로 연결한다 (팀 규칙)
        currentDigitImage = transform.Find("CurrentDigit")?.GetComponent<Image>();
        slashImage = transform.Find("Slash")?.GetComponent<Image>();
        totalDigitImage = transform.Find("TotalDigit")?.GetComponent<Image>();

        if (slashImage != null && slashSprite != null)
            slashImage.sprite = slashSprite;
    }

    /// <summary>현재 잡은 수 / 목표 마릿수를 갱신해서 표시한다 (각각 한 자리 숫자 0~9까지).</summary>
    public void SetCount(int killed, int total)
    {
        if (currentDigitImage != null)
            currentDigitImage.sprite = GetDigitSprite(killed);

        if (totalDigitImage != null)
            totalDigitImage.sprite = GetDigitSprite(total);
    }

    private Sprite GetDigitSprite(int value)
    {
        int digit = Mathf.Clamp(value, 0, 9);
        return (digitSprites != null && digitSprites.Length > digit) ? digitSprites[digit] : null;
    }
}
