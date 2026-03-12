# Aether Trail (MonoGame Vertical Slice)

C# + MonoGame로 만든 오리지널 2D 수집형 RPG 프로토타입입니다.
현재 버전은 **탐험 → 인카운터 → 턴제 전투 → 포획/승리 → 저장/로드** 루프를 실제로 플레이할 수 있는 vertical slice에 집중합니다.

## 실행 방법
1. .NET 8 SDK 설치
2. 루트 폴더에서 실행
   - `dotnet restore`
   - `dotnet build`
   - `dotnet run`

## 현재 구현된 핵심 시스템
- Title 메뉴: `New Game / Continue / Exit`
- 월드 탐험
  - 탑다운 이동 (WASD + 방향키)
  - 충돌/카메라 추적
  - 2개 구역: `Haven Hamlet`(안전 구역), `Whisper Field`(인카운터 구역)
  - 포탈 기반 구역 이동
- 랜덤 인카운터
  - 특정 지형 타일에서만 발동
  - 구역별 출현 테이블/레벨 범위/확률 데이터화(JSON)
- 턴제 전투 (1 vs 1)
  - 메뉴: `Attack / Item / Capture / Run`
  - 3개 이상 기술, HP/속도/간단 데미지 계산
  - 승리/패배/도주/포획 처리 후 월드 복귀
- 포획/파티/저장소
  - 포획 아이템 사용
  - 성공/실패 판정
  - 파티 최대 인원 초과 시 저장소로 이동
- 저장/로드
  - 저장 항목: 현재 구역, 좌표, 파티, 저장소, 인벤토리
  - `Continue`에서 저장 데이터 로드

## 조작키
- 공통
  - `Enter`: 선택/확정
  - `Esc`: 취소/뒤로/일시정지
  - `F3`: 디버그 오버레이 On/Off
- 월드
  - `WASD` 또는 `방향키`: 이동
  - `F5`: 즉시 저장
- 전투
  - `↑/↓`: 메뉴 이동
  - `Enter`: 행동 선택
  - `Esc`: 하위 메뉴에서 상위 메뉴로

## 데이터 파일
- `Content/Data/creatures.json`: 생물 종 정의
- `Content/Data/moves.json`: 기술 정의
- `Content/Data/items.json`: 아이템 정의
- `Content/Data/zones.json`: 구역/인카운터 테이블 정의
- `Content/Data/Maps/*.json`: 맵 타일 + 포탈 정의

## 저장 위치
- Windows 기준: `%LocalAppData%/PyGame/savegame.json`
- 코드 경로: `Environment.SpecialFolder.LocalApplicationData` 하위

## 수동 테스트 절차 (권장)
1. 타이틀에서 `New Game` 시작
2. 마을에서 포탈을 통해 필드로 이동
3. 필드의 인카운터 타일 이동으로 전투 진입 확인
4. `Attack`으로 승리 또는 `Run`으로 도주 확인
5. `Capture`로 포획 시 파티/저장소 반영 확인
6. `Item`으로 회복 아이템 사용 확인
7. `F5` 저장 후 종료
8. 재실행 → `Continue`로 동일 상태 복원 확인


## 빌드/검증 명령 (권장 순서)
1. `dotnet restore PyGame.sln`
2. `dotnet build PyGame.sln`
3. `dotnet run --project PyGame.csproj`

## 플레이 품질 점검 체크리스트
- 타이틀에서 `↑/↓` 또는 `W/S` 로 메뉴 이동 후 `Enter` 진입
- 월드에서 `WASD`/방향키로 이동 시 카메라가 플레이어를 따라가는지 확인
- 필드(Whisper Field) 인카운터 타일 이동으로 전투 진입 확인
- 전투에서 `Attack / Item / Capture / Run` 각각 동작 후 월드 복귀 흐름 확인
- `F3`로 디버그 오버레이 토글 확인 (`STATE/POS/INPUT/MOVED` 값 갱신)
- `F5` 저장 후 재실행 `Continue` 로 상태 복원 확인

## 아직 미구현
- 타입 상성/상태이상/정교한 AI
- 레벨업/진화/스킬 학습 확장
- NPC/퀘스트/상점 시스템
- 사운드 리소스 실제 재생 (현재는 구조만 확장 가능)
