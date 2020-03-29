using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Helpers;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Account;
using PegaTrade.Layer.Models.Helpers;

namespace PegaTrade.Controllers
{
    public class HomeController : BaseController
    {
        public IActionResult Index(bool timeout = false)
        {
            if (timeout) { TempData["timeout"] = true; }
            return View();
        }
        
        [Route("Legal")]
        public IActionResult Legal()
        {
            return View();
        }

        [Route("PrivacyPolicy")]
        public IActionResult PrivacyPolicy()
        {
            return View();
        }
    }
}
