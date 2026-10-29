# Resource Atlas — 어떤 모드가 누구의 리소스를 쓰는가

Resource Atlas는 게임의 모드들이 서로의 리소스를 어떻게 사용하는지 지도로 보여 줍니다. 대상은 배경, 효과음, 음악, 셰이더, 씬, 텍스처, 엔티티, 번역 키, 시나리오 액션, 패치입니다. 사용이 하나 있을 때마다 리소스를 가져오는 모드가 제공하는 모드에 의존하는지 검사합니다. 또한 서로 의존하지 않는 모드들이 같은 키를 제공하는 경우—이때 승자는 정해지지 않은 로드 순서에 달려 있습니다—와 어느 모드도 제공하지 않는 키도 찾아 냅니다.

Validator의 선언·참조 그래프를 기반으로 하며, Validator가 추적하지 않는 것들을 더합니다. 텍스처·효과음·셰이더 키, 번역 키, 그리고 C# 코드에 리터럴로 적힌 키입니다.

## 실행 방법

아틀라스는 도구 실행입니다. 리포트를 만들고 종료합니다.

```text
godot --path . -- --game=novel_template --with-mods=resource_atlas --resource-atlas --atlas-out=<folder>
godot --path . -- --resource-atlas --atlas-out=res://tests/artifacts/resource_atlas --with-test-suites
```

- `--game=<id>`를 붙이면 그 게임의 모드(셸 포함)를 매핑하고, 붙이지 않으면 설치된 모든 모드를 매핑합니다.
- `--with-test-suites`를 붙이면 모드의 테스트 스위트도 매핑합니다.
- `--atlas-out` 폴더에 `report.md`(읽기용)와 `report.json`(도구용)을 쓰고, `atlas: N error(s), M warning(s)`를 로그에 남기며, 오류가 있으면 종료 코드 1로 끝납니다.
- 테스트 러너에서는 테스트로 등록되어 있습니다. `resource_atlas`(모든 모드)와 게임마다 하나씩, 예를 들어 `resource_atlas_novel_template`입니다.

`report.md`의 본문은 러시아어로 쓰여 있습니다.

## 모드 간 예시

소설 템플릿 자체 모드인 `my_novel_game`에는 자기 이미지가 없습니다. 마을·바·집은 같은 게임의 재사용 가능한 팩인 `town_pack`의 배경을 사용합니다. 리포트의 링크 표에서는 이것이 한 행으로 표시됩니다:

```text
| `my_novel_game` | `town_pack` | yes | 3 | background | `bar/bar`, `home/vestibule`, `town/town_entry` |
```

열은 차례대로 그 모드, 리소스를 가져오는 상대 모드, 그 모드가 자신의 `depends`에 있는지 여부, 사용 횟수와 종류, 예시입니다. 이 행이 맞는 이유는 `my_novel_game`이 자신의 `depends`에 `town_pack>=1`을 선언하기 때문입니다.

이 페이지를 위해 한 번의 실행에서만 매니페스트에서 이 의존성을 제거했습니다. 그러자 아틀라스는 배경마다 하나씩, 각각 사용하는 파일과 함께 세 개의 오류로 실패했습니다:

```text
**Errors: 3** (mods: my_novel_game) · warnings: 0

### A foreign resource is used without a dependency in depends

- `my_novel_game` → `town_pack`: background `bar/bar` — uses a resource of the mod town_pack (data) that is not in depends — add the dependency or move the resource. `scenes/my_novel_bar.json`
```

의존성을 되돌리면 리포트는 다시 오류 없이 돌아옵니다.

중요한 이유: `depends`가 없으면 템플릿 모드와 함께 `town_pack`을 불러오게 만드는 것이 아무것도 없습니다. `my_novel_game`만 설치한 플레이어는 배경 없는 씬을 보게 됩니다.

## 리포트에 든 것

- **오류** — 의존성 없이 다른 모드의 리소스를 사용함. `depends`로 연결되지 않은 모드들이 같은 키를 제공함. 의존성 없이 다른 모드의 리소스를 패치함.
- **모드 간 링크** — 누가 누구의 리소스를 가져오는지, 선언되어 있는지, 몇 개이며 어떤 종류인지.
- **재정의** — 여러 모드가 제공하는 키와 어느 쪽이 이기는지.
- **다른 모드 리소스에 대한 패치** — 씬·엔티티 패치와 패치된 모드.
- **경고** — 로드된 어느 모드도 제공하지 않는 키.
- **모드별 요약**과 **로드 순서**.

키 매칭은 게임의 규칙을 따릅니다. 텍스처는 파일 이름으로, 효과음·음악·셰이더·씬은 그룹 안 경로로, 엔티티는 종류와 id로, 번역은 언어와 키로 맞춥니다.

## 발견한 것 고치기

1. **의존성 없이 사용되는 리소스.** 소유자를 `depends`에 추가하거나, 리소스를 그것을 사용하는 모드로 옮기거나, 여러분의 사본에 고유한 키를 주세요.
2. **관련 없는 모드에 같은 키.** 한쪽이 다른 쪽을 대체해야 한다면 대체하는 모드가 소유자에 의존해야 합니다. 그렇지 않다면 둘 중 하나의 이름을 바꾸세요.
3. **아무도 제공하지 않는 키.** 오타이거나, 제공하는 모드가 이 게임에 속하지 않는 것입니다.
