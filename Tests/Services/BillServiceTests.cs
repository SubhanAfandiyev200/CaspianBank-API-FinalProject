using CaspianBank.Tests.Infrastructure;
using Domain.Bills;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Bills;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class BillServiceTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public BillServiceTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        private static string NewRequest()
        {
            return Guid.NewGuid().ToString();
        }

        private async Task<T> WithAsync<T>(Func<IBillService, Task<T>> action)
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<IBillService>());
        }

        private async Task<BillProvider> ProviderAsync(BillCategory category, string? prefixes = null, int idLength = 10, decimal unitPrice = 0m,
                                                       decimal min = 1m, decimal max = 200m, bool active = true, string? name = null, int sort = 0)
        {
            return await _app.QueryAsync(async db =>
            {
                var n = TestData.Next();
                var provider = new BillProvider
                {
                    Code = "p" + n,
                    Name = name ?? "Provider " + n,
                    Category = category,
                    SortOrder = sort,
                    IsActive = active,
                    Prefixes = prefixes,
                    IdentifierLength = idLength,
                    IdentifierLabel = category == BillCategory.Mobile ? "Phone number" : "Subscriber code",
                    UnitName = unitPrice > 0m ? "kWh" : null,
                    UnitPrice = unitPrice,
                    MinPayment = min,
                    MaxPayment = max
                };
                db.BillProviders.Add(provider);
                await db.SaveChangesAsync();
                return provider;
            });
        }

        private async Task<BillSubscriber> SubscriberAsync(BillProvider provider, string identifier, decimal balance = 0m, decimal? fee = null, decimal? usage = null, bool active = true, string fullName = "Elçin Hüseynov")
        {
            return await _app.QueryAsync(async db =>
            {
                var subscriber = new BillSubscriber
                {
                    ProviderId = provider.Id,
                    Identifier = identifier,
                    FullName = fullName,
                    Balance = balance,
                    MonthlyFee = fee,
                    PackageName = fee.HasValue ? "Home " + fee : null,
                    AverageUsage = usage,
                    IsActive = active
                };
                db.BillSubscribers.Add(subscriber);
                await db.SaveChangesAsync();
                return subscriber;
            });
        }

        // Təkrarsız mobil nömrə: 0 + prefiks + 7 rəqəm
        private static string Phone(string prefix)
        {
            return "0" + prefix + (1000000 + TestData.Next() % 9000000).ToString("D7");
        }

        private static string Code(int length)
        {
            return (TestData.Next() * 7919L % (long)Math.Pow(10, length)).ToString("D" + length);
        }

        private static PayBillDto Pay(BillProvider provider, string identifier, decimal amount, int cardId, string? request = null)
        {
            return new PayBillDto { ProviderId = provider.Id, Identifier = identifier, Amount = amount, CardId = cardId, RequestId = request ?? NewRequest() };
        }

        // ---------- Təchizatçılar ----------

        [Fact]
        public async Task Providers_list_shows_only_active_ones_in_sort_order_with_hints()
        {
            var second = await ProviderAsync(BillCategory.Mobile, "50,51", sort: 9002, name: "Second Mobile");
            var first = await ProviderAsync(BillCategory.Electricity, idLength: 10, unitPrice: 0.1m, sort: 9001, name: "First Power");
            var off = await ProviderAsync(BillCategory.Water, sort: 9003, active: false, name: "Off Water");

            var providers = (await WithAsync(s => s.GetProvidersAsync())).ToList();

            Assert.DoesNotContain(providers, p => p.Id == off.Id);
            var indexFirst = providers.FindIndex(p => p.Id == first.Id);
            var indexSecond = providers.FindIndex(p => p.Id == second.Id);
            Assert.True(indexFirst >= 0 && indexFirst < indexSecond);
            var mobile = providers[indexSecond];
            Assert.Equal("055 123 45 67", mobile.IdentifierHint);
            Assert.Equal("050 or 051", mobile.PrefixesText);
            var power = providers[indexFirst];
            Assert.Equal("10 digits", power.IdentifierHint);
            Assert.Null(power.PrefixesText);
            Assert.Equal("kWh", power.UnitName);
        }

        // ---------- Axtarış ----------

        [Fact]
        public async Task Lookup_finds_a_mobile_number_in_any_written_form_and_masks_the_owner()
        {
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77");
            var phone = Phone("77");
            await SubscriberAsync(provider, phone, balance: -12.5m, fullName: "Elçin Hüseynov");
            var written = $"+994 {phone.Substring(1, 2)} {phone.Substring(3, 3)} {phone.Substring(6, 2)} {phone.Substring(8, 2)}";

            var result = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = provider.Id, Identifier = written }));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Equal(provider.Name, result.Data!.ProviderName);
            Assert.Equal($"{phone[..3]} {phone.Substring(3, 3)} {phone.Substring(6, 2)} {phone.Substring(8, 2)}", result.Data.Identifier);
            Assert.Equal("Elçin H.", result.Data.SubscriberName);
            Assert.Equal(12.5m, result.Data.Debt);
            Assert.Equal(12.5m, result.Data.SuggestedAmount);
            Assert.Equal(0m, result.Data.Credit);
        }

        [Fact]
        public async Task Lookup_shows_prepaid_credit_instead_of_debt()
        {
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77");
            var phone = Phone("70");
            await SubscriberAsync(provider, phone, balance: 8m);

            var result = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = provider.Id, Identifier = phone }));

            Assert.Equal(8m, result.Data!.Credit);
            Assert.Equal(0m, result.Data.Debt);
        }

        [Fact]
        public async Task Lookup_reports_a_bad_phone_a_missing_number_and_the_wrong_operator()
        {
            var nar = await ProviderAsync(BillCategory.Mobile, "70,77");
            var other = await ProviderAsync(BillCategory.Mobile, "55,99", name: "Other Operator");
            var phone = Phone("55");
            await SubscriberAsync(other, phone);

            var bad = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = nar.Id, Identifier = "123" }));
            var missing = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = nar.Id, Identifier = Phone("77") }));
            var wrongOperator = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = nar.Id, Identifier = phone }));

            Assert.Equal("Enter the phone number like 055 123 45 67.", bad.Errors[0]);
            Assert.Equal("This number doesn't exist.", missing.Errors[0]);
            Assert.Equal($"The operator is not correct. This number belongs to Other Operator, not {nar.Name}.", wrongOperator.Errors[0]);
        }

        [Fact]
        public async Task Lookup_of_a_prefix_nobody_serves_lists_the_valid_prefixes()
        {
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77");

            var result = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = provider.Id, Identifier = "0421234567" }));

            Assert.Equal($"The operator is not correct. {provider.Name} numbers start with 070 or 077.", result.Errors[0]);
        }

        [Fact]
        public async Task Lookup_of_a_utility_code_needs_the_right_number_of_digits()
        {
            var provider = await ProviderAsync(BillCategory.Electricity, idLength: 10, unitPrice: 0.1m);
            var code = Code(10);
            await SubscriberAsync(provider, code, usage: 100m);

            var ok = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = provider.Id, Identifier = code }));
            var tooShort = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = provider.Id, Identifier = "12345" }));
            var letters = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = provider.Id, Identifier = code[..9] + "x" }));
            var unknown = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = provider.Id, Identifier = Code(10) }));

            Assert.True(ok.IsSuccess);
            Assert.Equal(code, ok.Data!.Identifier);
            Assert.Equal("Elçin Hüseynov", ok.Data.SubscriberName);        // kommunalda tam ad göstərilir
            Assert.Equal("Enter the 10-digit subscriber code.", tooShort.Errors[0]);
            Assert.Equal("Enter the 10-digit subscriber code.", letters.Errors[0]);
            Assert.Equal("This subscriber code doesn't exist.", unknown.Errors[0]);
        }

        [Fact]
        public async Task Lookup_ignores_inactive_providers_and_inactive_subscribers()
        {
            var off = await ProviderAsync(BillCategory.Internet, idLength: 8, active: false);
            var on = await ProviderAsync(BillCategory.Internet, idLength: 8);
            var code = Code(8);
            await SubscriberAsync(on, code, fee: 30m, active: false);

            var inactiveProvider = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = off.Id, Identifier = Code(8) }));
            var inactiveSubscriber = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = on.Id, Identifier = code }));

            Assert.True(inactiveProvider.IsNotFound);
            Assert.False(inactiveSubscriber.IsSuccess);
        }

        [Fact]
        public async Task Lookup_shows_the_package_and_the_last_charge()
        {
            var provider = await ProviderAsync(BillCategory.Internet, idLength: 8);
            var code = Code(8);
            var subscriber = await SubscriberAsync(provider, code, fee: 30m);
            await _app.QueryAsync(async db =>
            {
                db.BillCharges.Add(new BillCharge { SubscriberId = subscriber.Id, Period = "2026-08", Amount = 30m, Description = "Home 30 · 2026-08" });
                db.BillCharges.Add(new BillCharge { SubscriberId = subscriber.Id, Period = "2026-09", Amount = 30m, Description = "Home 30 · 2026-09" });
                await db.SaveChangesAsync();
                return 0;
            });

            var result = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = provider.Id, Identifier = code }));

            Assert.Equal(30m, result.Data!.MonthlyFee);
            Assert.Equal("Home 30", result.Data.PackageName);
            Assert.Equal("2026-09", result.Data.LastChargePeriod);
            Assert.Equal(30m, result.Data.LastChargeAmount);
        }

        // ---------- Ödəniş ----------

        [Fact]
        public async Task Paying_debits_the_card_clears_the_debt_and_pays_cashback()
        {
            var kit = await _app.NewKitAsync(500m);
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77");
            var phone = Phone("77");
            var subscriber = await SubscriberAsync(provider, phone, balance: -10m);

            var result = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 100m, kit.Standard.Id)));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            var receipt = result.Data!;
            Assert.Equal(100m, receipt.Amount);
            Assert.Equal(10m, receipt.DebtCleared);
            Assert.Equal(90m, receipt.CreditAdded);
            Assert.Equal(0m, receipt.RemainingDebt);
            Assert.Equal(90m, receipt.BalanceAfter);
            Assert.Equal(0.5m, receipt.CashbackEarned);                 // Standard: 0.5%
            Assert.Equal(400m, receipt.CardBalanceAfter);
            Assert.StartsWith("BP-", receipt.Reference);
            Assert.Equal(400m, await app_Balance(kit.Standard.Id));
            Assert.Equal(0.5m, await app_Balance(kit.Cashback.Id));
            Assert.Equal(90m, await _app.QueryAsync(db => db.BillSubscribers.AsNoTracking().Where(x => x.Id == subscriber.Id).Select(x => x.Balance).FirstAsync()));
        }

        private Task<decimal> app_Balance(int cardId)
        {
            return _app.BalanceAsync(cardId);
        }

        [Fact]
        public async Task Paying_writes_the_journal_the_cashback_row_the_payment_record_and_the_treasury_entry()
        {
            var kit = await _app.NewKitAsync(500m);
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77", name: "Nar Test");
            var phone = Phone("77");
            await SubscriberAsync(provider, phone);

            var result = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 100m, kit.Standard.Id)));

            var row = (await _app.TransactionsAsync(kit.Standard.Id)).Single();
            Assert.Equal(TransactionType.BillPayment, row.Type);
            Assert.Equal(100m, row.Amount);
            Assert.Equal($"Nar Test · {phone[..3]} {phone.Substring(3, 3)} {phone.Substring(6, 2)} {phone.Substring(8, 2)}", row.Description);

            var cashback = (await _app.TransactionsAsync(kit.Cashback.Id)).Single();
            Assert.Equal(TransactionType.Cashback, cashback.Type);
            Assert.Equal(0.5m, cashback.Amount);
            Assert.Equal("CB-" + result.Data!.Reference.Substring(3), cashback.Reference);
            Assert.Equal("Cashback · Nar Test", cashback.Description);

            var payment = await _app.QueryAsync(db => db.BillPayments.AsNoTracking().SingleAsync(p => p.Reference == result.Data.Reference));
            Assert.Equal(100m, payment.Amount);
            Assert.Equal(0.5m, payment.CashbackEarned);
            Assert.Equal(kit.User.Id, payment.UserId);

            var entries = await _app.QueryAsync(db => db.TreasuryEntries.AsNoTracking().Where(t => t.Reference == cashback.Reference).ToListAsync());
            Assert.Equal(-0.5m, Assert.Single(entries).Amount);
        }

        [Fact]
        public async Task Paying_with_the_cashback_card_earns_no_cashback()
        {
            var kit = await _app.NewKitAsync();
            await _app.SetBalanceAsync(kit.Cashback.Id, 50m);
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77");
            var phone = Phone("70");
            await SubscriberAsync(provider, phone);

            var result = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 20m, kit.Cashback.Id)));

            Assert.True(result.IsSuccess);
            Assert.Equal(0m, result.Data!.CashbackEarned);
            Assert.Equal(30m, await app_Balance(kit.Cashback.Id));
        }

        [Fact]
        public async Task A_blocked_cashback_card_gets_no_cashback_but_the_payment_works()
        {
            var kit = await _app.NewKitAsync(100m);
            await _app.QueryAsync(async db =>
            {
                await db.Cards.Where(c => c.Id == kit.Cashback.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsBlocked, true));
                return 0;
            });
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77");
            var phone = Phone("70");
            await SubscriberAsync(provider, phone);

            var result = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 50m, kit.Standard.Id)));

            Assert.True(result.IsSuccess);
            Assert.Equal(0m, result.Data!.CashbackEarned);
            Assert.Equal(0m, await app_Balance(kit.Cashback.Id));
        }

        [Fact]
        public async Task Paying_less_than_the_debt_leaves_the_rest_as_debt()
        {
            var kit = await _app.NewKitAsync(500m);
            var provider = await ProviderAsync(BillCategory.Internet, idLength: 8);
            var code = Code(8);
            await SubscriberAsync(provider, code, balance: -50m, fee: 30m);

            var result = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, code, 20m, kit.Standard.Id)));

            Assert.Equal(20m, result.Data!.DebtCleared);
            Assert.Equal(0m, result.Data.CreditAdded);
            Assert.Equal(30m, result.Data.RemainingDebt);
        }

        [Fact]
        public async Task The_amount_must_be_within_the_provider_limits()
        {
            var kit = await _app.NewKitAsync(1000m);
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77", min: 1m, max: 200m);
            var phone = Phone("70");
            await SubscriberAsync(provider, phone);

            var low = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 0.5m, kit.Standard.Id)));
            var high = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 200.01m, kit.Standard.Id)));
            var edge = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 200m, kit.Standard.Id)));

            Assert.Equal("The amount must be between 1.00 and 200.00 AZN.", low.Errors[0]);
            Assert.Equal("The amount must be between 1.00 and 200.00 AZN.", high.Errors[0]);
            Assert.True(edge.IsSuccess);
        }

        [Fact]
        public async Task Payment_fails_for_low_balance_blocked_or_foreign_cards()
        {
            var kit = await _app.NewKitAsync(5m);
            var stranger = await _app.NewKitAsync(500m);
            var blocked = await _app.NewCardAsync(kit.User.Id, CardTier.Silver, 500m, blocked: true);
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77");
            var phone = Phone("70");
            await SubscriberAsync(provider, phone);

            var poor = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 10m, kit.Standard.Id)));
            var blockedResult = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 10m, blocked.Id)));
            var foreign = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 10m, stranger.Standard.Id)));

            Assert.Equal("That card doesn't have enough balance for this payment.", poor.Errors[0]);
            Assert.Equal("That card is blocked. Choose another card.", blockedResult.Errors[0]);
            Assert.Equal("Choose one of your own cards to pay from.", foreign.Errors[0]);
            Assert.Equal(5m, await app_Balance(kit.Standard.Id));
            Assert.Equal(500m, await app_Balance(stranger.Standard.Id));
        }

        [Fact]
        public async Task Payment_fails_for_an_inactive_provider_an_unknown_subscriber_and_a_restricted_account()
        {
            var kit = await _app.NewKitAsync(500m);
            var off = await ProviderAsync(BillCategory.Mobile, "70,77", active: false);
            var on = await ProviderAsync(BillCategory.Mobile, "70,77");

            var inactive = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(off, Phone("70"), 10m, kit.Standard.Id)));
            var unknownNumber = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(on, Phone("70"), 10m, kit.Standard.Id)));
            await _app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == kit.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true));
                return 0;
            });
            var phone = Phone("77");
            await SubscriberAsync(on, phone);
            var restricted = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(on, phone, 10m, kit.Standard.Id)));
            var ghost = await WithAsync(s => s.PayAsync("nobody", Pay(on, phone, 10m, kit.Standard.Id)));

            Assert.Equal("This provider isn't available right now.", inactive.Errors[0]);
            Assert.Equal("We couldn't find this number or code anymore. Check it and try again.", unknownNumber.Errors[0]);
            Assert.Equal("Your account is restricted. Please contact support.", restricted.Errors[0]);
            Assert.True(ghost.IsNotFound);
            Assert.Equal(500m, await app_Balance(kit.Standard.Id));
        }

        [Fact]
        public async Task The_same_request_id_is_paid_once_and_returns_the_same_receipt()
        {
            var kit = await _app.NewKitAsync(500m);
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77");
            var phone = Phone("70");
            await SubscriberAsync(provider, phone);
            var request = NewRequest();

            var first = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 40m, kit.Standard.Id, request)));
            var again = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 40m, kit.Standard.Id, request)));

            Assert.True(again.IsSuccess, string.Join(";", again.Errors));
            Assert.Equal(first.Data!.Reference, again.Data!.Reference);
            Assert.Equal(460m, await app_Balance(kit.Standard.Id));
        }

        [Fact]
        public async Task Parallel_payments_with_one_request_id_pay_once()
        {
            var kit = await _app.NewKitAsync(500m);
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77");
            var phone = Phone("70");
            await SubscriberAsync(provider, phone);
            var request = NewRequest();

            var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 40m, kit.Standard.Id, request)))));

            Assert.All(results, r => Assert.True(r.IsSuccess, string.Join(";", r.Errors)));
            Assert.Equal(460m, await app_Balance(kit.Standard.Id));
        }

        [Fact]
        public async Task Parallel_payments_never_overdraw_the_card()
        {
            var kit = await _app.NewKitAsync(100m);
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77");
            var phone = Phone("70");
            await SubscriberAsync(provider, phone);

            var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 40m, kit.Standard.Id)))));

            Assert.Equal(2, results.Count(r => r.IsSuccess));
            Assert.Equal(20m, await app_Balance(kit.Standard.Id));
        }

        [Fact]
        public async Task Utility_payment_normalizes_the_code()
        {
            var kit = await _app.NewKitAsync(500m);
            var provider = await ProviderAsync(BillCategory.Water, idLength: 10, unitPrice: 0.5m);
            var code = Code(10);
            await SubscriberAsync(provider, code, usage: 20m);

            var result = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, " " + code + " ", 25m, kit.Standard.Id)));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Equal(code, result.Data!.Identifier);
        }

        // ---------- Xülasə ----------

        [Fact]
        public async Task Overview_adds_up_this_months_payments_and_lists_the_latest_first()
        {
            var kit = await _app.NewKitAsync(1000m);
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77");
            var phone = Phone("70");
            await SubscriberAsync(provider, phone);
            for (var i = 1; i <= 3; i++)
            {
                await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 10m * i, kit.Standard.Id)));
            }

            var overview = await WithAsync(s => s.GetOverviewAsync(kit.User.Id));

            Assert.Equal(60m, overview.PaidThisMonth);
            Assert.Equal(3, overview.PaymentsThisMonth);
            Assert.Equal(0.3m, overview.CashbackThisMonth);                  // 0.05 + 0.10 + 0.15
            Assert.Equal(new[] { 30m, 20m, 10m }, overview.RecentPayments.Select(p => p.Amount).ToArray());
        }

        [Fact]
        public async Task Overview_shows_at_most_ten_recent_payments_and_nothing_for_other_users()
        {
            var kit = await _app.NewKitAsync(1000m);
            var other = await _app.NewKitAsync();
            var provider = await ProviderAsync(BillCategory.Mobile, "70,77");
            var phone = Phone("70");
            await SubscriberAsync(provider, phone);
            for (var i = 0; i < 12; i++)
            {
                await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, phone, 1m, kit.Standard.Id)));
            }

            var overview = await WithAsync(s => s.GetOverviewAsync(kit.User.Id));
            var empty = await WithAsync(s => s.GetOverviewAsync(other.User.Id));

            Assert.Equal(12, overview.PaymentsThisMonth);
            Assert.Equal(10, overview.RecentPayments.Count);
            Assert.Equal(0, empty.PaymentsThisMonth);
            Assert.Empty(empty.RecentPayments);
        }

        // ---------- Aylıq hesab ----------

        [Fact]
        public async Task Monthly_billing_charges_internet_subscribers_and_makes_the_balance_negative()
        {
            var provider = await ProviderAsync(BillCategory.Internet, idLength: 8);
            var code = Code(8);
            var subscriber = await SubscriberAsync(provider, code, fee: 30m, balance: 10m);

            await WithAsync(s => s.RunMonthlyBillingAsync());

            var period = BillingEngine.PeriodOf(Domain.Loans.LoanEngine.BakuToday(DateTime.UtcNow));
            var charge = await _app.QueryAsync(db => db.BillCharges.AsNoTracking().SingleAsync(c => c.SubscriberId == subscriber.Id));
            Assert.Equal(period, charge.Period);
            Assert.Equal(30m, charge.Amount);
            Assert.Equal($"Home 30 · {period}", charge.Description);
            Assert.Equal(-20m, await _app.QueryAsync(db => db.BillSubscribers.AsNoTracking().Where(x => x.Id == subscriber.Id).Select(x => x.Balance).FirstAsync()));
        }

        [Fact]
        public async Task Monthly_billing_charges_each_subscriber_once_per_month()
        {
            var provider = await ProviderAsync(BillCategory.Internet, idLength: 8);
            var subscriber = await SubscriberAsync(provider, Code(8), fee: 20m);

            await WithAsync(s => s.RunMonthlyBillingAsync());
            var second = await WithAsync(s => s.RunMonthlyBillingAsync());

            Assert.Equal(1, await _app.QueryAsync(db => db.BillCharges.CountAsync(c => c.SubscriberId == subscriber.Id)));
            Assert.Equal(0, second);
            Assert.Equal(-20m, await _app.QueryAsync(db => db.BillSubscribers.AsNoTracking().Where(x => x.Id == subscriber.Id).Select(x => x.Balance).FirstAsync()));
        }

        [Fact]
        public async Task Monthly_billing_skips_mobile_inactive_and_zero_charges_and_computes_utility_usage()
        {
            var mobile = await ProviderAsync(BillCategory.Mobile, "70,77");
            var mobileSubscriber = await SubscriberAsync(mobile, Phone("70"));
            var off = await ProviderAsync(BillCategory.Internet, idLength: 8, active: false);
            var offSubscriber = await SubscriberAsync(off, Code(8), fee: 30m);
            var internet = await ProviderAsync(BillCategory.Internet, idLength: 8);
            var inactiveSubscriber = await SubscriberAsync(internet, Code(8), fee: 30m, active: false);
            var zero = await SubscriberAsync(internet, Code(8), fee: null);
            var power = await ProviderAsync(BillCategory.Electricity, idLength: 10, unitPrice: 0.1m);
            var meter = await SubscriberAsync(power, Code(10), usage: 200m);

            await WithAsync(s => s.RunMonthlyBillingAsync());

            Assert.Equal(0, await _app.QueryAsync(db => db.BillCharges.CountAsync(c => c.SubscriberId == mobileSubscriber.Id)));
            Assert.Equal(0, await _app.QueryAsync(db => db.BillCharges.CountAsync(c => c.SubscriberId == offSubscriber.Id)));
            Assert.Equal(0, await _app.QueryAsync(db => db.BillCharges.CountAsync(c => c.SubscriberId == inactiveSubscriber.Id)));
            Assert.Equal(0, await _app.QueryAsync(db => db.BillCharges.CountAsync(c => c.SubscriberId == zero.Id)));

            var period = BillingEngine.PeriodOf(Domain.Loans.LoanEngine.BakuToday(DateTime.UtcNow));
            var expected = BillingEngine.ComputeCharge(power, meter, period);
            var charge = await _app.QueryAsync(db => db.BillCharges.AsNoTracking().SingleAsync(c => c.SubscriberId == meter.Id));
            Assert.Equal(expected.Amount, charge.Amount);
            Assert.Equal(expected.Usage, charge.Usage);
        }

        [Fact]
        public async Task A_charge_followed_by_a_payment_settles_the_debt()
        {
            var kit = await _app.NewKitAsync(500m);
            var provider = await ProviderAsync(BillCategory.Internet, idLength: 8);
            var code = Code(8);
            await SubscriberAsync(provider, code, fee: 30m);
            await WithAsync(s => s.RunMonthlyBillingAsync());

            var lookup = await WithAsync(s => s.LookupAsync(new LookupBillDto { ProviderId = provider.Id, Identifier = code }));
            var paid = await WithAsync(s => s.PayAsync(kit.User.Id, Pay(provider, code, lookup.Data!.SuggestedAmount, kit.Standard.Id)));

            Assert.Equal(30m, lookup.Data.Debt);
            Assert.Equal(0m, paid.Data!.RemainingDebt);
            Assert.Equal(30m, paid.Data.DebtCleared);
        }
    }
}
