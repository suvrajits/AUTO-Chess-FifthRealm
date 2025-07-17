using System;

namespace Assets.SimpleSignIn.Google.Scripts
{
    [Serializable]
    public class UserInfo
    {
        public string sub; // Id;
        public string name;
        public string given_name;
        public string family_name;
        public string picture;
        public string email;
        public bool email_verified;
        public string locale;
    }
}