# Resource Atlas — qué mod usa los recursos de cuál

El Atlas de recursos traza cómo los mods de un juego usan los recursos de los demás: fondos, sonidos, música, shaders, escenas, texturas, entidades, claves de traducción, acciones de escenario y parches. Para cada uso comprueba que el mod que toma el recurso dependa del que lo da. También encuentra la misma clave servida por mods que no se dependen entre sí —entonces el ganador depende del orden de carga, que no está definido—, y claves que nadie proporciona.

Se apoya en el grafo de declaraciones y referencias del Validator y añade lo que este no sigue: claves de texturas, sonidos y shaders, claves de traducción y claves escritas como literales en código C#.

## Cómo se ejecuta

El atlas es una ejecución de herramienta: construye el informe y sale.

```text
godot --path . -- --game=novel_template --with-mods=resource_atlas --resource-atlas --atlas-out=<folder>
godot --path . -- --resource-atlas --atlas-out=res://tests/artifacts/resource_atlas --with-test-suites
```

- Con `--game=<id>` mapea los mods de ese juego (más la shell); sin él, todos los mods instalados.
- `--with-test-suites` mapea también los conjuntos de pruebas de los mods.
- Escribe `report.md` (para leer) y `report.json` (para herramientas) en la carpeta `--atlas-out`, registra `atlas: N error(s), M warning(s)` y sale con código 1 si hay errores.
- El test runner lo tiene como pruebas: `resource_atlas` (todos los mods) y una por juego, como `resource_atlas_novel_template`.

El texto de `report.md` está escrito en ruso.

## Un ejemplo entre mods

El mod propio de la plantilla de novela, `my_novel_game`, no tiene imágenes suyas: su ciudad, su bar y su casa usan fondos de `town_pack`, un paquete reutilizable del mismo juego. La tabla de enlaces del informe lo muestra en una fila:

```text
| `my_novel_game` | `town_pack` | yes | 3 | background | `bar/bar`, `home/vestibule`, `town/town_entry` |
```

Las columnas son: el mod, el mod del que toma recursos, si ese mod está en su `depends`, el número de usos, sus clases y ejemplos. La fila es correcta porque `my_novel_game` declara `town_pack>=1` en su `depends`.

Para esta página se quitó la dependencia del manifiesto durante una ejecución. El atlas falló entonces con tres errores, uno por fondo, cada uno con el archivo que lo usa:

```text
**Errors: 3** (mods: my_novel_game) · warnings: 0

### A foreign resource is used without a dependency in depends

- `my_novel_game` → `town_pack`: background `bar/bar` — uses a resource of the mod town_pack (data) that is not in depends — add the dependency or move the resource. `scenes/my_novel_bar.json`
```

Con la dependencia de vuelta, el informe vuelve a quedarse sin errores.

Por qué importa: sin `depends`, nada obliga al juego a cargar `town_pack` junto con el mod de la plantilla. Un jugador que instale solo `my_novel_game` vería escenas sin fondos.

## Qué contiene el informe

- **Errores** — un recurso ajeno usado sin dependencia; una clave servida por mods no enlazados por `depends`; un parche de un recurso ajeno sin dependencia.
- **Enlaces entre mods** — quién toma los recursos de quién, si está declarado, cuántos y de qué clases.
- **Sustituciones** — claves servidas por varios mods y cuál gana.
- **Parches de recursos ajenos** — parches de escenas y entidades, con el mod parcheado.
- **Advertencias** — claves que ningún mod cargado proporciona.
- **Resumen por mod** y el **orden de carga**.

El emparejamiento de claves sigue las reglas del juego: texturas por nombre de archivo; sonidos, música, shaders y escenas por su ruta dentro del grupo; entidades por clase e id; traducciones por idioma y clave.

## Cómo arreglar lo que encuentra

1. **Un recurso usado sin dependencia.** Añade el propietario a `depends`, mueve el recurso al mod que lo usa o dale a tu copia su propia clave.
2. **La misma clave en mods sin relación.** Si uno debe sustituir al otro, el que sustituye debe depender del propietario; si no, renombra uno de los dos.
3. **Una clave que nadie proporciona.** Una errata, o el mod que la da no forma parte de este juego.
