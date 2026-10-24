using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace CaspianBank.Tests.Api
{
    // Hər controller-in giriş qaydası yoxlanılır: səhvən [Authorize] unudulsa test qırılır
    public class ControllerSecurityTests
    {
        private static readonly string[] PublicClientControllers =
        {
            "AccountController", "CardTiersController", "HomeController", "SettingsController", "I18nController"
        };

        private static IEnumerable<Type> Controllers(string ns)
        {
            return typeof(CaspianBank_API_FinalProject.Middlewares.ExceptionHandlingMiddleware).Assembly
                .GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace != null && t.Namespace.EndsWith(ns));
        }

        private static IEnumerable<MethodInfo> Actions(Type controller)
        {
            return controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes().Any(a => a is HttpMethodAttribute));
        }

        [Fact]
        public void Controllers_were_found()
        {
            Assert.NotEmpty(Controllers(".Client"));
            Assert.NotEmpty(Controllers(".Admin"));
        }

        [Fact]
        public void Every_admin_controller_is_role_restricted()
        {
            foreach (Type c in Controllers(".Admin"))
            {
                AuthorizeAttribute? attr = c.GetCustomAttribute<AuthorizeAttribute>();
                Assert.True(attr != null && !string.IsNullOrWhiteSpace(attr.Roles), c.Name + " must have [Authorize(Roles=...)]");
                Assert.Null(c.GetCustomAttribute<AllowAnonymousAttribute>());
            }
        }

        [Fact]
        public void Admin_controllers_never_allow_customers()
        {
            foreach (Type c in Controllers(".Admin"))
            {
                string roles = c.GetCustomAttribute<AuthorizeAttribute>()!.Roles ?? "";
                Assert.DoesNotContain("Customer", roles.Replace("CustomerSupport", ""));
            }
        }

        [Fact]
        public void Admin_actions_are_not_anonymous()
        {
            foreach (Type c in Controllers(".Admin"))
            {
                foreach (MethodInfo m in Actions(c))
                {
                    Assert.Null(m.GetCustomAttribute<AllowAnonymousAttribute>());
                }
            }
        }

        [Fact]
        public void Admin_routes_live_under_api_admin()
        {
            foreach (Type c in Controllers(".Admin"))
            {
                RouteAttribute? route = c.GetCustomAttribute<RouteAttribute>();
                Assert.True(route != null && route.Template.StartsWith("api/admin/"), c.Name + " route must start with api/admin/");
            }
        }

        [Fact]
        public void Client_controllers_require_login_except_the_public_list()
        {
            foreach (Type c in Controllers(".Client"))
            {
                bool isPublic = PublicClientControllers.Contains(c.Name);
                bool hasAuthorize = c.GetCustomAttribute<AuthorizeAttribute>() != null;
                bool allowAnon = c.GetCustomAttribute<AllowAnonymousAttribute>() != null;

                if (isPublic)
                {
                    continue;
                }

                Assert.True(hasAuthorize && !allowAnon, c.Name + " must require login");
            }
        }

        [Fact]
        public void Account_actions_that_touch_the_session_require_login()
        {
            Type account = Controllers(".Client").Single(c => c.Name == "AccountController");
            string[] protectedActions = { "Logout", "Me", "ChangePassword", "Profile", "Sessions" };

            foreach (MethodInfo m in Actions(account))
            {
                bool anonymousByDesign = m.GetCustomAttribute<AuthorizeAttribute>() == null;
                if (protectedActions.Any(p => m.Name.Contains(p, StringComparison.OrdinalIgnoreCase)))
                {
                    Assert.False(anonymousByDesign, "Account." + m.Name + " must be [Authorize]");
                }
            }
        }

        [Fact]
        public void Public_login_style_actions_are_rate_limited()
        {
            Type account = Controllers(".Client").Single(c => c.Name == "AccountController");
            foreach (MethodInfo m in Actions(account))
            {
                if (m.GetCustomAttribute<AuthorizeAttribute>() == null)
                {
                    Assert.NotNull(m.GetCustomAttribute<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>());
                }
            }
        }

        [Fact]
        public void Money_moving_actions_are_rate_limited()
        {
            string[] controllers = { "TransfersController", "LoansController", "DepositsController", "BillsController" };
            foreach (Type c in Controllers(".Client").Where(x => controllers.Contains(x.Name)))
            {
                bool classLevel = c.GetCustomAttribute<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>() != null;
                foreach (MethodInfo m in Actions(c).Where(a => a.GetCustomAttribute<HttpPostAttribute>() != null))
                {
                    bool methodLevel = m.GetCustomAttribute<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>() != null;
                    Assert.True(classLevel || methodLevel, c.Name + "." + m.Name + " moves money and must be rate limited");
                }
            }
        }

        [Fact]
        public void Every_controller_is_an_ApiController()
        {
            foreach (Type c in Controllers(".Client").Concat(Controllers(".Admin")))
            {
                Assert.NotNull(c.GetCustomAttribute<ApiControllerAttribute>());
            }
        }

        [Fact]
        public void Controller_routes_are_unique_per_http_verb()
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (Type c in Controllers(".Client").Concat(Controllers(".Admin")))
            {
                string prefix = (c.GetCustomAttribute<RouteAttribute>()?.Template ?? "").Replace("[controller]", c.Name.Replace("Controller", "").ToLowerInvariant());
                foreach (MethodInfo m in Actions(c))
                {
                    foreach (HttpMethodAttribute h in m.GetCustomAttributes<HttpMethodAttribute>())
                    {
                        string key = h.HttpMethods.First() + " " + prefix + "/" + (h.Template ?? "");
                        Assert.True(seen.Add(key), "Duplicate route " + key);
                    }
                }
            }
        }
    }
}
