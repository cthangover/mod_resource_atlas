# Resource Atlas — which mod uses whose resources

The Resource Atlas maps how the mods of a game use each other's resources: backgrounds, sounds, music, shaders, scenes, textures, entities, translation keys, scenario actions and patches. For every use it checks that the mod which takes the resource depends on the mod which gives it. It also finds the same key shipped by mods that do not depend on each other — then the winner depends on load order, which is not defined — and keys that no mod provides.

It builds on the Validator's graph of declarations and references and adds what the Validator does not follow: texture, sound and shader keys, translation keys and keys written as literals in C# code.

## Running it

The atlas is a tool run: it builds the report and quits.

```text
godot --path . -- --game=novel_template --with-mods=resource_atlas --resource-atlas --atlas-out=<folder>
godot --path . -- --resource-atlas --atlas-out=res://tests/artifacts/resource_atlas --with-test-suites
```

- With `--game=<id>` it maps the mods of that game (plus the shell); without it, every installed mod.
- `--with-test-suites` also maps the test suites of the mods.
- It writes `report.md` (to read) and `report.json` (for tools) into the `--atlas-out` folder, logs `atlas: N error(s), M warning(s)` and exits with 1 when there are errors.
- The test runner has it as tests: `resource_atlas` (all mods) and one per game, such as `resource_atlas_novel_template`.

The text of `report.md` is written in Russian.

## A cross-mod example

The novel template's own mod, `my_novel_game`, has no pictures of its own: its town, bar and home use backgrounds of `town_pack`, a reusable pack of the same game. The report's table of links shows this as one row:

```text
| `my_novel_game` | `town_pack` | yes | 3 | background | `bar/bar`, `home/vestibule`, `town/town_entry` |
```

The columns are: the mod, the mod it takes resources from, whether that mod is in its `depends`, the number of uses, their kinds and examples. The row is correct because `my_novel_game` declares `town_pack>=1` in its `depends`.

For this page the dependency was removed from the manifest for one run. The atlas then failed with three errors, one per background, each with the file that uses it:

```text
**Errors: 3** (mods: my_novel_game) · warnings: 0

### A foreign resource is used without a dependency in depends

- `my_novel_game` → `town_pack`: background `bar/bar` — uses a resource of the mod town_pack (data) that is not in depends — add the dependency or move the resource. `scenes/my_novel_bar.json`
```

With the dependency back, the report has no errors again.

Why it matters: without `depends`, nothing makes the game load `town_pack` with the template's mod. A player who installs only `my_novel_game` would get scenes without backgrounds.

## What the report contains

- **Errors** — a foreign resource used without a dependency; one key provided by mods not linked by `depends`; a patch of a foreign resource without a dependency.
- **Links between mods** — who takes whose resources, whether it is declared, how many and of which kinds.
- **Overrides** — keys shipped by several mods and which one wins.
- **Patches of foreign resources** — scene and entity patches, with the patched mod.
- **Warnings** — keys no loaded mod provides.
- **Summary by mod** and the **load order**.

How keys are matched follows the game: textures by file name, sounds, music, shaders and scenes by their path in the group, entities by kind and id, translations by language and key.

## Fixing what it finds

1. **A resource used without a dependency.** Add the owner to `depends`, or move the resource into the mod that uses it, or give your copy its own key.
2. **The same key in unrelated mods.** If one should replace the other, the replacing mod must depend on the owner; otherwise rename one of them.
3. **A key nobody provides.** A typo, or the providing mod is not part of this game.
