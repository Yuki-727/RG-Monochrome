using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MainLobby.Data;

namespace MainLobby.UI
{
    public class SongPanelItem : MonoBehaviour
    {
        [Header("--- NORMAL STATE UI ---")]
        [SerializeField] private TextMeshProUGUI normalSongNameText;   // Đổi tên biến cho rõ
        [SerializeField] private TextMeshProUGUI normalArtistText;

        [Header("--- SELECTED STATE UI ---")]
        [SerializeField] private TextMeshProUGUI selectedSongNameText; // Kéo Text trong SelectedVisualContainer vào đây
        [SerializeField] private TextMeshProUGUI selectedArtistText;   // Kéo Text trong SelectedVisualContainer vào đây

        [Header("--- STATE OBJECTS ---")]
        [SerializeField] private GameObject normalStateObj;
        [SerializeField] private GameObject selectedStateObj;

        [Header("--- OTHERS ---")]
        [SerializeField] private TextMeshProUGUI difficultyText;

        private System.Action _onClickAction;

        public void Setup(SongEntry song, string difficultyKey, bool isSelected, System.Action onClickAction)
        {
            _onClickAction = onClickAction;

            if (song == null)
            {
                if (normalStateObj) normalStateObj.SetActive(false);
                if (selectedStateObj) selectedStateObj.SetActive(false);
                return;
            }

            // 1. Bật/Tắt Container
            if (normalStateObj) normalStateObj.SetActive(!isSelected);
            if (selectedStateObj) selectedStateObj.SetActive(isSelected);

            // 2. Lấy Data
            var meta = song.GetMetadata(difficultyKey);

            // 3. Update Text cho NORMAL
            if (normalSongNameText) normalSongNameText.text = meta.title;
            if (normalArtistText) normalArtistText.text = meta.artist;

            // 4. Update Text cho SELECTED (FIX CHÍNH CỦA ÔNG Ở ĐÂY)
            if (selectedSongNameText) selectedSongNameText.text = meta.title;
            if (selectedArtistText) selectedArtistText.text = meta.artist;

            // 5. Diff Text (Thường chỉ hiện ở Normal, nhưng tùy ông)
            if (difficultyText) difficultyText.text = $"{meta.difficultyName.ToUpper()} {meta.level}";
        }

        public void OnClick() => _onClickAction?.Invoke();
    }
}