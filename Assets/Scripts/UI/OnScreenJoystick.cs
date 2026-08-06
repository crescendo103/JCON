using UnityEngine;

// 화면 좌측 절반 터치 영역의 이동용 아날로그 조이스틱. 눌린 지점에 배경 원이 순간이동해서 뜨고
// (플로팅), 그 안에서 노브가 드래그를 따라오다 놓으면 배경째로 사라진다. GamePlayerController는
// 이 static Direction만 읽으면 되므로 씬에 이 컴포넌트가 있는지 없는지 신경 쓸 필요가 없다
// (기본값 Vector2.zero라 안전). 재배치/터치 필터링/멀티터치 처리는 OnScreenJoystickBase가 담당한다.
public class OnScreenJoystick : OnScreenJoystickBase
{
    // 노브를 안 잡고 있으면 (0,0). 크기는 0~1의 아날로그 값(끝까지 밀어야 1) — GamePlayerController가
    // 이동 속도에 그대로 곱하므로 살짝만 밀면 천천히 움직인다.
    public static Vector2 Direction { get; private set; }

    protected override void OnDirectionChanged(Vector2 analog) => Direction = analog;
}
