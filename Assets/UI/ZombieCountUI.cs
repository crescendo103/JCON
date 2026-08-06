using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 잡은 몬스터 수 / 잡아야 하는 몬스터 수를 "9/15" 형태로 표시한다.
/// ScoreUI/TimeUI와 같은 방식으로, 텍스트가 아니라 숫자 스프라이트 이미지를 갈아끼운다.
/// 자기 자신(zombiecountimg)이 배경 아이콘이고, 하위에 CurrentDigitTens/CurrentDigit/Slash/
/// TotalDigitTens/TotalDigit 다섯 이미지를 둔다(부모의 HorizontalLayoutGroup이 순서대로 배치).
/// 십의 자리(*Tens)는 값이 10 미만이면 꺼서, 한 자리 수는 예전처럼 "9"로만 보이게 한다
/// (스테이지가 올라가 목표 마릿수가 10 이상이 되면서 Mathf.Clamp(0,9)에 걸려 항상 "9"로 보이던
/// 문제 — 두 자리까지 표시할 수 있게 자릿수를 하나씩 더 뒀다).
/// StageManager가 처치 수가 바뀔 때마다 SetCount()를 호출해서 갱신한다.
/// </summary>
public class ZombieCountUI : MonoBehaviour
{
    [Header("숫자 스프라이트 0~9 (인덱스 = 숫자)")]
    public Sprite[] digitSprites = new Sprite[10];

    [Header("구분자( / ) 스프라이트")]
    [Tooltip("아직 지정된 슬래시 스프라이트가 없다면 여기에 직접 넣어준다")]
    public Sprite slashSprite;

    private Image currentDigitTensImage;
    private Image currentDigitImage;
    private Image slashImage;
    private Image totalDigitTensImage;
    private Image totalDigitImage;

    private void Awake()
    {
        // 인스펙터 드래그 연결 대신, 이름으로 찾아 코드로 연결한다 (팀 규칙)
        currentDigitTensImage = transform.Find("CurrentDigitTens")?.GetComponent<Image>();
        currentDigitImage = transform.Find("CurrentDigit")?.GetComponent<Image>();
        slashImage = transform.Find("Slash")?.GetComponent<Image>();
        totalDigitTensImage = transform.Find("TotalDigitTens")?.GetComponent<Image>();
        totalDigitImage = transform.Find("TotalDigit")?.GetComponent<Image>();

        if (slashImage != null && slashSprite != null)
            slashImage.sprite = slashSprite;
    }

    /// <summary>현재 잡은 수 / 목표 마릿수를 갱신해서 표시한다 (0~99까지 두 자리로 표시).</summary>
    public void SetCount(int killed, int total)
    {
        SetDigitPair(currentDigitTensImage, currentDigitImage, killed);
        SetDigitPair(totalDigitTensImage, totalDigitImage, total);
    }

    // 십의 자리는 값이 10 미만이면 오브젝트 자체를 꺼서(HorizontalLayoutGroup이 빈 자리를 자동으로
    // 접어준다) 한 자리 수일 때 예전처럼 자연스럽게 보이게 하고, 10 이상이면 켜서 두 자리로 보여준다.
    private void SetDigitPair(Image tensImage, Image onesImage, int value)
    {
        value = Mathf.Clamp(value, 0, 99);
        int tens = value / 10;
        int ones = value % 10;

        if (tensImage != null)
        {
            bool showTens = tens > 0;
            tensImage.gameObject.SetActive(showTens);
            if (showTens) tensImage.sprite = GetDigitSprite(tens);
        }

        if (onesImage != null)
            onesImage.sprite = GetDigitSprite(ones);
    }

    private Sprite GetDigitSprite(int value)
    {
        int digit = Mathf.Clamp(value, 0, 9);
        return (digitSprites != null && digitSprites.Length > digit) ? digitSprites[digit] : null;
    }
}
