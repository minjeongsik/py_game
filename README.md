# Aether Trail (MonoGame Prototype)

오리지널 2D 몬스터 수집 RPG를 목표로 하는 C# + MonoGame 프로젝트입니다.
현재는 **필드 탐험 + 최소 턴제 전투 Vertical Slice 0.2** 범위까지 구현했습니다.

## 기술 스택
- C# (.NET 8)
- MonoGame DesktopGL
- JSON 기반 데이터

## 실행 방법
1. .NET 8 SDK 설치
2. 저장소 루트에서 실행:
   - `dotnet restore`
   - `dotnet build`
   - `dotnet run`

## 현재 구현 범위
- 게임 상태
  - `Title`
  - `WorldExploration`
  - `PauseMenu`
  - `BattleState`
- 월드 탐험
  - 탑다운 2D 이동 (WASD / 방향키)
  - 충돌 타일
  - 카메라 따라오기
  - JSON 맵 로딩
- 인카운터
  - 특수 지형 타일(`2`) 이동 시 확률적으로 야생 인카운터
- 최소 턴제 전투
  - 아군 1마리 vs 야생 1마리
  - 커맨드 선택(기술 2개 + 도주)
  - 속도 기반 행동 순서
  - 체력/공격/방어 기반 간단 데미지 계산
  - 간단 상성 골격(`Spark`, `Mist`, `Stone`)
  - 승리/패배/도주 후 월드 복귀
- 저장/로드 골격
  - F5 저장
  - F9 로드
  - 플레이어 위치 + 파티 구조

## 조작
- `Enter`: 타이틀 시작, 전투 종료 후 월드 복귀
- `WASD` 또는 `방향키`: 이동 / 전투 메뉴 선택(위·아래)
- `Esc`: 일시정지 메뉴 열기/닫기
- `F5`: 저장
- `F9`: 로드

## 폴더 구조
- `Core/`: 상태 관리, 입력, 카메라
- `World/`: 맵, 로더, 플레이어 이동, 인카운터, 월드 렌더링
- `Battle/`: 턴제 전투 흐름, 타입 상성, 전투 계산
- `Creatures/`: 크리처 정의/인스턴스 모델
- `Data/`: 기술/아이템/지역/세이브/JSON 데이터 스토어
- `UI/`: 화면 오버레이 렌더링
- `Content/Data/`: 맵/종/기술/지역 JSON 콘텐츠

## 데이터 파일
- `Content/Data/world_map.json`: 테스트 월드 맵
- `Content/Data/species_definitions.json`: 종 스탯/기술 학습 풀
- `Content/Data/move_definitions.json`: 전투 기술 정의
- `Content/Data/zone_definitions.json`: 지역 인카운터 풀

## 미구현 / 다음 단계
- 포획 시스템
- 경험치 및 레벨업
- 파티 교체/복수 파티원 전투
- 인벤토리/아이템 실사용
- 정식 텍스트 렌더링용 폰트/리소스 파이프라인
- 애니메이션/사운드/NPC 상호작용
