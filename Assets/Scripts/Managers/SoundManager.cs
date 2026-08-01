using UnityEngine;

/// <summary>
/// MapBuildScene 전용 배경음악(BGM) 재생 매니저. 이 씬 안에만 배치해서 쓴다.
/// UINavigator.OpenStageScene()이 스테이지에 들어갈 때마다 이 씬을 통째로 다시 로드하므로,
/// 씬이 열릴 때마다 새로 생성되고 씬을 나가면 같이 파괴되는 것으로 충분하다
/// (DontDestroyOnLoad/씬 전환 감지가 필요 없다. UIScene의 BGM은 별도로 관리한다).
/// StageProgressManager의 CurrentStage를 bgmList 배열 크기로 나눈 나머지를 인덱스로 삼아
/// 그 트랙을 재생한다(스테이지 수가 곡 수보다 많아도 배열을 처음부터 순환하며 재생된다).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Tooltip("스테이지별로 순환 재생할 배경음악 목록. 인덱스 = (CurrentStage - 1) % bgmList.Length")]
    public AudioClip[] bgmList;

    [Tooltip("AudioSource.volume 배율. 인스펙터 슬라이더는 1이 최대라 원본 클립보다 크게 키우려면 코드로 1을 넘겨야 한다")]
    [SerializeField] private float volumeMultiplier = 2f;

    private AudioSource audioSource;

    private void Awake()
    {
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volumeMultiplier;
    }

    private void Start()
    {
        PlayBgmForCurrentStage();
    }

    /// <summary>
    /// StageProgressManager.CurrentStage를 bgmList 크기로 나눈 나머지를 인덱스로 삼아 재생한다.
    /// </summary>
    public void PlayBgmForCurrentStage()
    {
        if (bgmList == null || bgmList.Length == 0)
            return;

        int stage = StageProgressManager.Instance.CurrentStage;
        int index = (stage - 1) % bgmList.Length;

        PlayBgm(bgmList[index]);
    }

    /// <summary>같은 곡이 이미 재생 중이면 다시 시작하지 않고 그대로 이어서 재생한다.</summary>
    public void PlayBgm(AudioClip clip)
    {
        if (clip == null)
            return;

        if (audioSource.clip == clip && audioSource.isPlaying)
            return;

        audioSource.clip = clip;
        audioSource.Play();
    }

    public void StopBgm()
    {
        audioSource.Stop();
    }
}
