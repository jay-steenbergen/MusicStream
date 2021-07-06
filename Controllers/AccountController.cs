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
        [HttpGet]
        public IActionResult Login(string returnURl="")
        {
            var model = new LoginViewModel { ReturnUrl = returnURl };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = new User { UserName = model.UserName, Email = model.Email };
                var result = await this.userManager.CreateAsync(user,model.Password);

                if(result.Succeeded)
                {
                    var confirmationToken = this.userManager.GenerateEmailConfirmationTokenAsync(user).Result;
                    var confirmationLink = Url.Action(nameof(VerifyEmail), "Account", new {userId = user.Id, token = confirmationToken }, protocol: HttpContext.Request.Scheme);

                    //testing purposes only
                    SmtpClient client = new SmtpClient();
                    client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                    client.PickupDirectoryLocation = @"c:/Test";
                    client.Send("test@localhost", user.Email, "Confirm your emial", confirmationLink);
                    
                    //await this.emailService.SendAsync("test@test.com", "Emial Verify",$"<a/ href=\"{link}\">Verify Email</a>", true);

                    return RedirectToAction("EmailVerification");
                    //await this.signInManager.SignInAsync(user, false);   old code that works
                    //    return RedirectToAction("Index", "Home");                
                }
                else
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                }
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if(ModelState.IsValid)
            {
                //check if email is verified
                var user = await this.userManager.FindByNameAsync(model.UserName);
                //if (user != null)
                //{
                //    if (!userManager.IsEmailConfirmedAsync(user).Result)
                //    {
                //        ModelState.AddModelError("", "Email not confirmed");
                //        return View(model);
                //    }
                //}

                //login user
                var result = await this.signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                        return Redirect(model.ReturnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            ModelState.AddModelError("", "Invalid login attempt");
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
           
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        public IActionResult EmailVerification() => View();
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
                ViewBag.Message = "Error while confirming your emial";
                return BadRequest();
            }
        }
        
    }
}
