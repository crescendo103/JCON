using UnityEngine;
// 걷기/달리기 애니메이션에서 왼발 닿는 프레임엔 PlayFootstep1(), 오른발 닿는 프레임엔 PlayFootstep2()를
// 각각 Animation Event(Add Event)로 연결해서 쓴다.
[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepSound : MonoBehaviour
{
    [Tooltip("발자국 소리 1 (예: 왼발)")]
    [SerializeField] private AudioClip footstepClip1;
    [Tooltip("발자국 소리 2 (예: 오른발)")]
    [SerializeField] private AudioClip footstepClip2;
    [SerializeField] private float volume = 1f;
    [Tooltip("매번 피치를 이 범위 안에서 무작위로 살짝 바꿔 단조로운 반복을 줄인다")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayFootstep1()
    {
        PlayClip(footstepClip1);
    }

    public void PlayFootstep2()
    {
        PlayClip(footstepClip2);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, volume * SoundSettings.SfxVolume);
    }
}