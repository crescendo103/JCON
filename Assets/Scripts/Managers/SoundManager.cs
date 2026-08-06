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

    [Tooltip("SoundSettings.BgmVolume(0~1 슬라이더) 위에 그대로 더해지는 값. 슬라이더가 0(무음)이어도 이 값만큼은 항상 들리게 키울 수 있다. 음수를 주면 반대로 줄어든다(0 밑으로는 안 내려감)")]
    [SerializeField] private float volumeMultiplier = 0.5f;

    [Tooltip("스코어보드(결과 화면)가 뜰 때 재생할 사운드. AudioListener.pause로 나머지 소리를 다 꺼도 이것만 들린다")]
    public AudioClip scoreboardSfx;

    [Tooltip("SoundSettings.SfxVolume(0~1 슬라이더) 위에 그대로 더해지는 값(volumeMultiplier와 같은 방식). 음수를 주면 줄어든다(0 밑으로는 안 내려감)")]
    [SerializeField] private float scoreboardVolumeMultiplier = 0.3f;

    private AudioSource audioSource;
    // 스테이지 종료 시 AudioListener.pause = true로 나머지 사운드를 전부 끄기 때문에,
    // 스코어보드 사운드는 그 영향을 받지 않는 별도의 소스(ignoreListenerPause)로 재생한다.
    private AudioSource scoreboardAudioSource;

    private void Awake()
    {
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = Mathf.Max(0f, volumeMultiplier + SoundSettings.BgmVolume);

        scoreboardAudioSource = gameObject.AddComponent<AudioSource>();
        scoreboardAudioSource.playOnAwake = false;
        scoreboardAudioSource.ignoreListenerPause = true;

        // 사운드 설정 캔버스의 배경음악 슬라이더를 움직이면 재생 중인 곡에도 바로 반영한다.
        SoundSettings.BgmVolumeChanged += ApplyBgmVolume;
    }

    private void OnDestroy()
    {
        SoundSettings.BgmVolumeChanged -= ApplyBgmVolume;
    }

    private void ApplyBgmVolume(float bgmVolume)
    {
        audioSource.volume = Mathf.Max(0f, volumeMultiplier + bgmVolume);
    }

    private void Start()
    {
        PlayBgmForCurrentStage();
    }

    private void Update()
    {
        // volumeMultiplier는 원래 Awake()에서 한 번만 반영돼서, Play 모드가 시작된 뒤 인스펙터에서
        // 숫자를 바꿔도 다시 시작하기 전까지는 소리 크기가 그대로였다. 매 프레임 다시 더해줘서
        // 인스펙터에서 값을 바꾸는 즉시(Play 중에도) 반영되게 한다.
        if (audioSource != null)
            audioSource.volume = Mathf.Max(0f, volumeMultiplier + SoundSettings.BgmVolume);
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

    /// <summary>스테이지 종료(시간초과/전멸) 시 StageManager가 호출한다. AudioListener.pause와 무관하게 들린다.</summary>
    public void PlayScoreboardSfx()
    {
        if (scoreboardSfx == null || scoreboardAudioSource == null)
            return;

        // 다른 효과음들과 같이 SoundSettings.SfxVolume(설정 슬라이더)을 따르면서, scoreboardVolumeMultiplier를
        // 그 위에 더해서 이 사운드만 따로 키우거나 줄인다(슬라이더가 0이어도 이 값만큼은 들리게 할 수 있다).
        scoreboardAudioSource.PlayOneShot(scoreboardSfx, Mathf.Max(0f, scoreboardVolumeMultiplier + SoundSettings.SfxVolume));
    }
}
