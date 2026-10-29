# Resource Atlas — mod nào dùng tài nguyên của mod nào

Resource Atlas vẽ ra cách các mod của một game dùng tài nguyên của nhau: background, âm thanh, nhạc, shader, cảnh, texture, entity, khóa dịch, hành động kịch bản và các bản vá. Với mỗi lần sử dụng, nó kiểm tra rằng mod lấy tài nguyên có phụ thuộc vào mod cho không. Nó cũng tìm ra trường hợp nhiều mod không phụ thuộc lẫn nhau lại cung cấp cùng một khóa — khi đó bên thắng phụ thuộc vào thứ tự tải, vốn không được định nghĩa — và những khóa không mod nào cung cấp.

Nó xây trên đồ thị khai báo và tham chiếu của Validator, cộng thêm những gì Validator không theo dõi: khóa texture, âm thanh và shader, khóa dịch, và các khóa được viết dạng literal trong mã C#.

## Cách chạy

Atlas là một lần chạy công cụ: nó tạo báo cáo rồi thoát.

```text
godot --path . -- --game=novel_template --with-mods=resource_atlas --resource-atlas --atlas-out=<folder>
godot --path . -- --resource-atlas --atlas-out=res://tests/artifacts/resource_atlas --with-test-suites
```

- Có `--game=<id>` thì nó vẽ các mod của game đó (cộng cả shell); không có thì vẽ mọi mod đã cài.
- `--with-test-suites` vẽ thêm cả các bộ kiểm thử của mod.
- Nó ghi `report.md` (để đọc) và `report.json` (cho công cụ) vào thư mục `--atlas-out`, ghi log `atlas: N error(s), M warning(s)` và thoát với mã 1 khi có lỗi.
- Trong test runner, nó là các phép kiểm thử: `resource_atlas` (mọi mod) và mỗi game một phép, ví dụ `resource_atlas_novel_template`.

Phần chữ của `report.md` được viết bằng tiếng Nga.

## Một ví dụ giữa các mod

Mod riêng của bản mẫu tiểu thuyết, `my_novel_game`, không có hình ảnh của riêng nó: thị trấn, quán bar và ngôi nhà của nó dùng background của `town_pack` — một gói tái sử dụng của cùng game. Bảng liên kết trong báo cáo hiển thị điều này thành một dòng:

```text
| `my_novel_game` | `town_pack` | yes | 3 | background | `bar/bar`, `home/vestibule`, `town/town_entry` |
```

Các cột lần lượt là: mod, mod mà nó lấy tài nguyên, mod đó có nằm trong `depends` của nó không, số lần dùng, các loại và ví dụ. Dòng này đúng vì `my_novel_game` khai báo `town_pack>=1` trong `depends` của mình.

Cho trang này, người ta đã gỡ phần phụ thuộc khỏi manifest trong đúng một lần chạy. Atlas lập tức thất bại với ba lỗi — mỗi background một lỗi, kèm theo tệp đang dùng nó:

```text
**Errors: 3** (mods: my_novel_game) · warnings: 0

### A foreign resource is used without a dependency in depends

- `my_novel_game` → `town_pack`: background `bar/bar` — uses a resource of the mod town_pack (data) that is not in depends — add the dependency or move the resource. `scenes/my_novel_bar.json`
```

Có lại phần phụ thuộc, báo cáo lại không còn lỗi.

Vì sao điều này quan trọng: không có `depends`, không gì buộc game tải `town_pack` cùng với mod của bản mẫu. Người chơi chỉ cài `my_novel_game` sẽ thấy những cảnh không có background.

## Báo cáo chứa gì

- **Lỗi** — dùng tài nguyên của người khác mà không có phụ thuộc; nhiều mod không liên kết bằng `depends` lại cung cấp cùng một khóa; vá tài nguyên của người khác mà không có phụ thuộc.
- **Liên kết giữa các mod** — ai lấy tài nguyên của ai, có được khai báo không, bao nhiêu và thuộc loại nào.
- **Ghi đè** — những khóa do nhiều mod cung cấp và bên nào thắng.
- **Vá tài nguyên của mod khác** — các bản vá cảnh và entity, kèm mod bị vá.
- **Cảnh báo** — những khóa không mod nào đang tải cung cấp.
- **Tổng kết theo mod** và **thứ tự tải**.

Cách khớp khóa theo đúng luật của game: texture theo tên tệp; âm thanh, nhạc, shader và cảnh theo đường dẫn trong nhóm; entity theo loại và id; bản dịch theo ngôn ngữ và khóa.

## Sửa những gì nó tìm thấy

1. **Tài nguyên được dùng mà không có phụ thuộc.** Thêm chủ sở hữu vào `depends`, chuyển tài nguyên sang mod đang dùng nó, hoặc cho bản sao của bạn một khóa riêng.
2. **Cùng một khóa ở những mod không liên quan.** Nếu bên này phải thay thế bên kia thì bên thay thế phải phụ thuộc vào chủ sở hữu; nếu không, hãy đổi tên một trong hai.
3. **Một khóa không ai cung cấp.** Hoặc là lỗi đánh máy, hoặc mod cung cấp nó không thuộc game này.
