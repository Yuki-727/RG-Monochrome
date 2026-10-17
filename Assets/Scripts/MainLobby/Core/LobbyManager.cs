using UnityEngine;
using System.Collections.Generic;

namespace MainLobby.Core
{
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        [Header("Scene Config")]
        [Tooltip("Kéo cái GameObject 'ActiveRoom' vào đây để làm cha của RoomPrefab")]
        [SerializeField] private Transform activeRoomContainer;

        [Header("Rooms Configuration")]
        [SerializeField] private List<GameObject> roomPrefabs;

        private GameObject _currentRoomInstance;

        private void Awake()
        {
            Instance = this;
            // Test thử load phòng 0 luôn khi chạy game
            if (roomPrefabs.Count > 0) LoadRoom(0);
        }

        public void LoadRoom(int roomIndex)
        {
            if (roomIndex < 0 || roomIndex >= roomPrefabs.Count) return;

            // 1. Dọn dẹp phòng cũ (nếu có)
            if (_currentRoomInstance != null) Destroy(_currentRoomInstance);

            // 2. Spawn phòng mới VÀ SET PARENT LÀ ACTIVE ROOM
            // Instantiate(Prefab, Parent) -> Đây là cái ông cần
            _currentRoomInstance = Instantiate(roomPrefabs[roomIndex], activeRoomContainer);

            // Đặt lại tọa độ local về 0,0,0 cho chắc ăn
            _currentRoomInstance.transform.localPosition = Vector3.zero;
        }
    }
}