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
        // ZM�NA: _emailStore u� nen� pot�eba, proto�e nebudeme prim�rn� pracovat s e-mailem
        // private readonly IUserEmailStore<IdentityUser> _emailStore; 
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender; // St�le ponech�me, pokud chcete pos�lat potvrzovac� e-maily (viz n�e)

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender) // ZM�NA: EmailSender je zde st�le, pokud ho pot�ebujete pro odes�l�n� e-mail�.
        {
            _userManager = userManager;
            _userStore = userStore;
            // ZM�NA: GetEmailStore() u� se zde nevol�, proto�e _emailStore nen� pot�eba
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
            [Required(ErrorMessage = "U�ivatelsk� jm�no je povinn�.")]
            // ZM�NA: [EmailAddress] atribut je zakomentov�n/smaz�n
            //[EmailAddress] 
            [Display(Name = "U�ivatelsk� jm�no")]
            public string UserName { get; set; } // Vlastnost je spr�vn� pojmenov�na na UserName

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Heslo je povinn�.")]
            [StringLength(100, ErrorMessage = "{0} mus� b�t alespo� {2} a maxim�ln� {1} znak� dlouh�.", MinimumLength = 1)]
            [DataType(DataType.Password)]
            [Display(Name = "Heslo")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Potvr�te heslo")]
            [Compare("Password", ErrorMessage = "Heslo a potvrzovac� heslo se neshoduj�.")]
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

                // ZM�NA: Nastavujeme UserName z Input.UserName
                await _userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);

                // ZM�NA: Nastavujeme Email na null, proto�e options.User.RequireUniqueEmail = false;
                // a nechceme ho pro p�ihl�en�.
                //await _userStore.SetEmailAsync(user, null, CancellationToken.None);
                user.EmailConfirmed = false; // ��et nen� potvrzen e-mailem

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // ZM�NA: Tento blok pro odes�l�n� potvrzovac�ho e-mailu je nyn� voliteln�
                    // Vzhledem k tomu, �e options.SignIn.RequireConfirmedAccount = false;
                    // a e-mail ji� nen� prim�rn� identifik�tor, m��ete tento blok
                    // �pln� odstranit, pokud nechcete potvrzov�n� e-mailem.
                    // Pokud ho nech�te, ale neposkytnete skute�n� EmailSender, m��e to selhat.

                    /*
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.RequestUri.Scheme); // ZM�NA: Pou��v�me Request.Scheme

                    // Pokud jste _emailSender odebrali z konstruktoru, tento ��dek by vyvolal chybu.
                    await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                    */

                    // ZM�NA: Proto�e RequireConfirmedAccount = false, tento IF blok se neprovede,
                    // ale pro jistotu jsem p�idal koment��.
                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        // ZM�NA: Pokud by se n�kdy zm�nilo RequireConfirmedAccount na true
                        // a nem�te re�ln� e-mail, tato str�nka by nem�la smysl.
                        // Pokud byste p�esto cht�l na tuto str�nku p�esm�rovat,
                        // musel byste zajistit, �e 'email' parametr je platn�.
                        return RedirectToPage("RegisterConfirmation", new { email = user.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        // Automatick� p�ihl�en� po registraci
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

        // ZM�NA: Tuto metodu GetEmailStore() m��eme smazat, proto�e u� _emailStore nepou��v�me.
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