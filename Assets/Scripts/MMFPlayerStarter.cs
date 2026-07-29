using UnityEngine;
using MoreMountains.Feedbacks;

public class MMFPlayerStarter : MonoBehaviour
{
    public MMF_Player Player;
    public bool PlayOnStart = true;
    public bool PlayOnEnable = false;
    public float Delay = 0f;

    private void Awake()
    {
        if (Player == null)
            Player = GetComponent<MMF_Player>();

        if (Player != null)
            Player.Initialization();
    }

    private void Start()
    {
        if (PlayOnStart)
            PlayDelayed();
    }

    private void OnEnable()
    {
        if (PlayOnEnable)
            PlayDelayed();
    }

    private void PlayDelayed()
    {
        if (Player == null) return;

        CancelInvoke(nameof(PlayNow));

        if (Delay <= 0f)
            PlayNow();
        else
            Invoke(nameof(PlayNow), Delay);
    }

    private void PlayNow()
    {
        if (Player == null) return;

        Player.Initialization();   // 모든 Feedback 초기화
        Player.PlayFeedbacks();    // Player 안의 모든 Feedback 실행
    }

    public void Play()
    {
        if (Player == null) return;

        Player.Initialization();
        Player.PlayFeedbacks();
    }

    public void Stop()
    {
        Player?.StopFeedbacks();
    }
}