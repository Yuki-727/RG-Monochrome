using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MainLobby.Data;
using TMPro;

namespace RhythmCore.UI
{
    public class ResultController : MonoBehaviour
    {
        [Header("--- MAIN STATS ---")]
        public GameObject panelRoot;
        public GameObject gameplayRoot;

        public TMP_Text rankText;  // Ông để cái này ở đâu cũng được
        public TMP_Text scoreText; // Để góc trên cùng cũng được
        public TMP_Text comboText; // Để giữa màn hình cũng được

        [Header("--- JUDGEMENT PANELS (Kéo từ Hierarchy vào) ---")]
        // Khai báo từng cái riêng biệt để ông muốn vứt nó ở đâu trong canvas thì vứt
        public JudgementEntryUI perfectUI;
        public JudgementEntryUI goodUI;
        public JudgementEntryUI missUI;

        [Header("--- COLORS ---")]
        public Color sRankColor = Color.cyan;
        public Color aRankColor = Color.green;
        public Color bRankColor = Color.yellow;
        public Color fRankColor = Color.red;

        // Tên Scene Lobby để load lại
        public string LobbyScene = "Lobby";

        void Awake()
        {
            if (gameplayRoot != null)
            {
                gameplayRoot.SetActive(true);
            }

            // Đảm bảo khi game vừa load lên là cái bảng này phải tắt
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void ShowResults(GlobalDataManager.PlayResultData data)
        {
            gameplayRoot.SetActive(false);
            panelRoot.SetActive(true);

            // 1. Set thông tin chính
            rankText.text = data.rank;
            scoreText.text = data.score.ToString("N0");
            comboText.text = data.maxCombo.ToString();

            // Set màu Rank
            switch (data.rank)
            {
                case "S": rankText.color = sRankColor; break;
                case "A": rankText.color = aRankColor; break;
                case "B": rankText.color = bRankColor; break;
                default: rankText.color = fRankColor; break;
            }

            // 2. Set dữ liệu cho từng Panel Judgement cố định
            // Không cần Loop gì cả, gán thẳng tay luôn
            if (perfectUI != null) perfectUI.SetCount(data.perfectCount);
            if (goodUI != null) goodUI.SetCount(data.goodCount);
            if (missUI != null) missUI.SetCount(data.missCount);

            // (Nâng cao) Tại đây ông có thể gọi Animation
            // Ví dụ: perfectUI.GetComponent<Animator>().Play("Bounce");
        }

        public void OnContinuePressed()
        {
            if (GlobalDataManager.Instance != null) GlobalDataManager.Instance.IsReturningFromGameplay = true;
            SceneManager.LoadScene(LobbyScene);
        }

        public void OnRetryPressed()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}