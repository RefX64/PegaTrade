using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Core.StaticLogic.Helper;
using PegaTrade.Helpers;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Account;
using PegaTrade.Layer.Models.Helpers;

namespace PegaTrade.Controllers
{
    public class AccountController : BaseController
    {
        #region Authorization

        [Route("Demo")]
        public async Task<ActionResult> Demo()
        {
            ResultsPair<PegaUser> pair = await AuthorizationLogic.AuthorizeUser("DemoUser", "49SPtrkuKDAtU27ifROw");
            if (pair.Result.IsSuccess)
            {
                SetSession(Constant.Session.SessionCurrentUser, pair.Value);
                return RedirectToAction("Index", "Crypto");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet("ViewUser/{username}", Name = "ViewUser_Route")]
        public ActionResult ViewUser(string username)
        {
            return View("ViewUser", new PegaUser { Username = username });
        }

        public PartialViewResult GetViewUser(string username)
        {
            var result = AuthorizationLogic.ViewUserProfile(username);
            if (!result.Result.IsSuccess) { return GeneratePartialViewError(result.Result.Message); }

            return PartialView("_ViewUser", result.Value);
        }

        [Route("Login")]
        public PartialViewResult Login()
        {
            return PartialView("FullLogin");
        }

        public PartialViewResult ModalLogin(string returnUrl = null)
        {
            return PartialView("_Login");
        }
        
        [HttpPost]
        [Route("Login")]
        [TypeFilter(typeof(ValidateReCaptcha))]
        public async Task<JsonResult> Login(PegaUser user)
        {
            ModelState.Remove("Email");
            if (!ModelState.IsValid) { return Json(ResultsItem.Error(ModelState.GetAllErrorsString())); }
            if (!Regex.IsMatch(user.Username, @"^[a-zA-Z0-9_\-\.@]+$")) { return Json(ResultsItem.Error("Username must contain only: Letters(A-Z), Numbers(0-9), _, -, ., or an email address.")); }

            ResultsPair<PegaUser> pair = await AuthorizationLogic.AuthorizeUser(user.Username, user.Password);
            if (pair.Result.IsSuccess)
            {
                SetSession(Constant.Session.SessionCurrentUser, pair.Value);
                return Json(ResultsItem.Success("Success"));
            }
            return Json(ResultsItem.Error(pair.Result.Message));
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        #if DEBUG
        public ActionResult LoginDev(string username = "xxx")
        {
            HttpContext.Session.SetString(Constant.Session.SessionCurrentUser, Utilities.Serialize(Utilities.GetDevUser(username)));
            return RedirectToAction("Index", "Crypto");
        }
        #endif

        #endregion

        #region CRUD
        public PartialViewResult Create()
        {
            return PartialView("_Create");
        }
        
        [HttpPost]
        [TypeFilter(typeof(ValidateReCaptcha))]
        public async Task<JsonResult> CreateNewUser(PegaUser user)
        {
            if (!ModelState.IsValid) { return Json(ResultsItem.Error(ModelState.GetAllErrorsString())); }
            if (!Regex.IsMatch(user.Username, @"^[a-zA-Z0-9_\-\.]+$")) { return Json(ResultsItem.Error("Username must contain only: Letters(A-Z), Numbers(0-9), _, -, or .")); }
            if (!Utilities.IsValidEmailAddress(user.Email)) { return Json("Please enter a valid email address.");  }

            var createPair = await AuthorizationLogic.CreateNewUser(user);
            if (createPair.Result.IsSuccess)
            {
                SetSession(Constant.Session.SessionCurrentUser, createPair.Value);
                TempData["UserCreatedTransferMessage"] = createPair.Result.Message;
                return Json(ResultsItem.Success("Success"));
            }

            return Json(ResultsItem.Error(createPair.Result.Message));
        }
        
        // Password Reset: User forgot their password and need to reset it from their email.
        // Password Change: Logged in user wants to change their password. 
        public PartialViewResult PasswordResetForm()
        {
            return PartialView("_PasswordReset");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult PasswordResetForm(string email)
        {
            return Json(AuthorizationLogic.RequestPasswordReset(email));
        }

        [Route("ResetPassword")] // Confirm Reset Password - Should be reaching here from email.
        public async Task<IActionResult> ResetPassword(string username, string authCode)
        {
            ResultsItem pwResetAuthResult = await AuthorizationLogic.AuthorizeResetPassword(username, authCode);
            if (pwResetAuthResult.IsSuccess)
            {
                PasswordUpdateRequest request = new PasswordUpdateRequest
                {
                    Username = username,
                    EmailAuthCode = authCode,
                    AuthenticationHash = Utilities.GenerateHmacSHA256Hash($"{username}{authCode}_ptpwresetreq", "PTPWRESET")
                };
                TempData["ResetPasswordRequest"] = Utilities.Serialize(request);

                return View("ExecutePasswordReset", request);
            }

            return Content(pwResetAuthResult.Message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ChooseNewResetPassword(PasswordUpdateRequest passRequest)
        {
            if (passRequest.NewPassword.Length < 8 || passRequest.NewPassword.Length > 30) { return Json(ResultsItem.Error("Password changed failed: Password length must be from 8 - 30 characters.")); }
            if (passRequest.NewPassword != passRequest.ConfirmNewPassword) { return Json(ResultsItem.Error("Passwords does not match.")); }

            PasswordUpdateRequest savedRequest = TempData["ResetPasswordRequest"] == null ? null : Utilities.Deserialize<PasswordUpdateRequest>(TempData["ResetPasswordRequest"].ToString());
            if (savedRequest == null || string.IsNullOrEmpty(savedRequest.AuthenticationHash)) { return Json(ResultsItem.Error("Password reset form expired. Please request another password reset.")); }

            if (savedRequest.Username != passRequest.Username) { return Json(ResultsItem.Error("Passwords does not match.")); }
            if (Utilities.GenerateHmacSHA256Hash($"{savedRequest.Username}{savedRequest.EmailAuthCode}_ptpwresetreq", "PTPWRESET") != passRequest.AuthenticationHash) { return Json(ResultsItem.Error("Authentication failed.")); }

            // Change password
            var pwChangeResult = await AuthorizationLogic.ChangePassword(passRequest.Username, passRequest.NewPassword);
            if (!pwChangeResult.IsSuccess) { return Json(pwChangeResult); }

            string successMessage = "Your password has been successfully reset. Please login again";
            TempData["message"] = successMessage;
            return Json(ResultsItem.Success(successMessage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [TypeFilter(typeof(ValidWriteUserOnly))]
        public async Task<JsonResult> CurrentUserChangePassword(PegaUser user)
        {
            if (user.NewChangedPassword.Length < 8 || user.NewChangedPassword.Length > 30) { return Json(ResultsItem.Error("Password changed failed: Password length must be from 8 - 30 characters.")); }
            if (user.NewChangedPassword != user.ConfirmNewChangedPassword) { return Json(ResultsItem.Error("Passwords does not match.")); }
            if (CurrentUser.Username != user.Username) { return Json(ResultsItem.Error("Username does not match.")); }
            
            // do-later: check if current pw is valid first. if (!AuthorizationLogic.AuthorizeUser(CurrentUser.Username, user.Password))

            // Change password
            var pwChangeResult = await AuthorizationLogic.ChangePassword(user.Username, user.NewChangedPassword);
            if (!pwChangeResult.IsSuccess) { return Json(pwChangeResult); }

            HttpContext.Session.Clear();

            string successMessage = "Your password has been successfully reset. Please login again";
            TempData["message"] = successMessage;
            return Json(ResultsItem.Success(successMessage));
        }

        [Route("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string username, string authCode)
        {
            ResultsItem emailResult = await AuthorizationLogic.ConfirmEmail(username, authCode);
            TempData["message"] = emailResult.Message;
            return RedirectToAction("Index", "Home");
        }

        [TypeFilter(typeof(ValidWriteUserOnly))]
        public PartialViewResult UserAccountManagement()
        {
            return PartialView("_UserAccount", CurrentUser);
        }

        #endregion

        public ResultsItem NotValidWriteUser()
        {
            return ResultsItem.Error("This user is not allowed to perform this action. Please login or create an account.");
        }

        public string SessionTimeout()
        {
            return "Sorry... :/ We thought you were away, so we've logged you out. Please login again.";
        }
    }
}