using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PegaTrade.Layer.Models.Coins;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PegaTrade.Layer.Models.Account
{
    public class PegaUser
    {
        // -- Database
        public int UserId { get; set; }
        
        [StringLength(50, MinimumLength = 3)]
        public string FullName { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 4)]
        public string Username { get; set; }
        
        [Required]
        [StringLength(30, MinimumLength = 8)]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public bool EmailConfirmed { get; set; }
        public DateTime CreatedDate { get; set; }

        // --- Local
        [StringLength(30, MinimumLength = 8)]
        public string NewChangedPassword { get; set; }
        public string ConfirmNewChangedPassword { get; set; }

        // --- Community
        public int TotalCreatedThreads { get; set; }
        public int TotalCreatedComments { get; set; }

        public bool IsSubscribeNewsLetter { get; set; }
        
        public List<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
        public List<ExchangeApiInfo> ExchangeApiList { get; set; } = new List<ExchangeApiInfo>();
        public PTUserInfo PTUserInfo { get; set; }

        // Do this check on CRUD operations. Do not allow portfolioId modification if the current user does not have it.
        public bool HasPortfolio(int portfolioId)
        {
            return Portfolios.Any(x => x.PortfolioId == portfolioId);
        }

        public string GravatarImageLink()
        {
            string hash = GetGravatarEmailHash();
            return $"https://www.gravatar.com/avatar/{hash}?d=mm";
        }

        private string GetGravatarEmailHash()
        {
            string formattedEmail = Email;
            formattedEmail = formattedEmail.Trim().ToLowerInvariant();
            using (var md5Hasher = System.Security.Cryptography.MD5.Create())
            {
                byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(formattedEmail));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
        }
    }
}
