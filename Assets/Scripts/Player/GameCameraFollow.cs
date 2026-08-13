using UnityEngine;
using DG.Tweening;

// 플레이어를 따라다니는 카메라. 2D 게임이므로 X/Y만 목표 위치를 따라가고
// Z는 카메라 원래 값(깊이)을 그대로 유지한다. SmoothDamp로 부드럽게 뒤쫓아간다.
public class GameCameraFollow : MonoBehaviour
{
    [Header("추적 대상 (비워두면 \"Player\" 태그로 자동 탐색)")]
    public Transform target;

    [Header("추적 방식")]
    [Tooltip("값이 작을수록 카메라가 더 빠르게(딱 붙어서) 따라간다")]
    public float smoothTime = 0.05f;
    public Vector2 offset = Vector2.zero;

    [Header("피격 카메라 흔들림 (DOTween Punch)")]
    [Tooltip("타겟에 GamePlayerController 또는 PlayerHealth가 있으면 피격 시 자동으로 흔들린다")]
    public bool shakeOnPlayerDamaged = true;
    public float shakeDuration = 0.3f;
    public float shakeStrength = 0.4f;
    public int shakeVibrato = 20;
    [Range(0f, 1f)] public float shakeElasticity = 0.5f;

    private Vector3 velocity = Vector3.zero;
    private float fixedZ;

    // SmoothDamp가 추적하는 "순수 추적 위치". transform.position에는 여기에 흔들림 오프셋을 더한 값이 들어간다.
    // (흔들림 자체가 다시 SmoothDamp 입력으로 들어가면 흔들림을 따라가려고 해서 이상하게 수렴함)
    private Vector3 currentFollowPos;
    private Vector3 shakeOffset = Vector3.zero;
    private Tweener shakeTween;
    private PlayerHealth subscribedHealth;
    private GamePlayerController subscribedController;

    private void Awake()
    {
        fixedZ = transform.position.z;
        currentFollowPos = transform.position;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        if (!shakeOnPlayerDamaged || target == null)
            return;

        // 실제 플레이어 구현이 프로젝트 내에 두 갈래(GamePlayerController / PlayerHealth)로 존재해서 둘 다 시도한다.
        if (target.TryGetComponent(out GamePlayerController controller))
        {
            subscribedController = controller;
            subscribedController.OnHit += HandlePlayerHit;
        }
        else if (target.TryGetComponent(out PlayerHealth health))
        {
            subscribedHealth = health;
            subscribedHealth.OnDamaged += HandlePlayerDamaged;
        }
    }

    private void OnDestroy()
    {
        if (subscribedController != null)
            subscribedController.OnHit -= HandlePlayerHit;

        if (subscribedHealth != null)
            subscribedHealth.OnDamaged -= HandlePlayerDamaged;

        shakeTween?.Kill();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, fixedZ);
        currentFollowPos = Vector3.SmoothDamp(currentFollowPos, desired, ref velocity, smoothTime);

        // 픽셀 그리드 스냅은 카메라에 붙어있는 PixelPerfectCamera 컴포넌트가 렌더링 단계에서
        // 이미 처리한다. 여기서 또 Mathf.Round로 수동 스냅하면 두 스냅이 겹쳐(pixelRatio만큼
        // 증폭되어) 카메라가 움직일 때마다 계단식으로 튀어 흔들리는 것처럼 보이는 원인이 된다.
        transform.position = currentFollowPos + shakeOffset;
    }

    private void HandlePlayerHit(float amount)
    {
        Shake(shakeDuration, shakeStrength, shakeVibrato, shakeElasticity);
    }

    private void HandlePlayerDamaged(float amount, Vector2 sourcePosition)
    {
        Shake(shakeDuration, shakeStrength, shakeVibrato, shakeElasticity);
    }

    /// <summary>DOTween Punch로 카메라를 흔든다. 인자를 생략하면 인스펙터 기본값 사용.</summary>
    public void Shake(float duration = -1f, float strength = -1f, int vibrato = -1, float elasticity = -1f)
    {
        if (duration < 0f) duration = shakeDuration;
        if (strength < 0f) strength = shakeStrength;
        if (vibrato < 0) vibrato = shakeVibrato;
        if (elasticity < 0f) elasticity = shakeElasticity;

        shakeTween?.Kill();
        shakeOffset = Vector3.zero;
        shakeTween = DOTween.Punch(
                () => shakeOffset,
                v => shakeOffset = v,
                new Vector3(strength, strength, 0f),
                duration,
                vibrato,
                elasticity)
            .SetUpdate(true) // 타임스케일이 0이어도(예: 히트스톱) 흔들리도록 unscaled time 사용
            .OnComplete(() => shakeOffset = Vector3.zero);
    }
}
