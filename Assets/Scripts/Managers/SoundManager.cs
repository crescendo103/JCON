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

    [Tooltip("SoundSettings.BgmVolume(0~1) 슬라이더가 0일 때도 최소한 이만큼(0~1)은 들리게 하는 " +
             "하한값. Awake/ApplyBgmVolume/Update가 이 값과 1 사이를 슬라이더 값으로 선형 보간(Lerp)" +
             "한다 — 예전엔 이 값을 슬라이더에 그냥 더하기만 해서 AudioSource.volume의 [0,1] " +
             "클램프에 걸려 슬라이더 위쪽 구간이 통째로 죽었다(WeaponPickup.cs의 PlayClipAtPointAmplified " +
             "주석이 같은 클램프 함정을 설명한다). Lerp로 바꿔 슬라이더 전 구간이 고르게 반영되게 한다")]
    [SerializeField, Range(0f, 1f)] private float minBgmVolume = 0.5f;

    [Tooltip("슬라이더가 최대(1)일 때 배경음악이 도달할 수 있는 실질 볼륨 배율. 1을 넘겨도 " +
             "AudioSource.volume 자체는 엔진이 1로 클램프해버려서(WeaponPickup.PlayClipAtPointAmplified 주석 참고) " +
             "그냥 숫자만 키워선 소리가 커지지 않는다 — 1을 넘는 구간은 OnAudioFilterRead에서 " +
             "샘플에 직접 곱해 증폭한다(아래 extraGain). 값을 키울수록 최대 음량에서 소리가 커지지만 " +
             "너무 크면 찢어지는(클리핑) 소리가 날 수 있다")]
    [SerializeField, Range(1f, 3f)] private float maxBgmVolume = 1.8f;

    [Tooltip("스코어보드(결과 화면)가 뜰 때 재생할 사운드. AudioListener.pause로 나머지 소리를 다 꺼도 이것만 들린다")]
    public AudioClip scoreboardSfx;

    [Tooltip("SoundSettings.SfxVolume(0~1) 슬라이더가 0이어도 스코어보드 사운드만큼은 최소 이 " +
             "정도(0~1)는 들리게 하는 하한값. minBgmVolume과 같은 방식(Lerp)으로 적용된다")]
    [SerializeField, Range(0f, 1f)] private float minScoreboardVolume = 0.3f;

    [Tooltip("슬라이더가 최대일 때 스코어보드 사운드의 볼륨 배율. PlayOneShot의 volumeScale은 " +
             "AudioSource.volume과 달리 1을 넘겨도 클램프되지 않고 실제로 증폭되므로 maxBgmVolume과 " +
             "달리 별도 증폭 코드 없이 이 값만으로 더 커진다")]
    [SerializeField, Range(1f, 3f)] private float maxScoreboardVolume = 1.5f;

    // BGM 볼륨이 maxBgmVolume(>1)까지 올라갔을 때 1을 넘는 만큼(=AudioSource.volume이 클램프해서 못 낸 만큼)을
    // OnAudioFilterRead에서 샘플에 곱해 실제로 증폭시키는 배율. 오디오 스레드에서 읽으므로 float 하나만 캐시해 둔다.
    private float extraGain = 1f;

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
        SetBgmVolume(SoundSettings.BgmVolume);

        // BGM용 AudioSource와 같은 GameObject에 두면 안 된다 — OnAudioFilterRead(extraGain 증폭용)는
        // "한 GameObject에 AudioSource/Listener가 여러 개면 그중 하나에만 적용된다"는 제약이 있어서
        // (Unity 콘솔 경고: "GameObject has multiple AudioSources..."), 같이 두면 스코어보드 소리까지
        // BGM용 증폭 필터를 함께 타 버리거나 어느 쪽에 적용될지 불확실해진다. 별도 자식 오브젝트로 분리한다.
        var scoreboardGO = new GameObject("ScoreboardAudioSource");
        scoreboardGO.transform.SetParent(transform, false);
        scoreboardAudioSource = scoreboardGO.AddComponent<AudioSource>();
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
        SetBgmVolume(bgmVolume);
    }

    // Lerp 결과가 1 이하인 구간은 그대로 audioSource.volume에 반영하고(엔진이 이 범위는 클램프하지 않는다),
    // 1을 넘는 구간은 audioSource.volume을 1로 고정한 채 그 초과분만 extraGain으로 넘겨
    // OnAudioFilterRead가 샘플을 직접 증폭하게 한다.
    private void SetBgmVolume(float bgmVolume)
    {
        float target = Mathf.Lerp(minBgmVolume, maxBgmVolume, bgmVolume);
        audioSource.volume = Mathf.Min(target, 1f);
        extraGain = Mathf.Max(target, 1f);
    }

    // audioSource가 재생 중인 소리가 스피커로 나가기 직전에 호출된다. data에는 이미 audioSource.volume이
    // 반영된 샘플이 들어있으므로, 여기서 extraGain(>=1)을 곱하면 그 클램프 한계(1) 너머로 실제 증폭된다.
    // extraGain이 1이면(= 슬라이더가 maxBgmVolume 이하 구간에 있으면) 곱해도 값이 그대로라 사실상 공짜다.
    private void OnAudioFilterRead(float[] data, int channels)
    {
        float gain = extraGain;
        if (gain <= 1f) return;

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Mathf.Clamp(data[i] * gain, -1f, 1f);
        }
    }

    private void Start()
    {
        PlayBgmForCurrentStage();
    }

    private void Update()
    {
        // minBgmVolume은 원래 Awake()에서 한 번만 반영돼서, Play 모드가 시작된 뒤 인스펙터에서
        // 숫자를 바꿔도 다시 시작하기 전까지는 소리 크기가 그대로였다. 매 프레임 다시 반영해서
        // 인스펙터에서 값을 바꾸는 즉시(Play 중에도) 반영되게 한다.
        if (audioSource != null)
            SetBgmVolume(SoundSettings.BgmVolume);
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

        // 슬라이더가 0이어도 최소 minScoreboardVolume만큼은 들리게, 1일 때는 완전한 최대 볼륨까지
        // 고르게 이어지도록 Lerp로 보간한다. 그냥 더해서 클램프하면(예전 방식) 슬라이더 위쪽
        // 구간이 죽는다 — minBgmVolume 주석 참고.
        scoreboardAudioSource.PlayOneShot(scoreboardSfx, Mathf.Lerp(minScoreboardVolume, maxScoreboardVolume, SoundSettings.SfxVolume));
    }
}