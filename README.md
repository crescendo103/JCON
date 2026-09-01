# JCON

본 프로젝트는 **JCON에서 진행하는 팀 프로젝트**로,
**Unity 엔진 기반의 뱀서라이크(Vampire Survivors-like) 장르 게임**입니다.

---

## 📚 기술 문서

프로젝트의 시스템 구조, 개발 과정 및 기술 구현 내용을 정리한 문서입니다.

### 📄 기술 문서 PDF

> 프로젝트 개발에 필요한 기술 문서를 PDF로 제공합니다.

**[📘 기술 문서 보기](./Docs/기술문서.pdf)**

---

## 📌 개발 규칙

<details>
<summary><strong>개발 규칙 펼치기</strong></summary>

### 1. 브랜치

* `main`은 항상 실행 가능한 상태로 유지
* 각자 `feature/이름` 브랜치를 만들어서 작업

  * 예: `feature/player`
  * 예: `feature/enemy`
* **머지는 2~3일에 한 번씩 정해진 시간에 모여서 진행**
* 개인이 임의로 `main`에 머지하지 않음

### 2. 프리팹으로 작업

* 씬(Scene) 파일을 여러 명이 동시에 수정하면 충돌이 발생할 수 있음
* 각자 담당 기능은 **프리팹 단위로 분리하여 작업**
* 완성된 프리팹만 씬에 배치
* 씬 파일은 최대한 수정하는 인원을 제한

### 3. 연결은 코드로

* 인스펙터에서 마우스로 드래그하여 오브젝트/컴포넌트를 연결하지 않음
* `GetComponent`, `Find`, `Awake`, `Start` 등을 활용하여 코드에서 참조 연결

```csharp
private Rigidbody2D rb;

void Awake()
{
    rb = GetComponent<Rigidbody2D>();
}
```

> 인스펙터를 통한 직접 참조는 프리팹/씬 파일에 참조 정보가 저장되어
> Git 병합 과정에서 충돌이 발생할 수 있으므로 코드에서 연결하는 것을 원칙으로 함.

</details>

---

## 🎮 Game

### ZombieRush

[Google Play](https://play.google.com/store/apps/details?id=com.jcon.zombierush&utm_source=chatgpt.com)

---
