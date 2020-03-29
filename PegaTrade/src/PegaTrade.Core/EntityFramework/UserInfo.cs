using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PegaTrade.Core.EntityFramework
{
    public partial class UserInfo
    {
        [Column("UserInfoID")]
        public int UserInfoId { get; set; }
        [Column(TypeName = "date")]
        public DateTime? Birthday { get; set; }
        [StringLength(300)]
        public string AboutMe { get; set; }
        public int? PhoneNumber { get; set; }
        public short? ConfirmationAuthType { get; set; }
        [StringLength(100)]
        public string ConfirmationAuthCode { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? ConfirmationAuthDate { get; set; }

        [ForeignKey("UserInfoId")]
        [InverseProperty("UserInfo")]
        public Users UserInfoNavigation { get; set; }
    }
}
