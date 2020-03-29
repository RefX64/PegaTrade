using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PegaTrade.Core.EntityFramework
{
    public partial class Users
    {
        public Users()
        {
            ApiDetails = new HashSet<ApiDetails>();
            BBCommentVotes = new HashSet<BBCommentVotes>();
            BBComments = new HashSet<BBComments>();
            BBThreadVotes = new HashSet<BBThreadVotes>();
            BBThreads = new HashSet<BBThreads>();
            Portfolios = new HashSet<Portfolios>();
        }

        [Key]
        [Column("UserID")]
        public int UserId { get; set; }
        [StringLength(50)]
        public string FullName { get; set; }
        [Required]
        [StringLength(20)]
        public string Username { get; set; }
        [Required]
        [StringLength(100)]
        public string Password { get; set; }
        [Required]
        [StringLength(50)]
        public string Salt { get; set; }
        [Required]
        [StringLength(50)]
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        [Column(TypeName = "date")]
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }

        [InverseProperty("User")]
        public PTUserInfo PTUserInfo { get; set; }
        [InverseProperty("UserInfoNavigation")]
        public UserInfo UserInfo { get; set; }
        [InverseProperty("User")]
        public ICollection<ApiDetails> ApiDetails { get; set; }
        [InverseProperty("User")]
        public ICollection<BBCommentVotes> BBCommentVotes { get; set; }
        [InverseProperty("User")]
        public ICollection<BBComments> BBComments { get; set; }
        [InverseProperty("User")]
        public ICollection<BBThreadVotes> BBThreadVotes { get; set; }
        [InverseProperty("User")]
        public ICollection<BBThreads> BBThreads { get; set; }
        [InverseProperty("User")]
        public ICollection<Portfolios> Portfolios { get; set; }
    }
}
