using System;
using System.Collections.Generic;
using System.Text;

namespace PegaTrade.Layer.Models.Account
{
    public class UserInfo
    {
        public int UserInfoId { get; set; }
        public DateTime? Birthday { get; set; }
        public string AboutMe { get; set; }
        public int? PhoneNumber { get; set; }
    }
}
