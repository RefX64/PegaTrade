using System;
using System.Collections.Generic;
using System.Text;

namespace PegaTrade.Layer.Models.Account
{
    [Serializable]
    public class PasswordUpdateRequest
    {
        public string Username { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }

        public string AuthenticationHash { get; set; }
        public string EmailAuthCode { get; set; }
    }
}
