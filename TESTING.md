# Hướng dẫn Kiểm thử (Testing Guide)

Tài liệu này hướng dẫn cách chạy và viết thêm các bài kiểm tra (test) cho hệ thống **The First Sect Origin**.

## 1. Chạy Test nhanh
Sử dụng Makefile để chạy các lệnh test:

- **Server Test**: `make test-server`
- **Client Test**: `make test-client` (Yêu cầu Unity Editor đã được cài đặt và có trong PATH)
- **Tất cả**: `make test-all`

---

## 2. Viết thêm Test cho Server (Go)

Hệ thống sử dụng thư viện `testing` tiêu chuẩn của Go.

### Quy tắc:
- File test phải có hậu tố `_test.go` (ví dụ: `auth_service_test.go`).
- Hàm test phải bắt đầu bằng `Test` (ví dụ: `func TestLogin(t *testing.T)`).
- Đặt file test cùng package với file cần test để truy cập các biến nội bộ, hoặc package riêng để test integration.

### Ví dụ Unit Test:
```go
func TestCalculatePower(t *testing.T) {
    result := CalculatePower(10, 5)
    expected := 15
    if result != expected {
        t.Errorf("Expected %d, got %d", expected, result)
    }
}
```

---

## 3. Viết thêm Test cho Client (Unity/C#)

Hệ thống sử dụng **Unity Test Framework**.

### Quy tắc:
- File test đặt trong thư mục `Tests` (đã cấu hình Assembly Definition nếu cần).
- Sử dụng thuộc tính `[Test]` cho EditMode hoặc `[UnityTest]` cho PlayMode.

### Ví dụ PlayMode Test:
```csharp
[UnityTest]
public IEnumerator TestDiscipleSpawn() {
    var prefab = Resources.Load<GameObject>("Prefabs/Disciple");
    var obj = ObjectPool.Instance.Get(prefab);
    
    yield return null; // Đợi 1 frame
    
    Assert.IsNotNull(obj);
    Assert.IsTrue(obj.activeSelf);
}
```

### Cách chạy trong Unity Editor:
1. Mở Unity Editor.
2. Menu `Window` > `General` > `Test Runner`.
3. Chọn `PlayMode` hoặc `EditMode` và nhấn `Run All`.

---

## 4. Kiểm tra Tích hợp (Integration Test)

Khi thêm một API mới (gRPC):
1. Cập nhật `integration_test.go` trong `server/internal/world/handler/`.
2. Thêm một `t.Run` mới để gọi thử API đó với dữ liệu.
3. Đảm bảo Backend trả về đúng `message_id` như đã cam kết trong file `.proto`.
