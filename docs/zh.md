# Resource Atlas — 哪个模组在用谁的资源

资源图谱描绘游戏中各模组如何使用彼此的资源：背景、音效、音乐、着色器、场景、纹理、实体、翻译键、剧情动作和补丁。对每一次使用，它都会检查取用资源的模组是否依赖提供资源的模组。它还会找出互不依赖的模组提供同一个键的情况——此时谁胜出取决于并未定义的加载顺序——以及没有任何模组提供的键。

它建立在 Validator 的声明与引用图之上，并补充了 Validator 不追踪的内容：纹理、音效和着色器的键、翻译键，以及以字面量形式写在 C# 代码里的键。

## 如何运行

图谱是一次工具运行：它生成报告后即退出。

```text
godot --path . -- --game=novel_template --with-mods=resource_atlas --resource-atlas --atlas-out=<folder>
godot --path . -- --resource-atlas --atlas-out=res://tests/artifacts/resource_atlas --with-test-suites
```

- 使用 `--game=<id>` 时，映射的是该游戏的模组（外加壳层）；不带它则映射所有已安装的模组。
- `--with-test-suites` 会把各模组的测试套件也纳入映射。
- 它会把 `report.md`（供阅读）和 `report.json`（供工具使用）写进 `--atlas-out` 文件夹，在日志里输出 `atlas: N error(s), M warning(s)`，并在有错误时以退出码 1 结束。
- 测试运行器把它当作测试：`resource_atlas`（所有模组），以及每个游戏一个，例如 `resource_atlas_novel_template`。

`report.md` 的正文是俄语。

## 一个跨模组的例子

小说模板自带的模组 `my_novel_game` 自己没有任何图片：它的小镇、酒吧和家使用的都是 `town_pack` 的背景——同一个游戏里可复用的素材包。报告的关联表把这一点显示为一行：

```text
| `my_novel_game` | `town_pack` | yes | 3 | background | `bar/bar`, `home/vestibule`, `town/town_entry` |
```

各列依次是：模组本身；它从哪个模组取用资源；该模组是否在它的 `depends` 里；使用次数、种类和示例。这一行是正确的，因为 `my_novel_game` 在自己的 `depends` 中声明了 `town_pack>=1`。

为了这一页，我们在一次运行前从清单里移除了这条依赖。图谱随即报出三个错误——每个背景一条，各自指出使用它的文件：

```text
**Errors: 3** (mods: my_novel_game) · warnings: 0

### A foreign resource is used without a dependency in depends

- `my_novel_game` → `town_pack`: background `bar/bar` — uses a resource of the mod town_pack (data) that is not in depends — add the dependency or move the resource. `scenes/my_novel_bar.json`
```

把依赖加回去之后，报告又恢复为没有错误。

为什么这很重要：没有 `depends`，就没有任何东西会让游戏在加载模板模组的同时加载 `town_pack`。只安装了 `my_novel_game` 的玩家会看到没有背景的场景。

## 报告里有什么

- **错误** — 使用了未声明依赖的他人资源；互不依赖（未被 `depends` 关联）的模组提供了同一个键；未声明依赖就给他人资源打补丁。
- **模组之间的关联** — 谁在用谁的资源、是否已声明、数量多少、属于哪些种类。
- **覆盖** — 由多个模组提供的键，以及哪一个胜出。
- **对他人资源的补丁** — 场景和实体补丁，并标出被打补丁的模组。
- **警告** — 没有任何模组提供的键。
- **按模组汇总**和**加载顺序**。

键的匹配方式沿用游戏的规则：纹理按文件名；音效、音乐、着色器和场景按其在组内的路径；实体按种类和 id；翻译按语言和键。

## 如何修复发现的问题

1. **资源在未声明依赖的情况下被使用。** 把所有者加进 `depends`，把资源移进使用它的模组，或者给你的副本一个自己的键。
2. **同一个键出现在互不相关的模组里。** 如果其中一个应当替换另一个，替换方必须依赖所有者；否则请重命名其中一个。
3. **没有人提供的键。** 要么是笔误，要么是提供它的模组不属于这个游戏。
