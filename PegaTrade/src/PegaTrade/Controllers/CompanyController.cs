using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Core.StaticLogic.Helper;
using PegaTrade.Helpers;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Helpers;
using PegaTrade.ViewModel;

namespace PegaTrade.Controllers
{
    public class CompanyController : Controller
    {
        [Route("Company")]
        [ResponseCache(Duration = 1200)]
        public IActionResult Company()
        {
            return View("PartialToFull", new PartialToFullVM
            {
                Title = "About Us",
                PartialViewName = "_AboutUs"
            });
        }

        public PartialViewResult CompanyPV() => PartialView("_AboutUs");

        [Route("ContactUs")]
        [HttpGet]
        public IActionResult ContactUs()
        {
            return View("PartialToFull", new PartialToFullVM
            {
                Title = "Contact Us",
                PartialViewName = "_ContactUs",
                ViewModel = new ContactUsVM()
            });
        }

        public PartialViewResult ContactUsPV() => PartialView("_ContactUs");

        [HttpPost]
        [ValidateAntiForgeryToken]
        [TypeFilter(typeof(ValidateReCaptcha))]
        public JsonResult SubmitContactUs(ContactUsVM vm)
        {
            if (ModelState.IsValid)
            {
                if (!Utilities.IsValidEmailAddress(vm.Email)) { return Json(ResultsItem.Error("Please enter a valid email address.")); }

                // Send the email and grab the result
                bool mailSendSuccess = Utilities.SendEmailUsingSyneiGmail(vm.Email, vm.Name, vm.Subject, $"{vm.Name}, {vm.Email} | {vm.Message}");
                return mailSendSuccess ? Json(ResultsItem.Success("Your email has been successfully sent. We'll get back to you as soon as possible. Thanks!"))
                                       : Json(ResultsItem.Error("Sending has failed. Please email us manually at support@PegaTrade.com."));
            }

            return Json(ResultsItem.Error(ModelState.GetAllErrorsString()));
        }
        
        public JsonResult SubscribeEmail(string email)
        {
            return Json(PegaLogic.SubscribeEmail(email, Types.EmailPreferences.Default));
        }
    }
}