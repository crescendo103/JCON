# JCON
본 프로젝트는 JCON에서 진행하는 팀 프로젝트로, Unity 엔진 기반의 뱀서라이크(Vampire Survivors-like) 장르 게임입니다.

# 개발 규칙
 
## 1. 브랜치
- `main`은 항상 실행 가능한 상태로 유지
- 각자 `feature/이름` 브랜치 만들어서 작업 (예: `feature/player`, `feature/enemy`)
- **머지는 2~3일에 한 번씩 정해진 시간에 모여서 진행** (개인이 임의로 아무 때나 main에 머지 금지)
## 2. 프리팹으로 작업
- 씬(Scene) 파일 하나를 여러 명이 동시에 건드리면 충돌남
- 각자 기능은 **프리팹 단위로 따로 만들고**, 완성된 프리팹만 씬에 배치
- 씬 파일은 최대한 건드리는 사람을 한정시킬 것
## 3. 연결은 코드로
- 인스펙터에서 마우스로 드래그해서 오브젝트/컴포넌트 연결 금지
- `GetComponent`, `Find`, `Awake`/`Start`에서 코드로 참조 연결
```csharp
private Rigidbody2D rb;
 
void Awake()
{
    rb = GetComponent<Rigidbody2D>();
}
```
→ 인스펙터 드래그 연결은 프리팹/씬 파일에 참조 정보가 박혀서 병합 충돌 원인이 됨. 코드로 연결하면 충돌 안 남.
 
