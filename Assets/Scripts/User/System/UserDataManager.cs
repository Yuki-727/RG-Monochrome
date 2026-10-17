using UnityEngine;
using User.Data;

namespace User.Systems
{
    public class UserDataManager : MonoBehaviour
    {
        public UserProfile CurrentProfile;

        public void LoadData()
        {
            // Tạm thời hardcode để test, sau này thay bằng Load từ File/Server
            CurrentProfile = new UserProfile();
            CurrentProfile.UserName = "Yn";
            CurrentProfile.Coin = 500;
            Debug.Log($"[UserSystem] Loaded profile: {CurrentProfile.UserName}");
        }
    }
}