using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    [Header("���� ��������Ʈ 0~9 (������� 10��)")]
    public Sprite[] digitSprites; // digitSprites[0] = "0" �̹���, digitSprites[9] = "9" �̹���

    [Header("5�ڸ��� ǥ���� �̹��� ���� (���� -> ������ ����)")]
    public Image[] digitImages; // ���� 5

    [Header("�ð��� ȹ�� ���� ����")]
    public int scorePerSecond = 2; // 1�ʸ��� ������ ����
    public float scoreInterval = 1f; // �� �ʸ��� ������ �ø���

    private int currentScore = 0;
    private float timer = 0f;

    void Start()
    {
        // ������ �� 00000���� �ʱ�ȭ
        UpdateDigits(0);

        

    }

    void Update()
    {
        if (StageManager.IsGameOver) return; // 게임이 끝난 뒤에는 초당 점수 증가를 멈춘다

        timer += Time.deltaTime;

        if (timer >= scoreInterval)
        {
            timer -= scoreInterval;
            AddScore(scorePerSecond);
        }
    }

    // �ܺο��� ������ �߰��� �� ȣ���ϴ� �Լ�
    // ���߿� ���� óġ ��: scoreDisplay.AddScore(10); ���� ������ ȣ��
    public void AddScore(int amount)
    {
        currentScore += amount;
        currentScore = Mathf.Clamp(currentScore, 0, 99999);
        UpdateDigits(currentScore);
    }

    public int GetScore()
    {
        return currentScore;
    }

    void UpdateDigits(int number)
    {
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
}