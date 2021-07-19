using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MusicStream.Models;
using MusicStream.ViewModel;
using NETCore.MailKit.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MusicStream.Controllers
{
    public class AccountController : Controller
    {
        private UserManager<User> userManager;
        private SignInManager<User> signInManager;
        private readonly IEmailService emailService;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, IEmailService emailService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.emailService = emailService;
        }
        [HttpGet]
        public ViewResult Register() { return View(); }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = new User { UserName = model.UserName, Email = model.Email };
                var result = await this.userManager.CreateAsync(user,model.Password);

                if(result.Succeeded)
                {
                    return generateEmailConfirmation(user);
                }
                else
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                }
            }
            return View();
        }
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await this.userManager.FindByNameAsync(model.UserName);
                if (emailConfirmation(user))
                {
                    return await loginUser(model, returnUrl);
                }
                else
                {
                    ModelState.AddModelError("", "Email not confirmed");
                    return View(model);
                }
            }
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
           
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }


        public IActionResult EmailVerification() => View();
        private bool emailConfirmation(User user)
        {
            return user != null && userManager.IsEmailConfirmedAsync(user).Result;
        }
        private IActionResult generateEmailConfirmation(User user)
        {
            var confirmationToken = this.userManager.GenerateEmailConfirmationTokenAsync(user).Result;
            var confirmationLink = Url.Action(nameof(VerifyEmail), "Account", new { userId = user.Id, token = confirmationToken }, protocol: HttpContext.Request.Scheme);
            var messageBody = $"Click the Link to verify email.  {confirmationLink}";
            //testing purposes only
            SmtpClient client = new SmtpClient();
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = @"c:/Test";
            client.Send("test@localhost", user.Email, "Confirm your MS account. Do not reply to this email.", messageBody);

            return RedirectToAction("EmailVerification");
        }
        public async Task<IActionResult> VerifyEmail(string userId, string token)
        {
            var user = await this.userManager.FindByIdAsync(userId);
            var result = await this.userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                ViewBag.Message = "Email confirmed successfully";
                return View("VerifiedEmail");
            }
            else
            {
                ViewBag.Message = "Error while confirming your email";
                return BadRequest();
            }
        }
        private async Task<IActionResult> loginUser(LoginViewModel model, string returnUrl)
        {
            var result = await this.signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, false);

            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }
            else
            {
                ModelState.AddModelError("", "Invalid login attempt");
                return View(model);
            }
        }
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }


    }
}
