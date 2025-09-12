<p align="center">
  <img src="Docs/icon.png" alt="TurnLine App Icon" width="128"/>
</p>

# 턴라인 (TurnLine)

---

## 기술 스택 / 버전
- **엔진**: Unity 6
- **언어**: C#
- **UI/애니메이션**: TextMeshPro, DOTween
- **타깃**: Standalone/모바일 공통 (정수·고정소수 관점에서 결정론 지향)

---

## 설계 원칙
1. **WEGO(동시입력·동시해석)**: 한 턴 동안 모든 명령을 수집하고, **고정된 해석 순서**로 한 번에 처리한다.  
2. **결정론**: 스킵(즉시해석)과 애니메이션(코루틴) 경로가 **동일 결과**를 보장한다.  
3. **가시성**: 전송은 화살표로, 예약/해제/도착 상태는 아이콘/토큰/컬러로 표현한다.  
4. **데이터 주도**: 업그 비용/생산/방어계수를 **인스펙터 배열**과 상수로 제어한다.

---

## 턴 해석 파이프라인(결정론 보장)
`GameController`가 턴을 잠그면 다음 시퀀스를 수행한다.

1) **UpgradePhase**  
   - 유효 업그만 확정(자원/상태 검증), 비용 차감, 레벨 상승은 즉시 반영.
2) **DefensePhase**  
   - 방어 **진입**: 이번 턴 도착 공격에 즉시 유효.  
   - 방어 **해제**: 이번 턴 도착 공격에는 **경감 미적용**(다음 턴부터 비방어로 취급).
3) **Move/CombatPhase**  
   - 목적지별·진영별 **도착 병력 합산**.  
   - **중립지**: 공격자끼리 상호차감 후 잔여 vs 중립 수비. 동률은 점령 없음.  
   - **소유지**: 방어 경감률을 적용한 유효공격력으로 단순차감 전투 → 점령/방어 결과 확정.  
   - 점령 직후 지역은 **해당 턴 행동 불가** 플래그.
4) **ProductionPhase**  
   - 소유 지역이 레벨당 생산치만큼 병력 생성.
5) **Win/Lose Evaluate**  
   - 모든 지역 점령 시 즉시 승리(또는 패배).

> 애니메이션 경로: 위 단계가 각 코루틴으로 시각화.  
> 스킵 경로: 동일 로직을 프레임 내 동기 실행.

---

## 전투 수학 / 데이터
- **방어 경감률**: `r = clamp(base + step * level, 0, cap)`  
  - 기본 예시: `base=0.20, step=0.06, cap=0.60`  
  - 방어 중이며 **해제하지 않은** 경우에만 적용.  
- **유효 공격력**: `A_eff = floor(A * (1 - r))`  
- **합산 규칙**: 동일 진영이 같은 턴 같은 목적지로 도착하면 **한 번의 전투로 합산 처리**.  
- **정수/반올림**: 중간값은 즉시 `floor` 고정. 내부 표현은 **64-bit 정수** 우선.

인스펙터 파라미터
- `UpgradeCostByLevel[]`, `ProductionPerLevel[]`, `DefenseBase/Step/Cap`, `MaxLevel`
- 씬 파라미터: `ClearSceneName`, `DefeatSceneName`

---

## 데이터 모델(런타임 개념)
- Region: `id`, `owner`, `level`, `troops`, `stance(Defend/None)`, `lockedThisTurn`, `justCaptured`
- Incoming Buffer: `incoming[Player, Enemy]` (도착 턴 합산 버퍼)
- Orders(큐): `Move(amount, target)`, `Upgrade`, `DefendOn/Off`, `Wait`

---

## UI/렌더링 구조
- **명령 패널(UICommandPanel)**: 현재 지역 상태 표시, 이동 슬라이더/업그/방어 토글, 유효성 반영.
- **타깃 선택(UITargetModeIndicator)**: 타깃 선택 모드 진입/해제 안내.
- **전송 가시화**  
  - `UIMoveToken`: 출발지에서 드래그/홀드 시 토큰 노출.  
  - `UIMoveArrow`: 목적지 확정 시 생성되는 화살(헤드/샤프트/패딩 분리). DOTween으로 페이드/크기/색 연출.  
  - `MoveArrowManager`: 키 관리, 플레이어/AI/홀드 구분.  
- **경량화 팁**: 포지션/회전 변경만 Dirty 갱신, 트윈 핸들은 `OnDisable/OnDestroy`에서 `Kill()`.

---

## AI 아키텍처(AIAgent)
- **루프**: 허브 후보 선정 → 보강/집결 → 공격 타깃 결정 → 전송 예약.
- **필요 병력 추정**: 목표 수비 병력과 방어 경감률을 반영해 `A_eff > D` 조건을 맞추도록 전송량 산출.
- **타이밍 로직**: 상대 **방어 해제 턴**에 도착하도록 경로/양 조절(취약 창구 공략).
- **난이도**: 가중치/역치·업그 한계(L-캡)·허브 우선순위를 파라미터화.

---

## 개발 과정(핵심 의사결정)
- 해석 순서를 **규칙서 기반으로 고정**해 모호성 제거.  
- “해제 턴 미적용”을 방어태세 스냅샷에서 강제, 러시-심리전의 코어로 삼음.  
- 애니/스킵 결과 일관성 테스트를 우선 과제로 삼아 결정론 확보.  
- 전투 수학을 단일 함수로 수렴(방어계수·합산·내림 처리)해 AI와 해석이 동일 공식을 공유.

---

## 빌드 & 실행
1) Unity 6로 열기 → 의존 패키지(DOTween, TMP) 설치  
2) 씬 파라미터(`ClearSceneName`, `DefeatSceneName`) 설정  
3) 모바일/PC 타깃 빌드. 해상도 스케일은 Canvas Scaler에서 조절

---

## 테스트(권장 스모크 시나리오)
- **방어 해제 턴**: 해제된 지역에 같은 턴 공격 도착 → 경감 **미적용** 확인  
- **중립 동시도착 동률**: P=X, E=X → 점령 없음  
- **애니 vs 스킵 동등성**: 동일 입력에 동일 결과/씬 로드  
- **레벨 경계**: MaxLevel에서 업그 비활성, 배열 인덱스 가드  
- **AI 필요치 검증**: 다양한 수비/경감 설정에서 과·소추정률 모니터

---

## 성능/안정화 메모
- 전투/수학 경로는 **할당 없는 정수 연산** 우선.  
- 화살/토큰 풀링 고려(현재 규모에선 GC 압력 낮음).  
- DOTween 트윈 핸들 정리(`Kill`) 습관화.  
- 큰 수 표시는 UI에서만 축약(1.2k/1.2M), 내부는 64-bit 정수 유지.

---

## 인게임 화면

<p align="center">
  <img src="Docs/Screenshot_20250912_121625_TurnLine.jpg" alt="메인 메뉴" width="300"/>
</p>

<p align="center">
  <img src="Docs/Screenshot_20250912_121628_TurnLine.jpg" alt="설정 화면" width="300"/>
</p>

<p align="center">
  <img src="Docs/Screenshot_20250912_121708_TurnLine.jpg" alt="인게임 UI" width="300"/>
</p>

<p align="center">
  <img src="Docs/Screenshot_20250912_121836_TurnLine.jpg" alt="승리 화면" width="300"/>
</p>

---