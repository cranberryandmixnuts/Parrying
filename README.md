# Parrying

공격 능력 없이 **패링만으로 전투를 해결하며 앞으로 나아가는 사이드뷰 2D 액션 게임**입니다.  
플레이어는 직접 공격하지 않고, 적의 공격을 받아내며 에너지를 쌓고, 에너지를 소모한 **강화 패링**을 성공시켜 적에게 피해를 주는 구조로 설계했습니다.

## 게임 개요

| 항목 | 내용 |
| --- | --- |
| 장르 | 사이드뷰 2D 액션 |
| 핵심 조작 | 이동, 점프, 대시, 패링, 강화 패링 |
| 전투 방식 | 직접 공격 없음. 적의 공격을 패링해 전투 진행 |
| 핵심 목표 | 적의 공격 패턴을 읽고, 패링 타이밍을 맞춰 에너지를 쌓은 뒤 강화 패링으로 적을 제압 |
| 개발 환경 | Unity 6, C# |
| 주요 구현 범위 | 플레이어 상태 구조, 패링 판정, 강화 패링, 일반 적 AI, 보스 패턴, 입력/키 리바인딩, 전투 VFX/SFX 연동 |

## 핵심 기획

Parrying의 전투는 일반적인 공격 버튼 중심 액션이 아니라, **방어 행동인 패링을 공격 수단으로 전환하는 방식**을 중심으로 구성했습니다.

플레이어는 기본 공격 능력이 없기 때문에 적을 먼저 공격할 수 없습니다. 대신 적의 공격을 유도하고, 공격 타이밍에 맞춰 패링을 성공시키면 에너지를 획득합니다. 충분한 에너지를 확보한 뒤 강화 패링을 성공시키면 적에게 피해를 줄 수 있습니다.

이 구조를 통해 전투의 초점은 다음 요소에 맞춰집니다.

- 적의 공격 거리와 공격 타이밍 파악
- 일반 패링과 강화 패링의 사용 시점 판단
- 패링 성공 후 위치 변화와 다음 행동 연결
- 공격을 피하는 게임이 아니라, 공격을 받아내며 진행하는 전투 흐름

## 플레이 루프

```text
적 접근
  ↓
적 공격 유도
  ↓
패링 타이밍 판정
  ↓
패링 성공 시 에너지 획득
  ↓
에너지 축적
  ↓
강화 패링 성공
  ↓
적 피해 / 처치
  ↓
다음 구간 진행
```

## 주요 시스템

### 1. 플레이어 상태 구조

플레이어는 상태 머신 기반으로 동작합니다.  
이동, 피격, 사망, 일반 패링, 강화 패링 등의 상태를 분리하여 각 상태가 자신의 진입, 갱신, 물리 갱신, 종료 처리를 담당하도록 구성했습니다.

주요 파일:

```text
Assets/Core/Scripts/Player/PlayerController.cs
Assets/Core/Scripts/Player/PlayerStateMachine.cs
Assets/Core/Scripts/Player/PlayerState.cs
Assets/Core/Scripts/Player/States/
```

이 구조를 통해 플레이어의 이동 로직과 전투 로직이 한 파일에 과도하게 섞이지 않도록 했고, 패링처럼 독립적인 판정 흐름을 가진 기능을 별도 상태로 관리할 수 있게 했습니다.

### 2. 일반 패링

일반 패링은 적의 공격과 플레이어의 패링 판정 범위가 겹쳤을 때 후보를 등록하고, 패링 상태 안에서 해당 후보를 처리하는 방식으로 구현했습니다.

패링 판정은 성공 타이밍에 따라 나뉩니다.

| 판정 | 결과 |
| --- | --- |
| 정확한 타이밍 패링 | 에너지 획득, 무적 시간 확보, 적 공격 반응 발생 |
| 늦은 패링 | 일부 피해를 받지만 에너지 획득 |
| 실패 | 적 공격에 피격 |

공중 패링 성공 시에는 넉백을 적용해 단순 방어가 아니라 위치 이동과 회피 흐름으로도 연결되도록 했습니다.

주요 파일:

```text
Assets/Core/Scripts/Player/States/ParryState.cs
Assets/Core/Scripts/CombatInterfaces.cs
```

### 3. 강화 패링

강화 패링은 일반 패링으로 축적한 에너지를 활용하는 핵심 공격 수단입니다.  
강화 패링 상태에 진입하면 플레이어의 중력과 속도를 일시적으로 제어하고, 적의 공격이 들어오는 순간 `OnCounterParry` 반응을 호출합니다.

일반 적은 강화 패링 성공 시 즉시 처치되며, 보스는 패턴과 페이즈에 따라 스택 감소, 그로기, 사망 등의 흐름으로 연결됩니다.

주요 파일:

```text
Assets/Core/Scripts/Player/States/CounterParryState.cs
Assets/Core/Scripts/CombatInterfaces.cs
```

### 4. 적 구조

적은 공통 기반 클래스인 `EnemyBase`를 상속받아 구현했습니다.  
공통적으로 플레이어 참조, 방향 전환, 사망 처리, 애니메이션 길이 계산, 활동 중지 처리를 공유하고, 개별 적은 각자의 상태와 공격 방식을 따로 구현합니다.

주요 파일:

```text
Assets/Core/Scripts/Enemy/EnemyBase.cs
Assets/Core/Scripts/Enemy/Common/SlasherEnemy.cs
Assets/Core/Scripts/Enemy/Common/ChargerEnemy.cs
```

#### Slasher Enemy

근거리 베기 공격을 사용하는 적입니다.

- 플레이어와의 거리 기준으로 대기, 접근, 후퇴, 공격 상태 전환
- 공격 애니메이션의 준비 구간, 유효 공격 구간, 회복 구간 분리
- 베기 궤적을 선분으로 계산해 패링 판정과 피격 판정 처리
- 강화 패링 성공 시 처치

#### Charger Enemy

돌진 공격을 사용하는 적입니다.

- 접근과 후퇴를 반복하다가 돌진 준비 상태로 전환
- 돌진 중 공격 판정 활성화
- 패링 성공 시 공격 판정을 끄고 지나가도록 처리
- 강화 패링 성공 시 처치

### 5. 보스 전투 구조

보스는 별도의 보스 상태 머신을 사용하며, 여러 패턴을 상태 단위로 분리했습니다.

주요 파일:

```text
Assets/Core/Scripts/Enemy/Conductor/BossController.cs
Assets/Core/Scripts/Enemy/Conductor/BossState.cs
Assets/Core/Scripts/Enemy/Conductor/States/
```

보스 패턴은 다음과 같은 구조로 구성되어 있습니다.

- 검 낙하 패턴
- 돌진/낙하 패턴
- 투사체 및 레이저 패턴
- 곡선 베기 패턴
- 페이즈 전환용 레이저 패턴
- 그로기 및 사망 상태

보스는 일반 적처럼 단순히 한 번의 강화 패링으로 끝나는 구조가 아니라, 강화 패링 성공 또는 투사체 반사 성공에 따라 스택을 소모하고 그로기 상태로 진입합니다. 특정 조건을 만족하면 다음 패턴 또는 사망 상태로 전환됩니다.

### 6. 입력 및 키 설정

입력은 Unity Input System 기반으로 관리합니다.  
이동, 점프, 대시, 패링, 회복, ESC 입력을 `InputManager`에서 통합적으로 읽고, 키 리바인딩과 입력 자동화 모드를 지원하도록 구성했습니다.

주요 파일:

```text
Assets/Core/Scripts/GameManagement/InputManager.cs
Assets/Core/etc/Input Actions.inputactions
Assets/Core/Scripts/UI/KeyBindFunction/
```

## 프로젝트 구조

```text
Assets/
├─ Core/
│  ├─ Scenes/
│  │  ├─ TitleScene.unity
│  │  └─ InGame/
│  ├─ Scripts/
│  │  ├─ Player/
│  │  ├─ Enemy/
│  │  ├─ GameManagement/
│  │  └─ UI/
│  ├─ Prefabs/
│  └─ Resources/
├─ Plugins/
│  ├─ Demigiant/
│  └─ Sirenix/
└─ UniversalRenderPipelineGlobalSettings.asset
```

## 기술 스택

| 분류 | 사용 기술 |
| --- | --- |
| Engine | Unity 6 |
| Language | C# |
| Physics | Unity 2D Physics |
| Input | Unity Input System |
| Animation | Unity Animator |
| Rendering | Universal Render Pipeline |
| Tools | Odin Inspector, DOTween Pro |

## 구현 의도

이 프로젝트에서 가장 중요하게 본 부분은 단순히 패링 기능 하나를 만드는 것이 아니라, **패링을 게임의 핵심 전투 규칙으로 확장하는 구조**였습니다.

일반 공격을 제거하면 플레이어가 적을 처리하는 방식이 제한되기 때문에, 적의 공격 자체가 플레이어의 진행 수단이 되어야 합니다. 그래서 적 공격은 단순한 위협이 아니라, 패링 후보 등록, 에너지 획득, 강화 패링 발동, 보스 스택 감소와 연결되는 전투 자원으로 설계했습니다.

또한 적과 보스는 모두 `IParryReactive` 인터페이스를 통해 패링 결과에 반응하도록 만들어, 플레이어의 패링 시스템과 적의 피격/무력화 처리를 느슨하게 연결했습니다.

## 주요 구현 포인트

- 공격 없는 플레이어 전투 구조
- 일반 패링과 강화 패링의 역할 분리
- 패링 성공 타이밍에 따른 보상 차등화
- 공중 패링 넉백을 활용한 이동 흐름 확장
- 적별 상태 머신 기반 AI 구현
- 보스 패턴 상태 분리 및 페이즈 흐름 구성
- 키 리바인딩을 포함한 입력 관리 시스템
- VFX/SFX와 전투 판정의 연동