using MainLobby.Core;
using MainLobby.Data;
using MainLobby.Logic;
using System.Collections;
using UnityEngine;

namespace MainLobby.Visuals
{
    public class NPCController : MonoBehaviour
    {
        [Header("Identity")]
        public string NPCID;

        [Header("Components")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        public bool IsBusy { get; private set; }
        private AnimatorOverrideController _overrideController;

        private int _defaultSortingOrder;
        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            _defaultSortingOrder = spriteRenderer.sortingOrder;

            // Init Animation Override
            if (animator.runtimeAnimatorController != null)
            {
                _overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
                animator.runtimeAnimatorController = _overrideController;
            }
        }

        public void InteractWith(SmartObject targetObject, InteractionSlot slot)
        {
            if (IsBusy) return; // Chặn nếu đang bận
            
            if (slot == null || slot.anchorPoint == null)
            {
                Debug.LogError($"[{NPCID}] Lỗi: Slot null hoặc chưa có Anchor!");
                return;
            }

            // 1. SET BUSY NGAY LẬP TỨC (Chốt đơn liền)
            IsBusy = true;

            transform.position = slot.anchorPoint.position;

            spriteRenderer.sortingOrder = slot.targetSortingOrder;
            
            Debug.Log($"[{NPCID}] Đã teleport tới {targetObject.name}");

            // 3. Lấy Data an toàn (Tránh null reference làm crash game)
            InteractionVisualData.InteractionClipSet clipSet = new InteractionVisualData.InteractionClipSet();

            if (LobbyDataManager.Instance != null)
            {
                clipSet = LobbyDataManager.Instance.GetInteractionClips(NPCID, targetObject.ObjectTypeID);
            }
            else
            {
                Debug.LogWarning("LobbyDataManager chưa khởi tạo! NPC sẽ dùng Default Anim.");
            }

            // 4. Lấy duration
            float taskDuration = targetObject.GetInteractionDuration();

            // 5. Chạy Coroutine
            StartCoroutine(PerformInteractionSequence(clipSet, taskDuration, slot));
        }

        private IEnumerator PerformInteractionSequence(InteractionVisualData.InteractionClipSet clips, float duration, InteractionSlot currentSlot)
        {
            // 1. Chiếm chỗ & Setup Layer
            currentSlot.SetOccupied(true);

            // Lưu lại sorting cũ để trả lại sau
            int oldSortingOrder = spriteRenderer.sortingOrder;
            // Set sorting mới để đè lên ghế hoặc nấp sau ghế
            spriteRenderer.sortingOrder = currentSlot.targetSortingOrder;

            // --- GIAI ĐOẠN 1: INTRO ---
            if (clips.introClip != null)
            {
                // B1: Gán clip vào
                _overrideController["Base_Intro"] = clips.introClip;

                // B2: QUAN TRỌNG - Chờ 1 frame để Animator kịp nuốt clip mới
                yield return null;

                // B3: Bắt buộc chạy từ giây thứ 0 (Layer 0, normalizedTime 0)
                animator.Play("Base_Intro", 0, 0f);

                Debug.Log($"[{NPCID}] ▶ Intro: {clips.introClip.name} ({clips.introClip.length}s)");

                // B4: Chờ hết thời gian clip
                yield return new WaitForSeconds(clips.introClip.length);
            }

            // --- GIAI ĐOẠN 2: LOOP ---
            // Xử lý logic chọn clip (Ưu tiên Override -> Default -> Không làm gì)
            AnimationClip loopToPlay = clips.loopClip != null ? clips.loopClip : currentSlot.defaultAnim;

            if (loopToPlay != null)
            {
                _overrideController["Base_Loop"] = loopToPlay;

                // Chờ 1 frame cho chắc cú
                yield return null;
                animator.Play("Base_Loop", 0, 0f);

                Debug.Log($"[{NPCID}] ↻ Loop: {loopToPlay.name} ({duration}s)");
            }

            // Giả vờ làm việc
            yield return new WaitForSeconds(duration);

            // --- GIAI ĐOẠN 3: OUTRO ---
            if (clips.outroClip != null)
            {
                _overrideController["Base_Outro"] = clips.outroClip;

                yield return null;
                animator.Play("Base_Outro", 0, 0f);

                Debug.Log($"[{NPCID}] ■ Outro: {clips.outroClip.name}");
                yield return new WaitForSeconds(clips.outroClip.length);
            }

            // --- KẾT THÚC ---
            currentSlot.SetOccupied(false);
            IsBusy = false;

            // Trả lại Sorting Order cũ (để đi lại không bị chìm xuống nền nhà)
            spriteRenderer.sortingOrder = oldSortingOrder;

            //animator.Play("Idle");
            Debug.Log($"[{NPCID}] ✅ Done!");
        }
    }
}