// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Diary.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        // ZMÌNA: _emailStore už není potøeba, protože nebudeme primárnì pracovat s e-mailem
        // private readonly IUserEmailStore<IdentityUser> _emailStore; 
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender; // Stále ponecháme, pokud chcete posílat potvrzovací e-maily (viz níže)

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender) // ZMÌNA: EmailSender je zde stále, pokud ho potøebujete pro odesílání e-mailù.
        {
            _userManager = userManager;
            _userStore = userStore;
            // ZMÌNA: GetEmailStore() už se zde nevolá, protože _emailStore není potøeba
            // _emailStore = GetEmailStore(); 
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Uživatelské jméno je povinné.")]
            // ZMÌNA: [EmailAddress] atribut je zakomentován/smazán
            //[EmailAddress] 
            [Display(Name = "Uživatelské jméno")]
            public string UserName { get; set; } // Vlastnost je správnì pojmenována na UserName

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Heslo je povinné.")]
            [StringLength(100, ErrorMessage = "{0} musí být alespoò {2} a maximálnì {1} znakù dlouhé.", MinimumLength = 1)]
            [DataType(DataType.Password)]
            [Display(Name = "Heslo")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Potvrïte heslo")]
            [Compare("Password", ErrorMessage = "Heslo a potvrzovací heslo se neshodují.")]
            public string ConfirmPassword { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = CreateUser();

                // ZMÌNA: Nastavujeme UserName z Input.UserName
                await _userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);

                // ZMÌNA: Nastavujeme Email na null, protože options.User.RequireUniqueEmail = false;
                // a nechceme ho pro pøihlášení.
                //await _userStore.SetEmailAsync(user, null, CancellationToken.None);
                user.EmailConfirmed = false; // Úèet není potvrzen e-mailem

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // ZMÌNA: Tento blok pro odesílání potvrzovacího e-mailu je nyní volitelný
                    // Vzhledem k tomu, že options.SignIn.RequireConfirmedAccount = false;
                    // a e-mail již není primární identifikátor, mùžete tento blok
                    // úplnì odstranit, pokud nechcete potvrzování e-mailem.
                    // Pokud ho necháte, ale neposkytnete skuteèný EmailSender, mùže to selhat.

                    /*
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.RequestUri.Scheme); // ZMÌNA: Používáme Request.Scheme

                    // Pokud jste _emailSender odebrali z konstruktoru, tento øádek by vyvolal chybu.
                    await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                    */

                    // ZMÌNA: Protože RequireConfirmedAccount = false, tento IF blok se neprovede,
                    // ale pro jistotu jsem pøidal komentáø.
                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        // ZMÌNA: Pokud by se nìkdy zmìnilo RequireConfirmedAccount na true
                        // a nemáte reálný e-mail, tato stránka by nemìla smysl.
                        // Pokud byste pøesto chtìl na tuto stránku pøesmìrovat,
                        // musel byste zajistit, že 'email' parametr je platný.
                        return RedirectToPage("RegisterConfirmation", new { email = user.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        // Automatické pøihlášení po registraci
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        // ZMÌNA: Tuto metodu GetEmailStore() mùžeme smazat, protože už _emailStore nepoužíváme.
        /*
        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
        */
    }
}