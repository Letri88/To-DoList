using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using To_doList.Models;
using To_doList.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;

namespace To_doList.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUserModel> _userManager;
        private readonly SignInManager<AppUserModel> _signInManager;

        // DI đúng 2 cái này
        public AccountController(UserManager<AppUserModel> userManager, SignInManager<AppUserModel> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new AppUserModel
                {
                    UserName = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    TempData["success"] = "Đăng ký tài khoản thành công!";
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: /Account/Login
        public IActionResult Login(string returnUrl = "/")
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // GET: /Account/LoginByGoogle
        public IActionResult LoginByGoogle(string returnUrl = "/")
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account", new { returnUrl }, Request.Scheme);

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                GoogleDefaults.AuthenticationScheme, redirectUrl);

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // GET: /Account/GoogleResponse
        public async Task<IActionResult> GoogleResponse(string returnUrl = "/")
        {
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);

            if (result?.Principal == null)
            {
                TempData["error"] = "Đăng nhập Google thất bại!";
                return RedirectToAction("Login");
            }

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);
            var googleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (email == null)
            {
                TempData["error"] = "Không lấy được email từ Google!";
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // Tạo user mới từ Google
                user = new AppUserModel
                {
                    UserName = email,                    // Dùng email làm UserName (an toàn hơn)
                    Email = email,
                    FullName = name ?? "Người dùng Google",
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    TempData["error"] = "Tạo tài khoản thất bại!";
                    return RedirectToAction("Login");
                }

                // Gán external login (rất quan trọng để lần sau login nhanh hơn)
                var info = new UserLoginInfo("Google", googleId!, "Google");
                await _userManager.AddLoginAsync(user, info);

                TempData["success"] = "Đăng ký Google thành công!";
            }

            // Đăng nhập người dùng
            await _signInManager.SignInAsync(user, isPersistent: false);

            return LocalRedirect(returnUrl);
        }

        // Đăng xuất
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
