using RhythmCore.Data;
using MainLobby.Data;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainLobby.UI
{
    public class SongInfoDisplay : MonoBehaviour
    {
        public static SongInfoDisplay Instance;

        [Header("--- UI References ---")]
        public Image coverImage;
        public AspectRatioFitter coverFitter;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI artistText;

        [Header("--- Difficulty Bubbles ---")]
        public DiffBubble bubbleEasy;
        public DiffBubble bubbleMedium;
        public DiffBubble bubbleHard;
        public DiffBubble bubbleSpecial;

        private void Awake() { Instance = this; }

        public void UpdateInfo(SongEntry song, string forcedDiffKey = null)
        {
            if (song == null) return;

            ChartMetadata displayMeta = null;

            if (!string.IsNullOrEmpty(forcedDiffKey))
            {
                // Nếu bị ép (ví dụ từ Final Check), lấy đúng cái đó
                displayMeta = GetMeta(song, forcedDiffKey);
                return;
            }

            // --- 1. SETUP BUBBLES (Dùng key chữ thường) ---
            // Quy tắc: 
            // - Key "easy" vào Bubble Easy
            // - Key "normal" hoặc "medium" vào Bubble Medium
            // - Key "hard" vào Bubble Hard
            // - Key "special" hoặc "expert" vào Bubble Special

            // Check Easy
            var easyMeta = GetMeta(song, "easy");
            bubbleEasy.Setup(easyMeta, false); // Tạm thời để false selected

            // Check Medium (hỗ trợ cả key "normal" và "medium")
            var medMeta = GetMeta(song, "normal") ?? GetMeta(song, "medium");
            bubbleMedium.Setup(medMeta, false);

            // Check Hard
            var hardMeta = GetMeta(song, "hard");
            bubbleHard.Setup(hardMeta, true); // Mặc định chọn Hard sáng đèn (hoặc logic khác tùy ông)

            // Check Special
            var specMeta = GetMeta(song, "special") ?? GetMeta(song, "expert");
            bubbleSpecial.Setup(specMeta, false);

            // --- 2. HIỂN THỊ INFO CHÍNH (Title, Artist, Cover) ---
            // Lấy cái meta của Hard để hiển thị, nếu ko có thì lấy Medium -> Easy -> Special
            displayMeta = hardMeta ?? medMeta ?? easyMeta ?? specMeta;

            if (displayMeta != null) UpdateDisplayUI(displayMeta);
        }

        // Hàm lấy Meta nhanh gọn
        private ChartMetadata GetMeta(SongEntry song, string key)
        {
            if (song.Charts.ContainsKey(key))
                return song.Charts[key].metadata;
            return null;
        }

        private void UpdateDisplayUI(ChartMetadata meta)
        {
            if (titleText) titleText.text = meta.title;
            if (artistText) artistText.text = meta.artist;

            // Load ảnh (Code cũ ông đã chạy ok)
            string cleanPath = PathHelper.FixResourcePath(meta.ArtCoverPath);
            Sprite loadedSprite = Resources.Load<Sprite>(cleanPath);
            if (loadedSprite != null && coverImage != null)
            {
                coverImage.gameObject.SetActive(true);
                coverImage.sprite = loadedSprite;
                if (coverFitter) coverFitter.aspectRatio = loadedSprite.rect.width / loadedSprite.rect.height;
            }
            else if (coverImage)
            {
                coverImage.gameObject.SetActive(false);
            }
        }
    }
}