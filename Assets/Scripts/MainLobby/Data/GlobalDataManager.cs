using UnityEngine;
using RhythmCore.Data;

namespace MainLobby.Data
{
    public class GlobalDataManager : MonoBehaviour
    {
        public static GlobalDataManager Instance;

        // --- KHU VỰC "VALI" (SESSION DATA) ---
        // Đây là nơi chứa đồ để mang qua Scene khác
        public GameSessionData CurrentSession = new GameSessionData();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Giữ vali sống qua các Scene
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Class chứa dữ liệu phiên chơi (Nested Class cho gọn)
        [System.Serializable]
        public class GameSessionData
        {
            public SongEntry SelectedSong;
            public ChartMetadata SelectedChart;
            public string SelectedDifficulty = "Hard"; // Mặc định Hard như đã chốt

            // Hàm đóng gói đồ đạc
            public void SetData(SongEntry song, string difficulty)
            {
                SelectedSong = song;
                SelectedDifficulty = difficulty;
                // Tự động lấy metadata tương ứng
                if (song != null)
                {
                    SelectedChart = song.GetMetadata(difficulty);
                    // Fallback nếu không tìm thấy difficulty
                    if (SelectedChart == null && song.Charts.Count > 0)
                        SelectedChart = song.Charts.GetEnumerator().Current.Value.metadata;
                }
            }
        }

        // --- Khu vực Result Data ---
        public bool IsReturningFromGameplay = false;

        // Hộp chứa kết quả màn chơi vừa xong
        public PlayResultData LastPlayResult = new PlayResultData();

        [System.Serializable]
        public class PlayResultData
        {
            public int score;
            public int maxCombo;
            public float accuracy;
            public string rank; // S, A, B, C, F
            public int perfectCount;
            public int goodCount;
            public int missCount;
            public bool isFullCombo;

            // Hàm reset sạch dữ liệu (dùng trước khi chơi mới)
            public void Reset()
            {
                score = 0; maxCombo = 0; accuracy = 0;
                rank = "F";
                perfectCount = 0; goodCount = 0; missCount = 0;
                isFullCombo = false;
            }
        }
    }
}