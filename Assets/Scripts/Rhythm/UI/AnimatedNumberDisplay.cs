using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RhythmCore.UI
{
    public class AnimatedNumberDisplay : MonoBehaviour
    {
        [System.Serializable]
        public struct DigitSet
        {
            public string name; // Để dễ nhìn trong Inspector (ví dụ: "Số 0")
            public Sprite[] variations; // Kéo 3 cái sprite (biến thể) vào đây
        }

        [Header("Data")]
        public DigitSet[] digitData; // Mảng chứa dữ liệu số từ 0 đến 9
        // Nếu muốn hỗ trợ dấu chấm hay chữ cái thì thêm vào đây sau

        [Header("Settings")]
        public GameObject digitPrefab; // Một cái Image rỗng để làm mẫu
        public Transform container;    // Cái khung chứa các con số (Horizontal Layout Group)
        public float animationSpeed = 0.3f; // Tốc độ đổi skin (giây)

        [Header("Debug")]
        public int testNumber = 0; // Để test thử trong Editor

        private List<Image> _activeDigits = new List<Image>();
        private float _timer;
        private int _currentFrameIndex;
        private string _currentString = "";

        void Start()
        {
            // Ẩn cái prefab mẫu đi nếu nó đang hiện
            if (digitPrefab != null) digitPrefab.SetActive(false);
        }

        void Update()
        {
            // Logic đổi skin (Animation)
            _timer += Time.deltaTime;
            if (_timer >= animationSpeed)
            {
                _timer = 0f;
                _currentFrameIndex++;

                // Lặp lại từ 0 -> 1 -> 2 -> 0...
                // Giả sử mỗi số đều có 3 biến thể
                RefreshSprites();
            }
        }

        // Hàm cập nhật lại hình ảnh cho các số đang hiện
        // Hàm cập nhật lại hình ảnh cho các số đang hiện
        void RefreshSprites()
        {
            // SỬA LỖI Ở ĐÂY:
            // Chỉ lặp qua độ dài của chuỗi số hiện tại (_currentString.Length)
            // Thay vì lặp qua tất cả _activeDigits (bao gồm cả những cái đang bị ẩn)

            for (int i = 0; i < _currentString.Length; i++)
            {
                // Bây giờ i luôn nằm trong phạm vi của _currentString, không bao giờ crash nữa
                int digitValue = _currentString[i] - '0';

                if (digitValue >= 0 && digitValue < digitData.Length)
                {
                    Sprite[] vars = digitData[digitValue].variations;
                    if (vars.Length > 0)
                    {
                        int spriteIndex = _currentFrameIndex % vars.Length;

                        // Đảm bảo activeDigits có đủ phần tử (thường là có đủ do EnsureDigitCount gọi trước rồi)
                        if (i < _activeDigits.Count)
                        {
                            _activeDigits[i].sprite = vars[spriteIndex];
                        }
                    }
                }
            }
        }

        // HÀM CHÍNH: Gọi cái này để set số (ví dụ từ ScoreManager)
        public void SetValue(int number)
        {
            _currentString = number.ToString(); // Chuyển số thành chuỗi "123"

            // 1. Tạo đủ số lượng Image
            EnsureDigitCount(_currentString.Length);

            // 2. Set kích thước native cho đẹp (Optional)
            foreach (var img in _activeDigits) img.SetNativeSize();

            // 3. Update hình ngay lập tức
            RefreshSprites();
        }

        // Đảm bảo có đủ UI Image để hiển thị
        void EnsureDigitCount(int count)
        {
            // Nếu thiếu thì tạo thêm
            while (_activeDigits.Count < count)
            {
                GameObject newObj = Instantiate(digitPrefab, container);
                newObj.SetActive(true);
                _activeDigits.Add(newObj.GetComponent<Image>());
            }

            // Nếu thừa thì ẩn đi (Object Pooling đơn giản)
            for (int i = 0; i < _activeDigits.Count; i++)
            {
                _activeDigits[i].gameObject.SetActive(i < count);
            }
        }

        // Để test trong Inspector
        [ContextMenu("Test Number")]
        public void Test()
        {
            SetValue(testNumber);
        }
    }
}