using UnityEngine;
using UnityEngine.UI;

public class TimeUI : MonoBehaviour
{
    [Header("���� ��������Ʈ 0~9 (������� 10��)")]
    public Sprite[] digitSprites; // digitSprites[0] = "0" �̹���, digitSprites[9] = "9" �̹���

    [Header("5�ڸ��� ǥ���� �̹��� ���� (���� -> ������ ����)")]
    public Image[] digitImages; // ���� 5, ��: ��/õ/��/��/���� �ڸ�

    [Header("Ÿ�̸� ����")]
    public float startTime = 300f; // ���� �ð�(��)

    private float remainingTime;

    void Start()
    {
        remainingTime = startTime;
        UpdateDigits(Mathf.CeilToInt(remainingTime));
    }

    void Update()
    {
        if (StageManager.IsGameOver) return; // 좀비를 다 잡아 스테이지가 이미 끝났으면 시간도 멈춘다
        if (remainingTime <= 0f) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime < 0f)
            remainingTime = 0f;

        int displaySeconds = Mathf.CeilToInt(remainingTime);
        UpdateDigits(displaySeconds);

        if (remainingTime <= 0f)
        {
            OnTimeUp();
        }
    }

    void OnTimeUp()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.NotifyTimeUp();
    }

    void UpdateDigits(int number)
    {
        // 5�ڸ��� ���� (00000 ~ 99999)
        number = Mathf.Clamp(number, 0, 99999);

        int[] digits = new int[5];
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            digits[i] = number % 10;
            number /= 10;
        }

        for (int i = 0; i < digitImages.Length; i++)
        {
            digitImages[i].sprite = digitSprites[digits[i]];
        }
    }

    // �ܺο��� ���� �ð��� �� ���� ������ ������ �� ��� (TotalUI ��� ����)
    public int GetRemainingSeconds()
    {
        return Mathf.CeilToInt(remainingTime);
    }
}