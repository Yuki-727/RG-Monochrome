using MainLobby.Data;
using RhythmCore.UI;
using System.Collections;
using UnityEngine;

namespace RhythmCore.Logic
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [Header("References")]
        public AudioSource musicSource;
        public ResultController resultController; // UI Overlay

        [Header("State")]
        public bool isGameActive = false;
        private bool isGameFinished = false;

        void Awake() => Instance = this;

        void Start()
        {
            // Reset điểm số khi bắt đầu màn chơi
            if (ScoreManager.Instance != null) ScoreManager.Instance.ResetSession();

            // Tự động start game (hoặc ông gọi hàm StartGame() từ chỗ khác)
            StartCoroutine(StartGameRoutine());
        }

        IEnumerator StartGameRoutine()
        {
            yield return new WaitForSeconds(1f); // Intro delay
            isGameActive = true;
        }

        void Update()
        {
            if (!isGameActive || isGameFinished) return;

            // CHECK END GAME: Khi nhạc ngừng chạy VÀ thời gian đã chạy hết độ dài bài
            // (Thêm dk musicSource.time > 0 để tránh check lúc mới load chưa play)
            if (!musicSource.isPlaying && musicSource.time == 0 && Time.timeSinceLevelLoad > 5f)
            {
                FinishGame();
            }
            // Fallback: Check time thủ công (đôi khi audio time không chính xác tuyệt đối)
            else if (musicSource.clip != null && musicSource.time >= musicSource.clip.length - 0.1f)
            {
                FinishGame();
            }
        }

        public void FinishGame()
        {
            if (isGameFinished) return;
            isGameFinished = true;

            Debug.Log("Game Finished! Saving results...");

            // 1. Lấy kết quả từ ScoreManager
            var finalData = ScoreManager.Instance.GetFinalResult();

            // 2. Lưu vào "Vali" GlobalDataManager
            if (GlobalDataManager.Instance != null)
            {
                GlobalDataManager.Instance.LastPlayResult = finalData;

                // Đánh dấu là đã chơi xong -> Để Lobby biết đường xử lý
                // (Lưu ý: Flag này sẽ được check bên LobbyScene)
                GlobalDataManager.Instance.IsReturningFromGameplay = true;
            }
            else
            {
                Debug.LogWarning("Không tìm thấy GlobalDataManager! (Đang test Scene lẻ?)");
            }

            // 3. Hiển thị UI Kết quả (ResultScreen)
            StartCoroutine(ShowResultDelay(finalData));
        }

        IEnumerator ShowResultDelay(GlobalDataManager.PlayResultData data)
        {
            yield return new WaitForSeconds(1.5f);
            if (resultController != null)
            {
                resultController.ShowResults(data);
            }
        }
    }
}