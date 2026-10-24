using CaspianBank.Tests.Infrastructure;
using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Repository.Data;
using Service.Helpers.DTOs.Accounts;
using Service.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class AuthServiceTests
    {
        // Registry-də vətəndaş: hələ hesabı yoxdur
        private static async Task<Customer> NewCitizenAsync(TestApp app, string name = "Aysel", string surname = "Məmmədova", bool deceased = false)
        {
            using var scope = app.NewScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestData.EnsureOwnBankAsync(db);
            return await TestData.NewCustomerAsync(db, name, surname, new DateTime(1992, 3, 14), deceased);
        }

        // Email təsdiqi: kod göndər + yoxla, tokeni qaytarır
        private static async Task<string> VerifiedTokenAsync(TestApp app, string email)
        {
            using var scope = app.NewScope();
            var otp = scope.ServiceProvider.GetRequiredService<IOtpService>();
            var sent = await otp.SendOtpAsync(new SendOtpDto { Email = email });
            Assert.True(sent.IsSuccess, string.Join(";", sent.Errors));
            var verified = await otp.VerifyOtpAsync(new VerifyOtpDto { Email = email, Code = sent.DevCode! });
            Assert.True(verified.IsSuccess, string.Join(";", verified.Errors));
            return verified.VerificationToken!;
        }

        private static RegisterDto RegisterModel(Customer citizen, string email, string token, string? phone = null)
        {
            return new RegisterDto
            {
                Email = email,
                PhoneNumber = phone ?? "05" + (10000000 + TestData.Next() % 90000000).ToString("D8"),
                Fin = citizen.Fin,
                IdCardNumber = citizen.IdCardNumber,
                Name = citizen.Name,
                Surname = citizen.Surname,
                Password = TestData.Password,
                VerificationToken = token
            };
        }

        private static async Task<AppUser> RegisterAsync(TestApp app, Customer citizen, string email)
        {
            var token = await VerifiedTokenAsync(app, email);
            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().RegisterAsync(RegisterModel(citizen, email, token));
            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            return (await app.QueryAsync(db => db.Users.AsNoTracking().FirstAsync(u => u.Email == email)));
        }

        // ---------- Qeydiyyat ----------

        [Fact]
        public async Task Register_creates_the_account_cards_and_links_it_to_the_citizen()
        {
            var app = await TestApp.SharedAsync();
            var citizen = await NewCitizenAsync(app);
            var email = TestData.UniqueEmail("reg");
            var token = await VerifiedTokenAsync(app, email);

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().RegisterAsync(RegisterModel(citizen, email, token, "0501234567"));

            Assert.True(result.IsSuccess);
            var user = await app.QueryAsync(db => db.Users.AsNoTracking().FirstAsync(u => u.Email == email));
            Assert.Equal(citizen.Id, user.CustomerId);
            Assert.Equal(citizen.Fin, user.FinKod);
            Assert.Equal(citizen.Name, user.Name);
            Assert.Equal(citizen.Surname, user.Surname);
            Assert.Equal(citizen.BirthDate, user.BirthDay);
            Assert.Equal(email, user.UserName);
            Assert.True(user.EmailConfirmed);
            Assert.Equal("0501234567", user.PhoneNumber);
            Assert.False(user.IsRestricted);

            using var check = app.NewScope();
            var roles = await check.ServiceProvider.GetRequiredService<UserManager<AppUser>>().GetRolesAsync(user);
            Assert.Equal(new[] { Roles.Customer }, roles.ToArray());

            var cards = await app.QueryAsync(db => db.Cards.AsNoTracking().Where(c => c.UserId == user.Id).ToListAsync());
            Assert.Equal(2, cards.Count);
        }

        [Fact]
        public async Task Register_takes_name_and_birth_date_from_the_registry_not_from_the_form()
        {
            var app = await TestApp.SharedAsync();
            var citizen = await NewCitizenAsync(app, "Elçin", "Hüseynov");
            var email = TestData.UniqueEmail("reg");
            var token = await VerifiedTokenAsync(app, email);
            var model = RegisterModel(citizen, email, token);
            model.Name = "elcin";                       // yalnız yoxlama üçündür: diakritik və böyük-kiçik hərf fərqi yoxdur
            model.Surname = "HUSEYNOV";

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().RegisterAsync(model);

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            var user = await app.QueryAsync(db => db.Users.AsNoTracking().FirstAsync(u => u.Email == email));
            Assert.Equal("Elçin", user.Name);
            Assert.Equal("Hüseynov", user.Surname);
        }

        [Fact]
        public async Task Register_normalizes_the_phone_number_to_the_zero_form()
        {
            var app = await TestApp.SharedAsync();
            var citizen = await NewCitizenAsync(app);
            var email = TestData.UniqueEmail("reg");
            var token = await VerifiedTokenAsync(app, email);
            var phone = (10000000 + TestData.Next() % 90000000).ToString("D8");

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().RegisterAsync(RegisterModel(citizen, email, token, "5" + phone));

            Assert.True(result.IsSuccess);
            var user = await app.QueryAsync(db => db.Users.AsNoTracking().FirstAsync(u => u.Email == email));
            Assert.Equal("05" + phone, user.PhoneNumber);
        }

        [Fact]
        public async Task Register_needs_a_verified_email()
        {
            var app = await TestApp.SharedAsync();
            var citizen = await NewCitizenAsync(app);
            var email = TestData.UniqueEmail("reg");

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().RegisterAsync(RegisterModel(citizen, email, "made-up-token"));

            Assert.False(result.IsSuccess);
            Assert.Equal("Email verification is missing or has expired. Please verify your email again.", result.Errors[0]);
            Assert.False(await app.QueryAsync(db => db.Users.AnyAsync(u => u.Email == email)));
        }

        [Fact]
        public async Task A_verification_token_belongs_to_one_email()
        {
            var app = await TestApp.SharedAsync();
            var citizen = await NewCitizenAsync(app);
            var verifiedEmail = TestData.UniqueEmail("a");
            var otherEmail = TestData.UniqueEmail("b");
            var token = await VerifiedTokenAsync(app, verifiedEmail);

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().RegisterAsync(RegisterModel(citizen, otherEmail, token));

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Verification_token_works_once()
        {
            var app = await TestApp.SharedAsync();
            var citizen = await NewCitizenAsync(app);
            var email = TestData.UniqueEmail("reg");
            var token = await VerifiedTokenAsync(app, email);

            using (var scope = app.NewScope())
            {
                Assert.True((await scope.ServiceProvider.GetRequiredService<IAccountService>().RegisterAsync(RegisterModel(citizen, email, token))).IsSuccess);
            }

            using var again = app.NewScope();
            Assert.False(await again.ServiceProvider.GetRequiredService<IOtpService>().IsVerifiedAsync(email, token));
        }

        [Fact]
        public async Task Register_rejects_wrong_registry_data_with_one_general_message()
        {
            var app = await TestApp.SharedAsync();
            var citizen = await NewCitizenAsync(app);
            var email = TestData.UniqueEmail("reg");
            var token = await VerifiedTokenAsync(app, email);
            var generic = "We couldn't find a citizen with these details. Check the FIN, ID card number, first name and last name and try again.";

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IAccountService>();

            var wrongFin = RegisterModel(citizen, email, token);
            wrongFin.Fin = "ZZZZZZZ";
            var wrongCard = RegisterModel(citizen, email, token);
            wrongCard.IdCardNumber = "AZE99999999";
            var wrongName = RegisterModel(citizen, email, token);
            wrongName.Name = "Someone";
            var wrongSurname = RegisterModel(citizen, email, token);
            wrongSurname.Surname = "Else";

            foreach (var model in new[] { wrongFin, wrongCard, wrongName, wrongSurname })
            {
                var result = await service.RegisterAsync(model);
                Assert.False(result.IsSuccess);
                Assert.Equal(generic, result.Errors[0]);
            }
        }

        [Fact]
        public async Task Register_accepts_lowercase_fin_and_card_number()
        {
            var app = await TestApp.SharedAsync();
            var citizen = await NewCitizenAsync(app);
            var email = TestData.UniqueEmail("reg");
            var token = await VerifiedTokenAsync(app, email);
            var model = RegisterModel(citizen, email, token);
            model.Fin = citizen.Fin.ToLowerInvariant();
            model.IdCardNumber = citizen.IdCardNumber.ToLowerInvariant();

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().RegisterAsync(model);

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
        }

        [Fact]
        public async Task Register_refuses_a_deceased_citizen()
        {
            var app = await TestApp.SharedAsync();
            var citizen = await NewCitizenAsync(app, deceased: true);
            var email = TestData.UniqueEmail("reg");
            var token = await VerifiedTokenAsync(app, email);

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().RegisterAsync(RegisterModel(citizen, email, token));

            Assert.False(result.IsSuccess);
            Assert.Equal("This ID record can't be used to open an account. If you think this is a mistake, please contact the bank.", result.Errors[0]);
        }

        [Fact]
        public async Task Register_refuses_a_second_account_for_the_same_person()
        {
            var app = await TestApp.SharedAsync();
            var citizen = await NewCitizenAsync(app);
            await RegisterAsync(app, citizen, TestData.UniqueEmail("first"));

            var email = TestData.UniqueEmail("second");
            var token = await VerifiedTokenAsync(app, email);
            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().RegisterAsync(RegisterModel(citizen, email, token));

            Assert.False(result.IsSuccess);
            Assert.Equal("An account already exists for this person. Log in instead.", result.Errors[0]);
        }

        [Fact]
        public async Task Register_refuses_a_phone_number_that_is_already_used()
        {
            var app = await TestApp.SharedAsync();
            var first = await NewCitizenAsync(app);
            var second = await NewCitizenAsync(app, "Nigar", "Əliyeva");
            await RegisterAsync(app, first, TestData.UniqueEmail("first"));
            var used = await app.QueryAsync(db => db.Users.AsNoTracking().Where(u => u.CustomerId == first.Id).Select(u => u.PhoneNumber).FirstAsync());

            var email = TestData.UniqueEmail("second");
            var token = await VerifiedTokenAsync(app, email);
            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().RegisterAsync(RegisterModel(second, email, token, used));

            Assert.False(result.IsSuccess);
            Assert.Equal("This phone number is already used by another account.", result.Errors[0]);
        }

        [Fact]
        public async Task Register_rejects_a_weak_password_with_the_validator_message()
        {
            var app = await TestApp.SharedAsync();
            var citizen = await NewCitizenAsync(app);
            var email = TestData.UniqueEmail("reg");
            var token = await VerifiedTokenAsync(app, email);
            var model = RegisterModel(citizen, email, token);
            model.Password = "weak";

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().RegisterAsync(model);

            Assert.False(result.IsSuccess);
            Assert.Contains("Use at least 6 characters.", result.Errors);
            Assert.False(await app.QueryAsync(db => db.Users.AnyAsync(u => u.Email == email)));
        }

        // ---------- Giriş ----------

        [Fact]
        public async Task Login_returns_a_valid_token_with_the_role_and_name()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(0m, "Aysel", "Məmmədova");

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().LoginAsync(new LoginDto { Email = kit.User.Email!, Password = TestData.Password });

            Assert.True(result.IsSuccess);
            Assert.Equal(kit.User.Id, result.UserId);
            Assert.Equal("Aysel Məmmədova", result.FullName);
            Assert.Equal(new[] { Roles.Customer }, result.Roles);
            Assert.True(result.ExpiresAt > DateTime.UtcNow.AddMinutes(50));

            var parameters = new TokenValidationParameters
            {
                ValidIssuer = "CaspianBank.Test",
                ValidAudience = "CaspianBank.Web",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-key-test-key-test-key-test-key-1234567890")),
                ValidateLifetime = true
            };
            var principal = new JwtSecurityTokenHandler().ValidateToken(result.Token, parameters, out _);
            Assert.Equal(kit.User.Id, principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            Assert.Contains(principal.FindAll(ClaimTypes.Role), c => c.Value == Roles.Customer);
        }

        [Fact]
        public async Task Login_trims_the_email_and_ignores_its_case()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().LoginAsync(new LoginDto { Email = "  " + kit.User.Email!.ToUpperInvariant() + " ", Password = TestData.Password });

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task Wrong_password_and_unknown_email_give_the_same_message()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IAccountService>();
            var wrongPassword = await service.LoginAsync(new LoginDto { Email = kit.User.Email!, Password = "Wrong123!" });
            var unknown = await service.LoginAsync(new LoginDto { Email = "nobody@test.az", Password = "Wrong123!" });

            Assert.False(wrongPassword.IsSuccess);
            Assert.False(unknown.IsSuccess);
            Assert.Equal("Invalid email or password.", wrongPassword.Errors[0]);
            Assert.Equal(unknown.Errors[0], wrongPassword.Errors[0]);
            Assert.Null(wrongPassword.Token);
        }

        [Fact]
        public async Task Five_wrong_passwords_lock_the_account_even_for_the_right_password()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IAccountService>();
            for (var i = 0; i < 5; i++)
            {
                await service.LoginAsync(new LoginDto { Email = kit.User.Email!, Password = "Wrong123!" });
            }

            var result = await service.LoginAsync(new LoginDto { Email = kit.User.Email!, Password = TestData.Password });

            Assert.False(result.IsSuccess);
            Assert.Equal("Too many failed attempts. Try again in a few minutes.", result.Errors[0]);
        }

        [Fact]
        public async Task A_successful_login_resets_the_failed_attempts()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IAccountService>();
            for (var round = 0; round < 3; round++)
            {
                for (var i = 0; i < 4; i++)
                {
                    await service.LoginAsync(new LoginDto { Email = kit.User.Email!, Password = "Wrong123!" });
                }
                Assert.True((await service.LoginAsync(new LoginDto { Email = kit.User.Email!, Password = TestData.Password })).IsSuccess);
            }
        }

        [Fact]
        public async Task A_restricted_account_cannot_log_in_even_with_the_right_password()
        {
            var app = await TestApp.SharedAsync();
            AppUser user;
            using (var scope = app.NewScope())
            {
                user = await TestData.NewUserAsync(scope.ServiceProvider, null, Roles.Customer, restricted: true);
            }

            using var run = app.NewScope();
            var result = await run.ServiceProvider.GetRequiredService<IAccountService>().LoginAsync(new LoginDto { Email = user.Email!, Password = TestData.Password });

            Assert.False(result.IsSuccess);
            Assert.Equal("Your account is restricted. Please contact support.", result.Errors[0]);
        }

        [Fact]
        public async Task Staff_login_returns_the_staff_role()
        {
            var app = await TestApp.SharedAsync();
            AppUser user;
            using (var scope = app.NewScope())
            {
                user = await TestData.NewUserAsync(scope.ServiceProvider, null, Roles.Accountant);
            }

            using var run = app.NewScope();
            var result = await run.ServiceProvider.GetRequiredService<IAccountService>().LoginAsync(new LoginDto { Email = user.Email!, Password = TestData.Password });

            Assert.True(result.IsSuccess);
            Assert.Equal(new[] { Roles.Accountant }, result.Roles);
        }

        [Fact]
        public async Task Login_with_missing_fields_reports_validation_errors()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().LoginAsync(new LoginDto { Email = "", Password = "" });

            Assert.False(result.IsSuccess);
            Assert.Contains("Enter your email.", result.Errors);
            Assert.Contains("Enter your password.", result.Errors);
        }

        // ---------- Email və şəxsiyyət yoxlaması ----------

        [Fact]
        public async Task CheckEmail_tells_whether_the_account_exists()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IAccountService>();
            Assert.True((await service.CheckEmailAsync(new CheckEmailDto { Email = kit.User.Email! })).Exists);
            Assert.True((await service.CheckEmailAsync(new CheckEmailDto { Email = "  " + kit.User.Email!.ToUpperInvariant() + " " })).Exists);
            var missing = await service.CheckEmailAsync(new CheckEmailDto { Email = "nobody@test.az" });
            Assert.True(missing.IsSuccess);
            Assert.False(missing.Exists);
            Assert.False((await service.CheckEmailAsync(new CheckEmailDto { Email = "bad" })).IsSuccess);
        }

        [Fact]
        public async Task CheckIdentity_returns_the_masked_registry_card_for_the_confirmation_step()
        {
            var app = await TestApp.SharedAsync();
            var citizen = await NewCitizenAsync(app, "Aysel", "Məmmədova");
            await app.QueryAsync(async db =>
            {
                await db.Customers.Where(c => c.Id == citizen.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.PhotoUrl, "/citizens/x.jpg"));
                return 0;
            });

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().CheckIdentityAsync(new CheckIdentityDto
            {
                Fin = citizen.Fin,
                IdCardNumber = citizen.IdCardNumber,
                Name = "AYSEL",
                Surname = "memmedova"
            });

            Assert.True(result.IsSuccess);
            Assert.Equal("Aysel", result.Name);
            Assert.Equal("M.", result.MaskedSurname);
            Assert.Equal(1992, result.BirthYear);
            Assert.Equal("/citizens/x.jpg", result.PhotoUrl);
        }

        [Fact]
        public async Task CheckIdentity_does_not_reveal_which_field_was_wrong()
        {
            var app = await TestApp.SharedAsync();
            var citizen = await NewCitizenAsync(app);

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IAccountService>();
            var wrongFin = await service.CheckIdentityAsync(new CheckIdentityDto { Fin = "ZZZZZZZ", IdCardNumber = citizen.IdCardNumber, Name = citizen.Name, Surname = citizen.Surname });
            var wrongName = await service.CheckIdentityAsync(new CheckIdentityDto { Fin = citizen.Fin, IdCardNumber = citizen.IdCardNumber, Name = "Nobody", Surname = citizen.Surname });

            Assert.False(wrongFin.IsSuccess);
            Assert.Equal(wrongFin.Errors[0], wrongName.Errors[0]);
        }

        [Fact]
        public async Task CheckIdentity_refuses_the_deceased_and_people_who_already_have_an_account()
        {
            var app = await TestApp.SharedAsync();
            var dead = await NewCitizenAsync(app, deceased: true);
            var registered = await NewCitizenAsync(app, "Nigar", "Əliyeva");
            await RegisterAsync(app, registered, TestData.UniqueEmail("done"));

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IAccountService>();
            var deadResult = await service.CheckIdentityAsync(new CheckIdentityDto { Fin = dead.Fin, IdCardNumber = dead.IdCardNumber, Name = dead.Name, Surname = dead.Surname });
            var registeredResult = await service.CheckIdentityAsync(new CheckIdentityDto { Fin = registered.Fin, IdCardNumber = registered.IdCardNumber, Name = registered.Name, Surname = registered.Surname });

            Assert.Equal("This ID record can't be used to open an account. If you think this is a mistake, please contact the bank.", deadResult.Errors[0]);
            Assert.Equal("An account already exists for this person. Log in instead.", registeredResult.Errors[0]);
        }

        [Fact]
        public async Task CheckIdentity_reports_validation_errors()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAccountService>().CheckIdentityAsync(new CheckIdentityDto { Fin = "x", IdCardNumber = "y", Name = "", Surname = "" });

            Assert.False(result.IsSuccess);
            Assert.Contains("The FIN code must be 7 letters or digits.", result.Errors);
        }

        [Fact]
        public async Task GetProfile_returns_the_account_and_registry_photo()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(0m, "Aysel", "Məmmədova");
            await app.QueryAsync(async db =>
            {
                await db.Customers.Where(c => c.Id == kit.Customer.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.PhotoUrl, "/citizens/p.jpg"));
                return 0;
            });

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IAccountService>();
            var profile = await service.GetProfileAsync(kit.User.Id);

            Assert.NotNull(profile);
            Assert.Equal("Aysel", profile!.Name);
            Assert.Equal(kit.Customer.Fin, profile.Fin);
            Assert.Equal(kit.User.Email, profile.Email);
            Assert.Equal("/citizens/p.jpg", profile.PhotoUrl);
            Assert.Null(await service.GetProfileAsync("nobody"));
        }

        [Fact]
        public async Task GetProfile_of_a_staff_member_has_no_photo()
        {
            var app = await TestApp.SharedAsync();
            AppUser staff;
            using (var scope = app.NewScope())
            {
                staff = await TestData.NewUserAsync(scope.ServiceProvider, null, Roles.Admin);
            }

            using var run = app.NewScope();
            var profile = await run.ServiceProvider.GetRequiredService<IAccountService>().GetProfileAsync(staff.Id);

            Assert.NotNull(profile);
            Assert.Null(profile!.PhotoUrl);
        }

        // ---------- OTP ----------

        [Fact]
        public async Task SendOtp_emails_a_six_digit_code_and_returns_it_in_development()
        {
            var app = await TestApp.SharedAsync();
            var email = TestData.UniqueEmail("otp");

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IOtpService>().SendOtpAsync(new SendOtpDto { Email = email });

            Assert.True(result.IsSuccess);
            Assert.Matches(@"^\d{6}$", result.DevCode!);
            var mail = app.Emails.LastTo(email)!;
            Assert.Equal("Your Caspian Bank verification code", mail.Subject);
            Assert.Equal(result.DevCode, mail.Code);
        }

        [Fact]
        public async Task SendOtp_stores_only_a_hash_of_the_code()
        {
            var app = await TestApp.SharedAsync();
            var email = TestData.UniqueEmail("otp");

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IOtpService>().SendOtpAsync(new SendOtpDto { Email = email });

            var row = await app.QueryAsync(db => db.EmailOtps.AsNoTracking().FirstAsync(o => o.Email == email));
            Assert.DoesNotContain(result.DevCode!, row.CodeHash);
            Assert.Equal(64, row.CodeHash.Length);
            Assert.True(row.ExpiresAt > DateTime.UtcNow.AddMinutes(4));
            Assert.True(row.ExpiresAt <= DateTime.UtcNow.AddMinutes(5));
        }

        [Fact]
        public async Task SendOtp_refuses_an_email_that_is_already_registered()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IOtpService>().SendOtpAsync(new SendOtpDto { Email = kit.User.Email! });

            Assert.False(result.IsSuccess);
            Assert.Equal("This email is already registered. Please log in.", result.Errors[0]);
            Assert.Null(app.Emails.LastTo(kit.User.Email!));
        }

        [Fact]
        public async Task SendOtp_makes_the_user_wait_a_minute_before_a_new_code()
        {
            var app = await TestApp.SharedAsync();
            var email = TestData.UniqueEmail("otp");

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IOtpService>();
            await service.SendOtpAsync(new SendOtpDto { Email = email });
            var second = await service.SendOtpAsync(new SendOtpDto { Email = email });

            Assert.False(second.IsSuccess);
            Assert.InRange(second.RetryAfterSeconds, 1, 60);
            Assert.Contains("Please wait", second.Errors[0]);
        }

        [Fact]
        public async Task SendOtp_reports_a_clear_error_when_the_email_cannot_be_sent()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                app.Emails.ShouldFail = true;
                using var scope = app.NewScope();
                var result = await scope.ServiceProvider.GetRequiredService<IOtpService>().SendOtpAsync(new SendOtpDto { Email = "someone@test.az" });

                Assert.False(result.IsSuccess);
                Assert.Equal("We could not send the email. Please try again later.", result.Errors[0]);
            }
        }

        [Fact]
        public async Task SendOtp_normalizes_the_email()
        {
            var app = await TestApp.SharedAsync();
            var email = TestData.UniqueEmail("otp");

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IOtpService>();
            var sent = await service.SendOtpAsync(new SendOtpDto { Email = "  " + email.ToUpperInvariant() + "  " });
            var verified = await service.VerifyOtpAsync(new VerifyOtpDto { Email = email, Code = sent.DevCode! });

            Assert.True(verified.IsSuccess);
        }

        [Fact]
        public async Task VerifyOtp_with_the_right_code_returns_a_token_and_uses_the_code_up()
        {
            var app = await TestApp.SharedAsync();
            var email = TestData.UniqueEmail("otp");

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IOtpService>();
            var sent = await service.SendOtpAsync(new SendOtpDto { Email = email });
            var verified = await service.VerifyOtpAsync(new VerifyOtpDto { Email = email, Code = sent.DevCode! });

            Assert.True(verified.IsSuccess);
            Assert.False(string.IsNullOrEmpty(verified.VerificationToken));
            Assert.True(await service.IsVerifiedAsync(email, verified.VerificationToken!));

            var again = await service.VerifyOtpAsync(new VerifyOtpDto { Email = email, Code = sent.DevCode! });
            Assert.False(again.IsSuccess);
        }

        [Fact]
        public async Task VerifyOtp_with_a_wrong_code_fails()
        {
            var app = await TestApp.SharedAsync();
            var email = TestData.UniqueEmail("otp");

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IOtpService>();
            var sent = await service.SendOtpAsync(new SendOtpDto { Email = email });
            var wrong = sent.DevCode == "000000" ? "111111" : "000000";
            var result = await service.VerifyOtpAsync(new VerifyOtpDto { Email = email, Code = wrong });

            Assert.False(result.IsSuccess);
            Assert.Equal("The code is incorrect.", result.Errors[0]);
            Assert.Null(result.VerificationToken);
        }

        [Fact]
        public async Task VerifyOtp_is_blocked_after_too_many_attempts_even_with_the_right_code()
        {
            var app = await TestApp.SharedAsync();
            var email = TestData.UniqueEmail("otp");

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IOtpService>();
            var sent = await service.SendOtpAsync(new SendOtpDto { Email = email });
            var wrong = sent.DevCode == "000000" ? "111111" : "000000";
            for (var i = 0; i < 5; i++)
            {
                await service.VerifyOtpAsync(new VerifyOtpDto { Email = email, Code = wrong });
            }

            var sixth = await service.VerifyOtpAsync(new VerifyOtpDto { Email = email, Code = sent.DevCode! });

            Assert.False(sixth.IsSuccess);
            Assert.Equal("Too many attempts. Please request a new code.", sixth.Errors[0]);
        }

        [Fact]
        public async Task VerifyOtp_fails_for_an_expired_code()
        {
            var app = await TestApp.SharedAsync();
            var email = TestData.UniqueEmail("otp");

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IOtpService>();
            var sent = await service.SendOtpAsync(new SendOtpDto { Email = email });
            await app.QueryAsync(async db =>
            {
                await db.EmailOtps.Where(o => o.Email == email).ExecuteUpdateAsync(s => s.SetProperty(o => o.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
                return 0;
            });

            var result = await service.VerifyOtpAsync(new VerifyOtpDto { Email = email, Code = sent.DevCode! });

            Assert.False(result.IsSuccess);
            Assert.Equal("The code has expired or is invalid. Please request a new one.", result.Errors[0]);
        }

        [Fact]
        public async Task A_new_code_cancels_the_previous_one()
        {
            var app = await TestApp.SharedAsync();
            var email = TestData.UniqueEmail("otp");

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IOtpService>();
            var first = await service.SendOtpAsync(new SendOtpDto { Email = email });
            // cooldown-u keçmək üçün birinci kodun vaxtı geri çəkilir
            await app.QueryAsync(async db =>
            {
                await db.EmailOtps.Where(o => o.Email == email).ExecuteUpdateAsync(s => s.SetProperty(o => o.CreatedAt, DateTime.UtcNow.AddMinutes(-2)));
                return 0;
            });
            using var later = app.NewScope();
            service = later.ServiceProvider.GetRequiredService<IOtpService>();      // yeni scope: köhnə scope-un izlədiyi sətir təzələnmir
            var second = await service.SendOtpAsync(new SendOtpDto { Email = email });

            Assert.True(second.IsSuccess);
            if (first.DevCode != second.DevCode)
            {
                Assert.False((await service.VerifyOtpAsync(new VerifyOtpDto { Email = email, Code = first.DevCode! })).IsSuccess);
            }
            Assert.True((await service.VerifyOtpAsync(new VerifyOtpDto { Email = email, Code = second.DevCode! })).IsSuccess);
        }

        [Fact]
        public async Task VerifyOtp_validates_the_input()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IOtpService>().VerifyOtpAsync(new VerifyOtpDto { Email = "x@test.az", Code = "12" });

            Assert.False(result.IsSuccess);
            Assert.Contains("The code must be 6 digits.", result.Errors);
        }

        [Fact]
        public async Task A_verification_token_stops_working_after_fifteen_minutes()
        {
            var app = await TestApp.SharedAsync();
            var email = TestData.UniqueEmail("otp");
            var token = await VerifiedTokenAsync(app, email);
            await app.QueryAsync(async db =>
            {
                await db.EmailOtps.Where(o => o.Email == email).ExecuteUpdateAsync(s => s.SetProperty(o => o.VerifiedAt, DateTime.UtcNow.AddMinutes(-16)));
                return 0;
            });

            using var scope = app.NewScope();
            Assert.False(await scope.ServiceProvider.GetRequiredService<IOtpService>().IsVerifiedAsync(email, token));
        }

        [Fact]
        public async Task IsVerified_is_false_for_an_empty_token()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IOtpService>();
            Assert.False(await service.IsVerifiedAsync("a@test.az", ""));
            Assert.False(await service.IsVerifiedAsync("a@test.az", "   "));
        }

        [Fact]
        public async Task A_register_code_cannot_confirm_a_card_action_and_the_other_way_round()
        {
            var app = await TestApp.SharedAsync();
            var email = TestData.UniqueEmail("otp");

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IOtpService>();
            var sent = await service.SendOtpAsync(new SendOtpDto { Email = email });

            var asCardCode = await service.ConfirmCodeAsync(email, OtpPurposes.CardBlock(1), sent.DevCode!);

            Assert.False(asCardCode.IsSuccess);
            Assert.True((await service.VerifyOtpAsync(new VerifyOtpDto { Email = email, Code = sent.DevCode! })).IsSuccess);
        }

        // ---------- Şifrə bərpası ----------

        [Fact]
        public async Task ForgotPassword_emails_a_reset_link_for_a_known_account()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IPasswordService>().ForgotPasswordAsync(new ForgotPasswordDto { Email = kit.User.Email! });

            Assert.True(result.IsSuccess);
            var mail = app.Emails.LastTo(kit.User.Email!)!;
            Assert.Equal("Reset your Caspian Bank password", mail.Subject);
            Assert.StartsWith("https://frontend.test/Account/ResetPassword?email=", mail.Link);
            Assert.Equal(mail.Link, result.DevLink);
        }

        [Fact]
        public async Task ForgotPassword_gives_the_same_answer_for_an_unknown_email_and_sends_nothing()
        {
            var app = await TestApp.SharedAsync();
            var email = TestData.UniqueEmail("ghost");

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<IPasswordService>().ForgotPasswordAsync(new ForgotPasswordDto { Email = email });

            Assert.True(result.IsSuccess);
            Assert.Null(result.DevLink);
            Assert.Null(app.Emails.LastTo(email));
        }

        [Fact]
        public async Task ForgotPassword_sends_nothing_to_a_restricted_account()
        {
            var app = await TestApp.SharedAsync();
            AppUser user;
            using (var scope = app.NewScope())
            {
                user = await TestData.NewUserAsync(scope.ServiceProvider, null, Roles.Customer, restricted: true);
            }

            using var run = app.NewScope();
            var result = await run.ServiceProvider.GetRequiredService<IPasswordService>().ForgotPasswordAsync(new ForgotPasswordDto { Email = user.Email! });

            Assert.True(result.IsSuccess);
            Assert.Null(app.Emails.LastTo(user.Email!));
        }

        [Fact]
        public async Task ForgotPassword_still_answers_success_when_the_email_server_fails()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewKitAsync();
                app.Emails.ShouldFail = true;

                using var scope = app.NewScope();
                var result = await scope.ServiceProvider.GetRequiredService<IPasswordService>().ForgotPasswordAsync(new ForgotPasswordDto { Email = kit.User.Email! });

                Assert.True(result.IsSuccess);
            }
        }

        private static async Task<(string Email, string Token)> ResetLinkAsync(TestApp app, string email)
        {
            using var scope = app.NewScope();
            await scope.ServiceProvider.GetRequiredService<IPasswordService>().ForgotPasswordAsync(new ForgotPasswordDto { Email = email });
            var link = app.Emails.LastTo(email)!.Link!;
            var query = System.Web.HttpUtility.ParseQueryString(new Uri(link).Query);
            return (query["email"]!, query["token"]!);
        }

        [Fact]
        public async Task ResetPassword_with_the_link_changes_the_password()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var (email, token) = await ResetLinkAsync(app, kit.User.Email!);

            using var scope = app.NewScope();
            var reset = await scope.ServiceProvider.GetRequiredService<IPasswordService>().ResetPasswordAsync(new ResetPasswordDto { Email = email, Token = token, NewPassword = "Brand#New1" });

            Assert.True(reset.IsSuccess, string.Join(";", reset.Errors));
            var account = scope.ServiceProvider.GetRequiredService<IAccountService>();
            Assert.False((await account.LoginAsync(new LoginDto { Email = kit.User.Email!, Password = TestData.Password })).IsSuccess);
            Assert.True((await account.LoginAsync(new LoginDto { Email = kit.User.Email!, Password = "Brand#New1" })).IsSuccess);
        }

        [Fact]
        public async Task A_reset_link_works_only_once()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var (email, token) = await ResetLinkAsync(app, kit.User.Email!);

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IPasswordService>();
            await service.ResetPasswordAsync(new ResetPasswordDto { Email = email, Token = token, NewPassword = "Brand#New1" });
            var second = await service.ResetPasswordAsync(new ResetPasswordDto { Email = email, Token = token, NewPassword = "Other#New2" });

            Assert.False(second.IsSuccess);
            Assert.Equal("This reset link is invalid or has expired.", second.Errors[0]);
        }

        [Fact]
        public async Task ResetPassword_rejects_a_broken_token_and_an_unknown_email_with_the_same_message()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<IPasswordService>();
            var garbage = await service.ResetPasswordAsync(new ResetPasswordDto { Email = kit.User.Email!, Token = "not-a-real-token", NewPassword = "Brand#New1" });
            var notBase64 = await service.ResetPasswordAsync(new ResetPasswordDto { Email = kit.User.Email!, Token = "***", NewPassword = "Brand#New1" });
            var unknown = await service.ResetPasswordAsync(new ResetPasswordDto { Email = "nobody@test.az", Token = "abcd", NewPassword = "Brand#New1" });

            Assert.Equal("This reset link is invalid or has expired.", garbage.Errors[0]);
            Assert.Equal("This reset link is invalid or has expired.", notBase64.Errors[0]);
            Assert.Equal("This reset link is invalid or has expired.", unknown.Errors[0]);
        }

        [Fact]
        public async Task ResetPassword_rejects_a_weak_new_password_and_keeps_the_old_one()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var (email, token) = await ResetLinkAsync(app, kit.User.Email!);

            using var scope = app.NewScope();
            var weak = await scope.ServiceProvider.GetRequiredService<IPasswordService>().ResetPasswordAsync(new ResetPasswordDto { Email = email, Token = token, NewPassword = "weak" });

            Assert.False(weak.IsSuccess);
            Assert.True((await scope.ServiceProvider.GetRequiredService<IAccountService>().LoginAsync(new LoginDto { Email = kit.User.Email!, Password = TestData.Password })).IsSuccess);
        }

        [Fact]
        public async Task ResetPassword_unlocks_an_account_that_was_locked_out()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            using (var scope = app.NewScope())
            {
                var account = scope.ServiceProvider.GetRequiredService<IAccountService>();
                for (var i = 0; i < 5; i++)
                {
                    await account.LoginAsync(new LoginDto { Email = kit.User.Email!, Password = "Wrong123!" });
                }
                Assert.False((await account.LoginAsync(new LoginDto { Email = kit.User.Email!, Password = TestData.Password })).IsSuccess);
            }

            var (email, token) = await ResetLinkAsync(app, kit.User.Email!);
            using var run = app.NewScope();
            await run.ServiceProvider.GetRequiredService<IPasswordService>().ResetPasswordAsync(new ResetPasswordDto { Email = email, Token = token, NewPassword = "Brand#New1" });

            Assert.True((await run.ServiceProvider.GetRequiredService<IAccountService>().LoginAsync(new LoginDto { Email = kit.User.Email!, Password = "Brand#New1" })).IsSuccess);
        }

        // ---------- JWT ----------

        [Fact]
        public async Task CreateToken_contains_the_identity_and_all_roles()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            var user = new AppUser { Id = "u1", Email = "a@test.az", Name = "Nurlan", Surname = "Efendiyev" };

            var (token, expiresAt) = scope.ServiceProvider.GetRequiredService<ITokenService>().CreateToken(user, new[] { Roles.Admin, Roles.Security });

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            Assert.Equal("CaspianBank.Test", jwt.Issuer);
            Assert.Contains("CaspianBank.Web", jwt.Audiences);
            Assert.Equal("u1", jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.Equal("a@test.az", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
            Assert.Equal("Nurlan Efendiyev", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
            Assert.Equal(new[] { Roles.Admin, Roles.Security }, jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).OrderBy(r => r).ToArray());
            Assert.InRange((expiresAt - DateTime.UtcNow).TotalMinutes, 59, 60.1);
        }

        [Fact]
        public async Task Every_token_has_its_own_id()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var user = new AppUser { Id = "u1", Email = "a@test.az" };

            var first = service.CreateToken(user, new[] { Roles.Customer }).Token;
            var second = service.CreateToken(user, new[] { Roles.Customer }).Token;

            Assert.NotEqual(first, second);
        }

        [Fact]
        public async Task The_hub_token_is_short_lived_and_has_a_different_audience()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ITokenService>();

            var (token, expiresAt) = service.CreateHubToken("u1");
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Contains("CaspianBank.Web.Hub", jwt.Audiences);
            Assert.DoesNotContain("CaspianBank.Web", jwt.Audiences);
            Assert.True(expiresAt <= DateTime.UtcNow.AddMinutes(2.1));
            Assert.Equal("u1", jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.DoesNotContain(jwt.Claims, c => c.Type == ClaimTypes.Role);
        }

        [Fact]
        public async Task The_hub_token_is_not_accepted_as_an_api_token()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            var (token, _) = scope.ServiceProvider.GetRequiredService<ITokenService>().CreateHubToken("u1");

            var parameters = new TokenValidationParameters
            {
                ValidIssuer = "CaspianBank.Test",
                ValidAudience = "CaspianBank.Web",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-key-test-key-test-key-test-key-1234567890"))
            };

            Assert.Throws<SecurityTokenInvalidAudienceException>(() => new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _));
        }
    }
}
