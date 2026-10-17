using System;

namespace User.Data
{
    [Serializable]
    public class UserProfile
    {
        public string UserName;
        public int Level;
        public long Coin;

        public UserProfile()
        {
            UserName = "Hana";
            Level = 1;
            Coin = 0;
        }
    }
}