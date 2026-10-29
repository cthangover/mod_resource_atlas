# Resource Atlas — どの MOD がどの MOD のリソースを使っているか

Resource Atlas は、ゲーム内の MOD 同士が互いのリソースをどう使っているかを地図にします。対象は背景、効果音、音楽、シェーダー、シーン、テクスチャ、エンティティ、翻訳キー、シナリオアクション、パッチです。利用が一つあるごとに、リソースを取る側の MOD が与える側の MOD に依存しているかを検査します。さらに、互いに依存していない MOD が同じキーを提供している場合——そのとき勝者は未定義の読み込み順で決まります——や、どの MOD も提供していないキーも見つけます。

土台は Validator の宣言と参照のグラフで、そこに Validator が追跡しないものを足します。テクスチャ・効果音・シェーダーのキー、翻訳キー、そして C# コードにリテラルとして書かれたキーです。

## 実行方法

アトラスはツール実行です。レポートを生成して終了します。

```text
godot --path . -- --game=novel_template --with-mods=resource_atlas --resource-atlas --atlas-out=<folder>
godot --path . -- --resource-atlas --atlas-out=res://tests/artifacts/resource_atlas --with-test-suites
```

- `--game=<id>` を付けるとそのゲームの MOD（シェルも含む）をマップし、付けない場合はインストール済みの全 MOD を対象にします。
- `--with-test-suites` を付けると、MOD のテストスイートもマップします。
- `--atlas-out` フォルダーに `report.md`（読む用）と `report.json`（ツール用）を書き込み、`atlas: N error(s), M warning(s)` をログに出し、エラーがあれば終了コード 1 で終わります。
- テストランナーでは次のテストとして登録されています。`resource_atlas`（全 MOD）と、ゲームごとの `resource_atlas_novel_template` など。

`report.md` の本文はロシア語で書かれています。

## MOD をまたいだ例

小説テンプレート自前の MOD である `my_novel_game` には、独自の画像がありません。その町・バー・家は、同じゲームの再利用可能なパック `town_pack` の背景を使います。レポートのリンク表では、これが 1 行として現れます:

```text
| `my_novel_game` | `town_pack` | yes | 3 | background | `bar/bar`, `home/vestibule`, `town/town_entry` |
```

列は左から、その MOD、リソースを取る相手の MOD、相手が自分の `depends` に入っているかどうか、利用数とその種類、そして例です。この行が正しいのは、`my_novel_game` が自分の `depends` で `town_pack>=1` を宣言しているからです。

このページのため、1 回の実行でのみマニフェストからこの依存を取り除きました。するとアトラスは 3 つのエラーで失敗しました。背景ごとに 1 つずつ、それぞれ使っているファイル付きです:

```text
**Errors: 3** (mods: my_novel_game) · warnings: 0

### A foreign resource is used without a dependency in depends

- `my_novel_game` → `town_pack`: background `bar/bar` — uses a resource of the mod town_pack (data) that is not in depends — add the dependency or move the resource. `scenes/my_novel_bar.json`
```

依存を戻すと、レポートはまたエラーなしに戻ります。

なぜ重要か: `depends` がなければ、テンプレートの MOD と一緒に `town_pack` を読み込ませるものは何もありません。`my_novel_game` だけをインストールしたプレイヤーは、背景のないシーンを見ることになります。

## レポートの中身

- **エラー** — 依存関係なしに他 MOD のリソースを使っている。`depends` でつながっていない MOD が同じキーを提供している。依存関係なしに他 MOD のリソースへパッチを当てている。
- **MOD 間のリンク** — 誰が誰のリソースを取るか、それが宣言されているか、件数と種類。
- **上書き** — 複数の MOD が提供しているキーと、どれが勝つか。
- **他 MOD リソースへのパッチ** — シーンとエンティティのパッチと、パッチ先の MOD。
- **警告** — 読み込まれたどの MOD も提供していないキー。
- **MOD ごとの集計**と**読み込み順**。

キーの対応づけはゲームのルールに従います。テクスチャはファイル名、効果音・音楽・シェーダー・シーンはグループ内のパス、エンティティは種類と id、翻訳は言語とキーです。

## 見つかった問題の直し方

1. **依存関係なしに使われているリソース。** 所有者を `depends` に足すか、リソースを使っている側の MOD へ移すか、自分のコピーに固有のキーを与えます。
2. **無関係な MOD に同じキー。** 一方がもう一方を置き換えるはずなら、置き換える側が所有者に依存しなければなりません。そうでなければ、どちらかの名前を変えます。
3. **誰も提供していないキー。** タイプミスか、提供する MOD がこのゲームに含まれていないかです。
