using CaspianBank.Tests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Analytics;
using Service.Helpers.DTOs.Bank;
using Service.Helpers.DTOs.HomeStats;
using Service.Helpers.DTOs.Transfers;
using Service.Helpers.DTOs.Translations;
using Service.Services.Interfaces;
using System.Globalization;
using Xunit;

namespace CaspianBank.Tests.Services
{
    // ---------- Tərcümələr ----------

    public class TranslationServiceTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public TranslationServiceTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        private async Task<T> WithAsync<T>(Func<ITranslationService, Task<T>> action)
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<ITranslationService>());
        }

        private static string Key(string name)
        {
            return name + " " + TestData.Next();
        }

        [Fact]
        public async Task The_languages_are_english_azerbaijani_and_russian()
        {
            using var scope = _app.NewScope();
            var languages = scope.ServiceProvider.GetRequiredService<ITranslationService>().GetLanguages().ToList();

            Assert.Equal(new[] { "en", "az", "ru" }, languages.Select(l => l.Code).ToArray());
            Assert.Equal("Azərbaycanca", languages[1].Name);
        }

        [Fact]
        public async Task Saving_stores_both_languages_and_trims_the_key()
        {
            var key = Key("Transfer money");

            var saved = await WithAsync(s => s.SaveAsync(new SaveTranslationDto { Key = "  " + key + "  ", Az = "  Pul köçür  ", Ru = " Перевести деньги " }));

            Assert.True(saved.IsSuccess);
            Assert.Equal(key, saved.Data!.Key);
            var az = await WithAsync(s => s.GetDictionaryAsync("az"));
            var ru = await WithAsync(s => s.GetDictionaryAsync("ru"));
            Assert.Equal("Pul köçür", az.Data![key]);
            Assert.Equal("Перевести деньги", ru.Data![key]);
        }

        [Fact]
        public async Task Saving_again_updates_and_an_empty_value_removes_that_language()
        {
            var key = Key("Balance");
            await WithAsync(s => s.SaveAsync(new SaveTranslationDto { Key = key, Az = "Balans", Ru = "Баланс" }));

            await WithAsync(s => s.SaveAsync(new SaveTranslationDto { Key = key, Az = "Qalıq", Ru = "" }));

            var az = await WithAsync(s => s.GetDictionaryAsync("az"));
            var ru = await WithAsync(s => s.GetDictionaryAsync("ru"));
            Assert.Equal("Qalıq", az.Data![key]);
            Assert.False(ru.Data!.ContainsKey(key));
        }

        [Fact]
        public async Task Saving_validates_the_key_and_the_value_lengths()
        {
            var empty = await WithAsync(s => s.SaveAsync(new SaveTranslationDto { Key = "   ", Az = "x" }));
            var longKey = await WithAsync(s => s.SaveAsync(new SaveTranslationDto { Key = new string('k', 401), Az = "x" }));
            var longValue = await WithAsync(s => s.SaveAsync(new SaveTranslationDto { Key = Key("Long"), Az = new string('v', 2001) }));
            var edge = await WithAsync(s => s.SaveAsync(new SaveTranslationDto { Key = new string('e', 399) + TestData.Next() % 10, Az = new string('v', 2000) }));

            Assert.Equal("The text to translate must be 1 to 400 characters.", empty.Errors[0]);
            Assert.Equal("The text to translate must be 1 to 400 characters.", longKey.Errors[0]);
            Assert.Equal("A translation can be at most 2000 characters.", longValue.Errors[0]);
            Assert.True(edge.IsSuccess, string.Join(";", edge.Errors));
        }

        [Fact]
        public async Task The_dictionary_is_empty_for_english_and_not_found_for_unknown_languages()
        {
            var en = await WithAsync(s => s.GetDictionaryAsync("EN"));
            var unknown = await WithAsync(s => s.GetDictionaryAsync("fr"));
            var padded = await WithAsync(s => s.GetDictionaryAsync(" AZ "));

            Assert.True(en.IsSuccess);
            Assert.Empty(en.Data!);
            Assert.True(unknown.IsNotFound);
            Assert.True(padded.IsSuccess);
        }

        [Fact]
        public async Task The_admin_page_searches_and_can_show_only_missing_translations()
        {
            var both = Key("Both languages");
            var missing = Key("Only azerbaijani");
            await WithAsync(s => s.SaveAsync(new SaveTranslationDto { Key = both, Az = "az", Ru = "ru" }));
            await WithAsync(s => s.SaveAsync(new SaveTranslationDto { Key = missing, Az = "yalnız az" }));

            var onlyMissing = await WithAsync(s => s.GetPageAsync(new TranslationFilterDto { Search = "Only azerbaijani", OnlyMissing = true }));
            var complete = await WithAsync(s => s.GetPageAsync(new TranslationFilterDto { Search = "Both languages", OnlyMissing = true }));
            var found = await WithAsync(s => s.GetPageAsync(new TranslationFilterDto { Search = both }));
            var byValue = await WithAsync(s => s.GetPageAsync(new TranslationFilterDto { Search = "yalnız az" }));

            Assert.Equal(missing, Assert.Single(onlyMissing.Data!.Items).Key);
            Assert.Equal("yalnız az", onlyMissing.Data.Items[0].Az);
            Assert.Equal(string.Empty, onlyMissing.Data.Items[0].Ru);
            Assert.Empty(complete.Data!.Items);
            var row = Assert.Single(found.Data!.Items);
            Assert.Equal("az", row.Az);
            Assert.Equal("ru", row.Ru);
            Assert.Equal(missing, Assert.Single(byValue.Data!.Items).Key);
        }

        [Fact]
        public async Task The_admin_page_is_ordered_and_clamps_the_paging()
        {
            await WithAsync(s => s.SaveAsync(new SaveTranslationDto { Key = "ZZ page key " + TestData.Next(), Az = "a" }));
            await WithAsync(s => s.SaveAsync(new SaveTranslationDto { Key = "AA page key " + TestData.Next(), Az = "a" }));

            var page = await WithAsync(s => s.GetPageAsync(new TranslationFilterDto { Search = "page key", PageSize = 1, Page = 1 }));
            var second = await WithAsync(s => s.GetPageAsync(new TranslationFilterDto { Search = "page key", PageSize = 1, Page = 2 }));
            var clamped = await WithAsync(s => s.GetPageAsync(new TranslationFilterDto { Page = -1, PageSize = 100000 }));
            var zero = await WithAsync(s => s.GetPageAsync(new TranslationFilterDto { PageSize = 0 }));

            Assert.StartsWith("AA", page.Data!.Items[0].Key);
            Assert.StartsWith("ZZ", second.Data!.Items[0].Key);
            Assert.Equal(2, page.Data.TotalPages);
            Assert.Equal(1, clamped.Data!.Page);
            Assert.Equal(100, clamped.Data.PageSize);
            Assert.Equal(30, zero.Data!.PageSize);
        }

        [Fact]
        public async Task A_key_that_differs_only_by_case_is_the_same_translation()
        {
            var key = Key("Case Matters");
            await WithAsync(s => s.SaveAsync(new SaveTranslationDto { Key = key, Az = "birinci" }));

            await WithAsync(s => s.SaveAsync(new SaveTranslationDto { Key = key.ToLowerInvariant(), Az = "ikinci" }));

            var az = (await WithAsync(s => s.GetDictionaryAsync("az"))).Data!;
            Assert.Equal(1, az.Keys.Count(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase)));
        }
    }

    // ---------- Bankın qaydaları ----------

    public class BankRulesServiceTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public BankRulesServiceTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        private async Task<T> WithAsync<T>(Func<IBankRulesService, Task<T>> action)
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<IBankRulesService>());
        }

        private async Task EnsureBankAsync()
        {
            await _app.QueryAsync(async db =>
            {
                await TestData.EnsureOwnBankAsync(db);
                return 0;
            });
        }

        private static BankRulesDto Rules()
        {
            return new BankRulesDto
            {
                ReservePercent = 10m, ProfitTaxPercent = 20m, OperatingCostMonthly = 150m, MaxInterbankAmount = 10000m,
                MinLoanAmount = 500m, MaxLoanAmount = 50000m, AnnualInterestRate = 10m, FixedPenalty = 10m, DailyPenaltyPercent = 0.5m,
                MaxActiveLoans = 3, OfferValidDays = 7
            };
        }

        [Fact]
        public async Task Without_a_bank_row_the_rules_are_not_available()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                using var scope = app.NewScope();
                var service = scope.ServiceProvider.GetRequiredService<IBankRulesService>();

                Assert.Equal("The bank rules are not set up yet.", (await service.GetAsync()).Errors[0]);
                Assert.Equal("The bank rules are not set up yet.", (await service.UpdateAsync(Rules())).Errors[0]);
            }
        }

        [Fact]
        public async Task The_rules_are_read_from_the_bank_row()
        {
            await EnsureBankAsync();

            var result = await WithAsync(s => s.GetAsync());

            Assert.True(result.IsSuccess);
            Assert.Equal(10m, result.Data!.ReservePercent);
            Assert.Equal(20m, result.Data.ProfitTaxPercent);
            Assert.Equal(3, result.Data.MaxActiveLoans);
        }

        [Fact]
        public async Task An_update_saves_every_field_and_lists_what_changed()
        {
            await EnsureBankAsync();
            var model = Rules();
            model.ReservePercent = 12.5m;
            model.MaxLoanAmount = 80000m;
            model.MaxActiveLoans = 4;

            var result = await WithAsync(s => s.UpdateAsync(model));

            Assert.True(result.IsSuccess, string.Join(";", result.Errors));
            Assert.Equal("Reserve % 10 → 12.5; Max loan 50000 → 80000; Open loans 3 → 4", result.Data.Changes);
            var bank = await _app.QueryAsync(db => db.OwnBanks.AsNoTracking().FirstAsync());
            Assert.Equal(12.5m, bank.ReservePercent);
            Assert.Equal(80000m, bank.MaxLoanAmount);
            Assert.Equal(4, bank.MaxActiveLoans);

            // geri qaytar
            var back = await WithAsync(s => s.UpdateAsync(Rules()));
            Assert.Equal("Reserve % 12.5 → 10; Max loan 80000 → 50000; Open loans 4 → 3", back.Data.Changes);
        }

        [Fact]
        public async Task Saving_the_same_values_reports_no_change()
        {
            await EnsureBankAsync();
            await WithAsync(s => s.UpdateAsync(Rules()));

            var result = await WithAsync(s => s.UpdateAsync(Rules()));

            Assert.Equal("No change", result.Data.Changes);
        }

        [Fact]
        public async Task Invalid_rules_are_refused_and_nothing_is_saved()
        {
            await EnsureBankAsync();
            await WithAsync(s => s.UpdateAsync(Rules()));
            var model = Rules();
            model.ReservePercent = 150m;
            model.MaxLoanAmount = 100m;

            var result = await WithAsync(s => s.UpdateAsync(model));

            Assert.False(result.IsSuccess);
            Assert.Contains("The reserve must be between 0% and 100%.", result.Errors);
            Assert.Contains("The largest loan can't be smaller than the smallest.", result.Errors);
            var bank = await _app.QueryAsync(db => db.OwnBanks.AsNoTracking().FirstAsync());
            Assert.Equal(10m, bank.ReservePercent);
        }
    }

    // ---------- Bank tanıma ----------

    public class BankDirectoryServiceTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public BankDirectoryServiceTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        private async Task<T> WithAsync<T>(Func<IBankDirectoryService, Task<T>> action)
        {
            using var scope = _app.NewScope();
            return await action(scope.ServiceProvider.GetRequiredService<IBankDirectoryService>());
        }

        private async Task SeedAsync()
        {
            await _app.QueryAsync(async db =>
            {
                await TestData.EnsureOwnBankAsync(db);
                if (!await db.ExternalBanks.AnyAsync())
                {
                    db.ExternalBanks.Add(new ExternalBank { Name = "International Bank of Azerbaijan (ABB)", Bin = "55220993" });
                    db.ExternalBanks.Add(new ExternalBank { Name = "Kapital Bank", Bin = "41690000" });
                    await db.SaveChangesAsync();
                }
                return 0;
            });
        }

        [Fact]
        public async Task The_first_six_digits_identify_an_external_bank_and_the_name_is_short()
        {
            await SeedAsync();

            var abb = await WithAsync(s => s.LookupAsync("552209"));
            var long8 = await WithAsync(s => s.LookupAsync("5522099300001111"));
            var spaced = await WithAsync(s => s.LookupAsync("5522 0999"));
            var kapital = await WithAsync(s => s.LookupAsync("416900"));

            Assert.Equal("ABB", abb.Data!.Name);
            Assert.False(abb.Data.IsCaspian);
            Assert.Equal("ABB", long8.Data!.Name);
            Assert.Equal("ABB", spaced.Data!.Name);
            Assert.Equal("Kapital Bank", kapital.Data!.Name);
        }

        [Fact]
        public async Task The_caspian_bin_is_recognised_as_caspian()
        {
            await SeedAsync();

            var result = await WithAsync(s => s.LookupAsync("963010"));

            Assert.True(result.IsSuccess);
            Assert.True(result.Data!.IsCaspian);
            Assert.Equal("Caspian Bank", result.Data.Name);
        }

        [Fact]
        public async Task A_card_start_of_an_existing_caspian_card_is_caspian_too()
        {
            await SeedAsync();
            var kit = await _app.NewKitAsync();

            var result = await WithAsync(s => s.LookupAsync(kit.Standard.CardNumber.Substring(0, 7)));

            Assert.True(result.IsSuccess);
            Assert.True(result.Data!.IsCaspian);
        }

        [Fact]
        public async Task Fewer_than_six_digits_is_refused_and_an_unknown_bin_is_not_found()
        {
            await SeedAsync();

            var tooShort = await WithAsync(s => s.LookupAsync("55220"));
            var empty = await WithAsync(s => s.LookupAsync(null));
            var letters = await WithAsync(s => s.LookupAsync("abcdef"));
            var unknown = await WithAsync(s => s.LookupAsync("999999"));

            Assert.Equal("Enter at least the first 6 digits of the card number.", tooShort.Errors[0]);
            Assert.False(empty.IsSuccess);
            Assert.False(letters.IsSuccess);
            Assert.True(unknown.IsNotFound);
        }
    }

    // ---------- Home statistikası ----------

    public class HomeStatsServiceTests : IClassFixture<IsolatedApp>
    {
        private readonly TestApp _app;

        public HomeStatsServiceTests(IsolatedApp fixture)
        {
            _app = fixture.App;
        }

        [Fact]
        public async Task The_numbers_are_the_base_values_plus_the_real_counts()
        {
            using (var empty = _app.NewScope())
            {
                var baseline = await empty.ServiceProvider.GetRequiredService<IHomeStatsService>().GetUIAsync();
                Assert.Equal(900, baseline.CustomerCount);
                Assert.Equal(4200, baseline.TransferCount);
                Assert.Equal(1850000m, baseline.TransferVolume);
                Assert.Equal(1750, baseline.CardCount);
            }

            var sender = await _app.NewKitAsync(1000m);
            var recipient = await _app.NewKitAsync();
            await _app.NewStaffAsync();
            using (var scope = _app.NewScope())
            {
                var result = await scope.ServiceProvider.GetRequiredService<ITransferService>().TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = 125m, RequestId = Guid.NewGuid().ToString() });
                Assert.True(result.IsSuccess);
            }

            using var after = _app.NewScope();
            var stats = await after.ServiceProvider.GetRequiredService<IHomeStatsService>().GetUIAsync();

            Assert.Equal(902, stats.CustomerCount);            // işçi sayılmır
            Assert.Equal(4201, stats.TransferCount);
            Assert.Equal(1850125m, stats.TransferVolume);
            Assert.Equal(1754, stats.CardCount);
        }
    }

    // ---------- Xərc analitikası ----------

    public class AnalyticsServiceTests
    {
        private static async Task AddRowAsync(TestApp app, int cardId, TransactionType type, decimal amount, DateTime createdUtc, string description = "row", bool income = false)
        {
            await app.QueryAsync(async db =>
            {
                db.Transactions.Add(new Transaction { CardId = cardId, Type = type, IsIncome = income, Amount = amount, BalanceAfter = 0m, Description = description, Reference = "AN-" + TestData.Next(), CreatedAt = createdUtc });
                await db.SaveChangesAsync();
                return 0;
            });
        }

        private static DateTime InMonth(int monthsAgo, int day = 10)
        {
            var today = LoanEngine.BakuToday(DateTime.UtcNow);
            var date = new DateTime(today.Year, today.Month, 1).AddMonths(-monthsAgo).AddDays(day - 1);
            return DateTime.SpecifyKind(date.AddHours(12) - TimeSpan.FromHours(4), DateTimeKind.Utc);
        }

        private static async Task<SpendingAnalyticsDto> GetAsync(TestApp app, string userId, int months)
        {
            using var scope = app.NewScope();
            return await scope.ServiceProvider.GetRequiredService<IAnalyticsService>().GetSpendingAsync(userId, months);
        }

        [Fact]
        public async Task Spending_is_split_into_categories_per_month()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            await AddRowAsync(app, kit.Standard.Id, TransactionType.TransferOut, 100m, InMonth(0));
            await AddRowAsync(app, kit.Standard.Id, TransactionType.BillPayment, 40m, InMonth(0));
            await AddRowAsync(app, kit.Standard.Id, TransactionType.LoanPayment, 300m, InMonth(0));
            await AddRowAsync(app, kit.Standard.Id, TransactionType.Commission, 1m, InMonth(0));
            await AddRowAsync(app, kit.Standard.Id, TransactionType.CardFee, 10m, InMonth(0));
            await AddRowAsync(app, kit.Standard.Id, TransactionType.DepositOpen, 500m, InMonth(0));

            var result = await GetAsync(app, kit.User.Id, 3);

            Assert.Equal(3, result.Months.Count);
            var current = result.Months.Last();
            Assert.Equal(100m, current.Categories.Single(c => c.Code == "Transfers").Amount);
            Assert.Equal(40m, current.Categories.Single(c => c.Code == "Bills").Amount);
            Assert.Equal(300m, current.Categories.Single(c => c.Code == "Loans").Amount);
            Assert.Equal(11m, current.Categories.Single(c => c.Code == "Fees").Amount);
            Assert.Equal(500m, current.Categories.Single(c => c.Code == "Deposits").Amount);
            Assert.Equal(951m, current.Total);
            Assert.Equal(951m, result.CurrentTotal);
            Assert.Equal("Put into deposits", result.TopCategory);
            Assert.Equal(new[] { "Transfers", "Bills", "Loans", "Fees", "Deposits" }, current.Categories.Select(c => c.Code).ToArray());
        }

        [Fact]
        public async Task Transfers_between_own_cards_and_incoming_money_are_not_spending()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            await AddRowAsync(app, kit.Standard.Id, TransactionType.TransferOut, 999m, InMonth(0), "Transfer to your Silver card •••• 1111");
            await AddRowAsync(app, kit.Standard.Id, TransactionType.TopUp, 500m, InMonth(0), "Top up", income: true);
            await AddRowAsync(app, kit.Standard.Id, TransactionType.TransferIn, 50m, InMonth(0), "Transfer from X", income: true);
            await AddRowAsync(app, kit.Standard.Id, TransactionType.TransferOut, 25m, InMonth(0), "Transfer to •••• 2222");

            var result = await GetAsync(app, kit.User.Id, 1);

            Assert.Equal(25m, result.CurrentTotal);
        }

        [Fact]
        public async Task Months_are_baku_months_and_old_rows_fall_outside_the_range()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var today = LoanEngine.BakuToday(DateTime.UtcNow);
            var monthStartBaku = new DateTime(today.Year, today.Month, 1);
            // Bu ayın 1-i 00:30 Bakı = əvvəlki günün 20:30 UTC: bu aya aiddir
            await AddRowAsync(app, kit.Standard.Id, TransactionType.BillPayment, 7m, DateTime.SpecifyKind(monthStartBaku.AddMinutes(30) - TimeSpan.FromHours(4), DateTimeKind.Utc));
            // Əvvəlki ayın sonu 23:30 Bakı: əvvəlki aya aiddir
            await AddRowAsync(app, kit.Standard.Id, TransactionType.BillPayment, 3m, DateTime.SpecifyKind(monthStartBaku.AddMinutes(-30) - TimeSpan.FromHours(4), DateTimeKind.Utc));
            await AddRowAsync(app, kit.Standard.Id, TransactionType.BillPayment, 1000m, InMonth(5));

            var result = await GetAsync(app, kit.User.Id, 2);

            Assert.Equal(7m, result.CurrentTotal);
            Assert.Equal(3m, result.PreviousTotal);
            Assert.Equal(2, result.Months.Count);
        }

        [Fact]
        public async Task The_change_percent_and_the_average_are_calculated()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            await AddRowAsync(app, kit.Standard.Id, TransactionType.BillPayment, 100m, InMonth(1));
            await AddRowAsync(app, kit.Standard.Id, TransactionType.BillPayment, 150m, InMonth(0));

            var result = await GetAsync(app, kit.User.Id, 2);

            Assert.Equal(50m, result.ChangePercent);
            Assert.Equal(125m, result.AverageMonthly);
        }

        [Fact]
        public async Task Without_a_previous_base_the_change_is_empty_and_without_spending_there_is_no_top_category()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            var empty = await GetAsync(app, kit.User.Id, 3);
            Assert.Null(empty.ChangePercent);
            Assert.Null(empty.TopCategory);
            Assert.Equal(0m, empty.AverageMonthly);

            await AddRowAsync(app, kit.Standard.Id, TransactionType.BillPayment, 20m, InMonth(0));
            var single = await GetAsync(app, kit.User.Id, 1);
            Assert.Null(single.ChangePercent);
            Assert.Equal(0m, single.PreviousTotal);
            Assert.Equal("Bill payments", single.TopCategory);
        }

        [Fact]
        public async Task The_months_count_is_clamped_between_one_and_twelve_and_only_my_money_counts()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var other = await app.NewKitAsync();
            await AddRowAsync(app, other.Standard.Id, TransactionType.BillPayment, 500m, InMonth(0));

            Assert.Single((await GetAsync(app, kit.User.Id, 0)).Months);
            Assert.Single((await GetAsync(app, kit.User.Id, -5)).Months);
            Assert.Equal(12, (await GetAsync(app, kit.User.Id, 100)).Months.Count);
            Assert.Equal(0m, (await GetAsync(app, kit.User.Id, 1)).CurrentTotal);
        }

        [Fact]
        public async Task Month_labels_and_keys_are_formatted()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var today = LoanEngine.BakuToday(DateTime.UtcNow);

            var result = await GetAsync(app, kit.User.Id, 2);

            Assert.Equal(today.ToString("yyyy-MM", CultureInfo.InvariantCulture), result.Months.Last().Key);
            Assert.Equal(today.ToString("MMM yyyy", CultureInfo.InvariantCulture), result.Months.Last().Label);
            Assert.Equal(new DateTime(today.Year, today.Month, 1).AddMonths(-1).ToString("yyyy-MM", CultureInfo.InvariantCulture), result.Months[0].Key);
        }
    }
}
