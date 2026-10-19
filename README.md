# Court of Ni

판사·피고인·배심원으로 나뉘어 "누가 범인인가"를 설득하는 온라인 파티 보드게임.

- **엔진:** Unity 6 (6000.0 LTS 이상), 2D
- **네트워크:** Netcode for GameObjects, 호스트 권한(방장 한 명이 서버 역할). 지금은 Unity Transport를 쓰고, 출시할 때 Steam P2P 트랜스포트로 바꿀 예정이다.
- **음성:** `IVoiceService` 인터페이스만 있다. 지금은 빈 구현(`NullVoiceService`)이고, Vivox 등은 나중에 연동한다.

## 처음 열기

1. Unity Hub ▸ **Add ▸ Add project from disk**에서 이 폴더를 고르고, Unity 6 에디터로 연다.
2. 처음 열면 Unity가 `ProjectSettings/`와 모든 `.meta` 파일을 만든다. 이 파일들은 **커밋한다** (`Library/`, `Temp/` 등은 `.gitignore`에 들어 있다).
3. 메뉴 **Court of Ni ▸ Create Dev Scene and Sample Data**를 실행한다. 샘플 증거물 카드 12장, 배심원 미션 4개, 규칙 에셋, 테스트 씬(`Assets/_Project/Scenes/Dev.unity`)이 만들어진다.
4. 4명 이상으로 테스트하려면 **Window ▸ Multiplayer ▸ Multiplayer Play Mode**(Package Manager에서 `com.unity.multiplayer.playmode` 설치)로 가상 플레이어를 띄우거나, 빌드를 여러 개 실행한다. 한 명이 **호스트**를 누르고 나머지는 **접속**을 누른 뒤, 호스트가 **게임 시작**을 누르면 된다.

## 구조

```
Assets/_Project/Scripts/
  Core/          규칙 엔진. Unity에 의존하지 않는 순수 C# (noEngineReferences)
    Roles/         역할, 역할 배정, 균등 로테이션 (한 바퀴 = 모두가 판사를 한 번씩)
    Phases/        페이즈 상태 머신: 역할 공개 → 사건 만들기 → [변론A → 변론B → 질문 → 배심원 회의 → 투표] × 라운드 → 판결
    Cards/         증거물 카드, 범용 덱 (버린 더미 자동 재활용)
    Match/         MatchSession(한 판), GameSession(게임 전체), 투표·판결, 규칙 값(MatchRules)
    Missions/      배심원 개인 미션 인터페이스와 기본 미션 2종
    Chat/          역할별 채팅 채널 규칙 (법정 / 배심원 전용)
    Voice/         역할·페이즈별 음성 채널 규칙과 IVoiceService
    Events/        GameEventBus와 게임 이벤트 (연출을 붙이는 지점)
  Data/          ScriptableObject: 규칙 에셋, 증거물 카드, 배심원 미션
  Net/           NGO 연결: CourtNetworkGame(호스트 권한 진행기), VoiceRouter, 개발용 로비 화면
  Presentation/  연출 훅: GameEventPresenter<T> 기반 클래스와 페이즈 연출 예시
  Editor/        개발용 씬·샘플 데이터 생성 메뉴
Assets/_Project/Tests/EditMode/  Core 단위 테스트
tools/           Unity 없이 Core를 검증하는 .NET 하네스
```

**흐름:** 서버(호스트)만 `GameSession`을 돌린다. 규칙 코드가 내보낸 이벤트를 `CourtNetworkGame`이 필요한 사람에게만 RPC로 보낸다 (배심원 미션과 배심원 채팅은 배심원에게만, 개별 투표 내역은 비공개). 클라이언트는 받은 이벤트를 `GameEventBus`로 발행하고, 연출 컴포넌트는 그 이벤트를 구독해서 애니메이션과 사운드를 재생한다.

## 규칙 커스터마이즈

규칙 값은 모두 `GameRulesetAsset`에 있다. 라운드 수, 변론 시 뽑는 카드 수, 유죄/무죄 점수(n점), 동률 처리, 페이즈별 제한 시간(0이면 판사가 직접 넘김), 사용할 카드와 미션 목록을 설정할 수 있다.

새 배심원 미션 종류는 `IJuryMission`을 구현하는 클래스와, 그것을 만드는 `JuryMissionAsset` 하위 클래스를 추가하면 Create 메뉴에 나타난다.

## 테스트

- **Unity:** Window ▸ General ▸ Test Runner ▸ EditMode
- **Unity 없이:** `dotnet test tools/CoreTests`로 Core 규칙과 테스트를 실행한다. `dotnet build tools/CompileCheck`는 Core, Data, Presentation이 Unity API 수준(.NET Standard 2.1, C# 9)에서 컴파일되는지 확인한다. 둘 다 .NET 8 SDK가 필요하다.

## 현재 가정한 규칙 (기획 확정 시 수정)

- 투표는 "이 피고인이 범인이다"에 던지는 표다. 라운드마다 배심원 한 명이 한 표를 던지고, 모든 라운드의 합계로 판결한다.
- 총 득표가 같으면 마지막 라운드 득표로 가리고, 그래도 같으면 아무도 유죄가 되지 않는다 (점수 변동 없음).
- 라운드마다 먼저 변론하는 피고인이 번갈아 바뀐다.
- 역할은 역할 카드를 뽑는 대신 로테이션으로 균등하게 배정한다. 역할 카드는 화면 연출로 보여준다.
- 판사는 점수를 얻거나 잃지 않는다.
- 게임 중 접속이 끊긴 플레이어 처리는 아직 없다.
