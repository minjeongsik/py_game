# Aether Trail (MonoGame Prototype)

오리지널 2D 몬스터 수집 RPG를 목표로 하는 C# + MonoGame 프로젝트입니다.
현재는 **첫 번째 플레이 가능한 골격(Vertical Slice 0.1)** 에 집중하여,
타이틀 화면에서 월드 진입 후 이동/충돌/랜덤 인카운터 트리거까지 동작하는 구조를 제공합니다.

## 기술 스택
- C# (.NET 8)
- MonoGame DesktopGL
- JSON 기반 콘텐츠/세이브 데이터

## 실행 방법
1. .NET 8 SDK 설치
2. 저장소 루트에서 실행:
   - `dotnet restore`
   - `dotnet build`
   - `dotnet run`

## 현재 구현 범위
- 게임 상태 관리(확장형 상태 객체 구조)
  - Title
  - WorldExploration
  - PauseMenu
  - EncounterOverlay(임시 전투 진입 화면)
- 월드 탐험
  - 탑다운 2D 이동 (WASD / 방향키)
  - 충돌 타일
  - 카메라 따라오기
  - JSON 맵 로딩
- 랜덤 인카운터 골격
  - 오리지널 특수 지형 타일(맵의 `2`) 이동 중 확률 트리거
- 저장/로드 골격
  - F5 저장
  - F9 로드
  - 플레이어 위치 / 파티 / 인벤토리 구조 포함
- 데이터 모델 골격
  - CreatureSpeciesDefinition
  - CreatureInstance
  - MoveDefinition
  - ItemDefinition
  - ZoneDefinition

## 조작
- `Enter`: 타이틀 -> 월드 진입, 인카운터 오버레이 종료
- `WASD` 또는 `방향키`: 이동
- `Esc`: 일시정지 메뉴 열기/닫기
- `F5`: 저장
- `F9`: 로드

## 폴더 구조
- `Core/`
  - 입력, 카메라, 상태 타입/매니저, 상태 컨텍스트
  - `Core/States/`: 상태별 업데이트 로직 (`TitleState`, `WorldExplorationState` 등)
- `World/`: 맵/로더, 플레이어 이동, 인카운터, 월드 렌더링
- `Creatures/`: 크리처 종/개체 데이터 모델
- `Data/`: 기술/아이템/지역/세이브 모델 및 저장 서비스(JSON)
- `UI/`: 화면 오버레이 렌더링
- `Content/Data/`: JSON 콘텐츠(테스트 맵)

## 미구현 / 다음 단계
- 실제 전투 시스템 (턴 처리, 스킬 실행, 피해 계산)
- 파티/인벤토리 UI 및 데이터 편집 루프
- 크리처 획득/관리 루프
- NPC, 상호작용 오브젝트, 퀘스트 진행
- 타일셋/애니메이션/사운드 리소스 파이프라인
