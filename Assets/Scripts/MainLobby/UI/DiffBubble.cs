using MainLobby.Data;
using RhythmCore.Data;
using TMPro;
using UnityEngine;

namespace MainLobby.UI
{
    public class DiffBubble : MonoBehaviour
    {
        [Header("--- UI References ---")]
        public GameObject selectedBgObj;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI nameText;

        public void Setup(ChartMetadata meta, bool isSelected)
        {
            // Nếu meta null (tức là bài này ko có độ khó này) -> Tắt Bubble
            if (meta == null)
            {
                this.gameObject.SetActive(false);
                return;
            }

            // Có dữ liệu -> Bật lên
            this.gameObject.SetActive(true);

            // 1. Tên hiển thị (Ưu tiên difficultyName, nếu ko có thì lấy difficultyType)
            string displayName = !string.IsNullOrEmpty(meta.difficultyName)
                                 ? meta.difficultyName
                                 : meta.difficultyType;

            // Format cho đẹp (Viết hoa chữ cái đầu hoặc viết hoa hết tùy ý)
            if (nameText) nameText.text = displayName;

            // 2. Level
            if (levelText) levelText.text = meta.level.ToString();

            // 3. Highlight 
            if (selectedBgObj) selectedBgObj.SetActive(isSelected);
        }
    }
}