# Design of The TileTrip
- File này đã được tóm tắt bằng AI và đã qua chỉnh sửa của em giúp cho việc truyền tải trở nên dễ đàng hơn

## 1. Quyết định Kiến trúc (Architecture Decisions)
*   **MVC:** Tách biệt logic cốt lõi vào các Controller (Gameplay, Tray, Home). `GameplayController` làm trung tâm điều phối.
*   **Giao tiếp qua Event:** Dùng `C# Action` và `Unity Event` để kết nối các hệ thống rời rạc (UI, Audio, Gameplay) giúp giảm sự phụ thuộc (loose coupling).
*   **Xử lý Bất đồng bộ:** Dùng `UniTask` và `DOTween` theo yêu cầu của đề bài đồng thời cũng xử lý  luồng game giúp mượt mà và dễ đọc

*   **Tối ưu hiệu năng:** Áp dụng `Object Pooling` cho Tile và Particle Effect để tránh giật lag do Garbage Collection.

### GamePlayController đóng vai trò điều phối các script bên dưới:
- Board Factory => Quản lý việc sinh ra màn chơi
- TrayManager => Quản lý các Tile đã được nhấn trên thanh Tray đồng thời sắp xếp các Animation. (Do thời gian quá gấp nên không thể tách ra thành 2 file để quản lý dữ liệu và animation được nên đã gộp chung)
- ComboManager => Quản lý việc đếm Combo và thực thi animation. (Vẫn chưa thể tách làm 2 file)
- Game UI Manger => Quản Lý Manager Thắng thua và các Animation



## 2. Cấu trúc Dữ liệu Màn chơi (Level Data)
Theo như game mẫu, các tile sẽ có quy định như sau : 
- Các tile trên cùng 1 layer ko được xếp chồng lên nhau
- Các tile trên layer cao hơn có thể che khuất trong các số 25% 50% và 100% tile layer thấp. ==> Có thể sắp xếp các layer trước rồi sử dụng thuật toán generate ra màn chơi
 

Sử dụng **ScriptableObject** để quản lý:
*   `LevelData`: Lưu thông tin 1 màn chơi (Số ô trong khay, các loại icon, và danh sách tọa độ (X, Y, Layer) của từng ô).
*   `LevelDatabase`: Lưu mảng toàn bộ các level theo thứ tự.
*   **Custom Editor Tool:** Đã viết một công cụ trực quan trên Unity Editor (`LevelEditorWindow`) để designer dễ dàng vẽ và sắp xếp các ô gạch thay vì nhập số liệu thủ công.
 ==> Có thể dễ dàng tạo ra 20 Level trong thời gian ngắn

## 3. Đảm bảo màn chơi có thể giải được (Solvability)

Đầu tiên sẽ phải chắc chắn số tile có trong màn chơi là bội số của 3.
Sau khi sắp xếp tất cả vô LevelData sẽ đưa lên cho SolvableGenerator để kiểm thử và đưa các ID vào.

Thay vì xếp random, thuật toán (`SolvableGenerator`) sử dụng phương pháp **"Giải ngược" (Bottom-Up)**:
1.  Tạo đồ thị ảo chứa các ô, tính toán xem ô nào đang đè lên ô nào.
2.  Tìm 3 ô đang "tự do" (không bị đè), gán cho chúng cùng 1 loại ID (icon), sau đó xóa 3 ô này khỏi đồ thị ảo để lộ ra các ô bên dưới.
3.  Lặp lại quá trình trên cho đến khi hết ô. 
4.  Cuối cùng, trộn (shuffle) ngẫu nhiên ID của các ô nằm cùng độ sâu (Layer) để màn chơi không bị trùng lặp mỗi lần chơi.

## 4 Any trade-offs or areas you'd improve with more time 
Các điểm cần cải thiện : 
    - Do màn chơi được sinh ra từ thuật toán nên các màn về sau sẽ thử thách mang tính may rủi. Tùy vào độ may mắn của người chơi mà code sinh ra màn chơi khó hoặc dễ. ==> Cải thiện thuật toán

    - ComboManager vẫn còn hoạt động không được mượt mà và chưa thể hiện rõ trên UI

    - Polish responsive UI

Những điểm sẽ thêm vào: 
- Với mục tiêu clone gần như toàn diện phần game mà Sun Studio đã cung cấp, em sẽ thêm vào đơn vị tiền tệ cũng như các công cụ để trợ giúp người chơi vượt qua màn

- Thêm nhiều phần thưởng cho tiến trình của người chơi.

# Em cảm ơn ban giám khảo đã đọc ạ!