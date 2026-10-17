using UnityEngine;
using System.Collections.Generic;
using MainLobby.Data;  // Để lấy SongLibraryManager
using MainLobby.Logic; // Để lấy SongSelectionManager
using RhythmCore.Data; // Để lấy ChartData

namespace MainLobby.UI
{
    public class TOCPanel : BasePanel
    {
        [Header("References")]
        [SerializeField] private Transform listContainer; // Cái Content của ScrollView hoặc VerticalLayout
        [SerializeField] private TOCItem itemPrefab;

        private bool _isInitialized = false;

        private void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(() => LobbyUIManager.Instance.Back());
        }

        public override void Show()
        {
            base.Show();
            if (!_isInitialized)
            {
                GenerateTOC();
                _isInitialized = true;
            }
        }

        private void GenerateTOC()
        {
            // Clear cũ
            foreach (Transform child in listContainer) Destroy(child.gameObject);

            // Lấy List từ Library
            var library = SongLibraryManager.Instance.GetSongList();

            for (int i = 0; i < library.Count; i++)
            {
                SongEntry entry = library[i];
                string songName = "Unknown Song";

                // TRICK: Lấy tên bài hát từ bất kỳ Chart nào có trong entry
                // Vì SongEntry là Dictionary<string, ChartData>
                if (entry.Charts != null && entry.Charts.Count > 0)
                {
                    foreach (var chartData in entry.Charts.Values)
                    {
                        if (chartData != null && chartData.metadata != null)
                        {
                            songName = chartData.metadata.title;
                            break; // Lấy được rồi thì thoát loop
                        }
                    }
                }
                else
                {
                    songName = entry.SongID; // Fallback nếu lỗi data
                }

                TOCItem newItem = Instantiate(itemPrefab, listContainer);
                newItem.Setup(songName, i, this);
            }
        }

        public void OnSongSelected(int index)
        {
            // 1. Lưu logic game (đã từng mở sách)
            LobbyUIManager.Instance.MarkSongSelectedFromTOC();

            // 2. Set bài hát trong Manager (Logic mới thêm)
            SongSelectionManager.Instance.SetCurrentIndex(index);

            // 3. Chuyển scene
            LobbyUIManager.Instance.OpenSongSelect();
        }
    }
}