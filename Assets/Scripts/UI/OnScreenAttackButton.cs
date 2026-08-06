using UnityEngine;

// 화면 우측 절반 터치 영역의 공격 조이스틱. 이동 조이스틱(OnScreenJoystick)과 완전히 같은 방식으로
// 눌린 지점에 배경 원이 떠서 그 안에서 노브를 드래그해 공격 방향을 직접 조정한다. 동시에 누르고
// 있는 동안은 마우스 왼쪽 버튼과 동등한 공격 입력(Held/눌림)도 겸한다 — GamePlayerController.Click()이
// 이 값을 함께 읽는다. 재배치/터치 필터링/멀티터치 처리는 OnScreenJoystickBase가 담당한다.
public class OnScreenAttackButton : OnScreenJoystickBase
{
    // Input.GetKey(Mouse0)에 대응 — 누르고 있는 동안 계속 true (연사/연타 무기용).
    public static bool Held { get; private set; }

    // 안 누르고 있으면 (0,0). OnScreenJoystick.Direction과 같은 규칙: 크기 0~1의 아날로그 값이고,
    // 데드존 안이면 0으로 취급한다. GamePlayerController.GetAimDirection()이 이 값을 조준으로 쓴다.
    public static Vector2 Direction { get; private set; }

    // Input.GetKeyDown(Mouse0)에 대응 — 한 번 읽으면 바로 false로 리셋된다.
    // Update당 정확히 한 번만 ConsumePress()를 호출해야 GetKeyDown과 같은 "이번 프레임에 눌렸는가"
    // 의미가 유지된다(GamePlayerController.Click()이 매 프레임 정확히 한 번 호출).
    private static bool pressedThisFrame;

    public static bool ConsumePress()
    {
        if (!pressedThisFrame) return false;
        pressedThisFrame = false;
        return true;
    }

    protected override void OnPressed()
    {
        Held = true;
        pressedThisFrame = true;
    }

    protected override void OnReleased()
    {
        Held = false;
    }

    protected override void OnDirectionChanged(Vector2 analog) => Direction = analog;
}
