﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Identity.Models;

namespace WebApp.Identity.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<MyUser> _userManager;
        private readonly IUserClaimsPrincipalFactory<MyUser> _userClaimsPrincipalFactory;
        private readonly SignInManager<MyUser> _signInManager;

        public HomeController(UserManager<MyUser> userManager,
            IUserClaimsPrincipalFactory<MyUser> userClaimsPrincipalFactory,
            SignInManager<MyUser> signInManager)
        {
            _userManager = userManager;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _signInManager = signInManager;
        }


        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.UserName);

                if (user != null && !await _userManager.IsLockedOutAsync(user))
                {

                    if (await _userManager.CheckPasswordAsync(
                    user, model.Password))
                    {
                        if (!await _userManager.IsEmailConfirmedAsync(user))
                        {
                            ModelState.AddModelError("", "E-mail não está válido!");
                            return View();
                        }

                        await _userManager.ResetAccessFailedCountAsync(user);

                        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

                        await HttpContext.SignInAsync("Identity.Application", principal);

                        return RedirectToAction("About");
                    }

                    await _userManager.AccessFailedAsync(user);

                    if(await _userManager.IsLockedOutAsync(user))
                    {
                        //Email deve ser enviado com sugestão para mudança de senha
                    }
                }

                ModelState.AddModelError("", "Usuário ou Senha Invalida.");

            }

            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }



        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var resetURL = Url.Action("ResetPassword", "Home",
                            new { token = token, email = model.Email }, Request.Scheme);

                    System.IO.File.WriteAllText("resetLink.txt", resetURL);
                    return View("Success");
                }
                else
                {
                    //Aqui você pode colocar falando com o usuário que a informação não foi encontrada
                }
            }

            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            return View(new ResetPasswordModel { Token = token, Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    var result = await _userManager.ResetPasswordAsync(user,
                        model.Token, model.Password);

                    if (!result.Succeeded)
                    {
                        foreach (var erro in result.Errors)
                        {
                            ModelState.AddModelError("", erro.Description);
                        }

                        return View();
                    }

                    return View("Success");
                }
                ModelState.AddModelError("", "Invalid Request");
            }
            return View();
        }



        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.UserName);

                if (user == null)
                {
                    user = new MyUser()
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = model.UserName,
                        Email = model.UserName,
                    };

                    var result = await _userManager.CreateAsync(
                        user, model.Password);


                    if (result.Succeeded)
                    {
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var confirmationEmail = Url.Action("ConfirmEmailAddress", "Home",
                            new { token = token, email = user.Email }, Request.Scheme);

                        System.IO.File.WriteAllText("confirmationEmail.txt", confirmationEmail);
                    }
                    else
                    {
                        foreach (var erro in result.Errors)
                        {
                            ModelState.AddModelError("", erro.Description);
                        }

                        return View();
                    }
                }

                return View("Success");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmailAddress(string token, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);

                if (result.Succeeded)
                {
                    return View("Success");
                }
            }

            return View();
        }



        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
