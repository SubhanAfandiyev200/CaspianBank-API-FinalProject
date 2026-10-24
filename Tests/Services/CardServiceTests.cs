using CaspianBank.Tests.Infrastructure;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository.Data;
using Service.Helpers.DTOs.Cards;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class CardServiceTests
    {
        private static TopUpDto ValidTopUp(decimal amount)
        {
            return new TopUpDto { Amount = amount, SourceCardNumber = "4111 1111 1111 1111", Expiry = DateTime.UtcNow.AddYears(2).ToString("MM/yy"), Cvv = "123" };
        }

        // Kartsız, hesabı reyestrə bağlı yeni müştəri
        private static async Task<(AppUser User, Customer Customer)> NewUserWithoutCardsAsync(TestApp app)
        {
            using var scope = app.NewScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestData.EnsureOwnBankAsync(db);
            var customer = await TestData.NewCustomerAsync(db);
            var user = await TestData.NewUserAsync(scope.ServiceProvider, customer);
            return (user, customer);
        }

        // ---------- İlk kartlar ----------

        [Fact]
        public async Task CreateInitialCards_opens_a_standard_and_a_cashback_card_with_zero_balance()
        {
            var app = await TestApp.SharedAsync();
            var (user, _) = await NewUserWithoutCardsAsync(app);

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().CreateInitialCardsAsync(user.Id);

            Assert.True(result.IsSuccess);
            var cards = result.Data!.ToList();
            Assert.Equal(2, cards.Count);
            Assert.Contains(cards, c => c.Tier == "Standard");
            Assert.Contains(cards, c => c.Tier == "Cashback");
            Assert.All(cards, c => Assert.Equal(0m, c.Balance));
            Assert.All(cards, c => Assert.False(c.IsBlocked));
            Assert.All(cards, c => Assert.Equal(4, c.Last4.Length));
        }

        [Fact]
        public async Task Initial_cards_have_valid_numbers_expiry_four_years_ahead_and_the_holder_name()
        {
            var app = await TestApp.SharedAsync();
            var (user, customer) = await NewUserWithoutCardsAsync(app);

            using var scope = app.NewScope();
            var created = await scope.ServiceProvider.GetRequiredService<ICardService>().CreateInitialCardsAsync(user.Id);

            var stored = await app.QueryAsync(db => db.Cards.AsNoTracking().Where(c => c.UserId == user.Id).ToListAsync());
            Assert.Equal(2, stored.Count);
            Assert.All(stored, c => Assert.Equal(16, c.CardNumber.Length));
            Assert.All(stored, c => Assert.StartsWith("96301020", c.CardNumber));
            Assert.All(stored, c => Assert.True(Service.Helpers.CardNumberGenerator.IsValid(c.CardNumber)));
            Assert.All(stored, c => Assert.Equal(DateTime.UtcNow.Year + 4, c.ExpiryDate.Year));
            Assert.All(stored, c => Assert.Equal(DateTime.DaysInMonth(c.ExpiryDate.Year, c.ExpiryDate.Month), c.ExpiryDate.Day));
            Assert.All(created.Data!, c => Assert.Equal((customer.Name + " " + customer.Surname).ToUpperInvariant(), c.HolderName));
            Assert.Equal(2, stored.Select(c => c.CardNumber).Distinct().Count());
        }

        [Fact]
        public async Task CreateInitialCards_cannot_be_run_twice()
        {
            var app = await TestApp.SharedAsync();
            var (user, _) = await NewUserWithoutCardsAsync(app);

            using var first = app.NewScope();
            await first.ServiceProvider.GetRequiredService<ICardService>().CreateInitialCardsAsync(user.Id);
            using var second = app.NewScope();
            var again = await second.ServiceProvider.GetRequiredService<ICardService>().CreateInitialCardsAsync(user.Id);

            Assert.False(again.IsSuccess);
            Assert.Equal("Your first cards have already been created.", again.Errors[0]);
            Assert.Equal(2, (await app.QueryAsync(db => db.Cards.CountAsync(c => c.UserId == user.Id))));
        }

        [Fact]
        public async Task CreateInitialCards_needs_an_account_linked_to_a_citizen()
        {
            var app = await TestApp.SharedAsync();
            AppUser staff;
            using (var scope = app.NewScope())
            {
                staff = await TestData.NewUserAsync(scope.ServiceProvider, null, Roles.Admin);
            }

            using var run = app.NewScope();
            var result = await run.ServiceProvider.GetRequiredService<ICardService>().CreateInitialCardsAsync(staff.Id);

            Assert.False(result.IsSuccess);
            Assert.Contains("not linked to a citizen record", result.Errors[0]);
        }

        [Fact]
        public async Task CreateInitialCards_for_an_unknown_user_is_not_found()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().CreateInitialCardsAsync("nobody");
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task Parallel_initial_card_requests_create_only_one_pair()
        {
            var app = await TestApp.SharedAsync();
            var (user, _) = await NewUserWithoutCardsAsync(app);

            var tasks = Enumerable.Range(0, 5).Select(async _ =>
            {
                using var scope = app.NewScope();
                return await scope.ServiceProvider.GetRequiredService<ICardService>().CreateInitialCardsAsync(user.Id);
            }).ToList();
            var results = await Task.WhenAll(tasks);

            Assert.Equal(1, results.Count(r => r.IsSuccess));
            Assert.Equal(2, await app.QueryAsync(db => db.Cards.CountAsync(c => c.UserId == user.Id)));
        }

        // ---------- Kartların siyahısı ----------

        [Fact]
        public async Task GetMyCards_returns_only_my_cards_in_creation_order()
        {
            var app = await TestApp.SharedAsync();
            var mine = await app.NewKitAsync(10m);
            var other = await app.NewKitAsync(20m);

            using var scope = app.NewScope();
            var cards = (await scope.ServiceProvider.GetRequiredService<ICardService>().GetMyCardsAsync(mine.User.Id)).ToList();

            Assert.Equal(new[] { mine.Standard.Id, mine.Cashback.Id }, cards.Select(c => c.Id).ToArray());
            Assert.DoesNotContain(cards, c => c.Id == other.Standard.Id);
            Assert.Equal(10m, cards[0].Balance);
            Assert.False(string.IsNullOrEmpty(cards[0].DesignImage));
        }

        [Fact]
        public async Task GetMyCards_for_an_unknown_user_is_empty()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<ICardService>().GetMyCardsAsync("nobody"));
        }

        [Fact]
        public async Task GetMyCard_returns_the_full_number_and_tier_rules_to_the_owner_only()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var other = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            var mine = await service.GetMyCardAsync(kit.User.Id, kit.Standard.Id);
            var theirs = await service.GetMyCardAsync(other.User.Id, kit.Standard.Id);

            Assert.True(mine.IsSuccess);
            Assert.Equal(kit.Standard.CardNumber, mine.Data!.CardNumber);
            Assert.Equal(500m, mine.Data.TransferLimit);
            Assert.Equal(1m, mine.Data.CommissionPercent);
            Assert.Equal(10m, mine.Data.IssueFee);
            Assert.True(theirs.IsNotFound);
        }

        // ---------- Əlavə kart ----------

        [Fact]
        public async Task AddCard_charges_the_issue_fee_and_opens_the_card()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(100m);

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().AddCardAsync(kit.User.Id, new AddCardDto { Tier = "Gold", FundingCardId = kit.Standard.Id });

            Assert.True(result.IsSuccess);
            Assert.Equal("Gold", result.Data!.Tier);
            Assert.Equal(60m, await app.BalanceAsync(kit.Standard.Id));          // Gold haqqı 40
            var fee = Assert.Single((await app.TransactionsAsync(kit.Standard.Id)), t => t.Type == TransactionType.CardFee);
            Assert.Equal(40m, fee.Amount);
            Assert.Equal(60m, fee.BalanceAfter);
            Assert.Equal(3, await app.QueryAsync(db => db.Cards.CountAsync(c => c.UserId == kit.User.Id)));
        }

        [Fact]
        public async Task AddCard_accepts_the_tier_name_in_any_case()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(100m);

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().AddCardAsync(kit.User.Id, new AddCardDto { Tier = "silver", FundingCardId = kit.Standard.Id });

            Assert.True(result.IsSuccess);
            Assert.Equal("Silver", result.Data!.Tier);
            Assert.Equal(85m, await app.BalanceAsync(kit.Standard.Id));          // Silver haqqı 15
        }

        [Fact]
        public async Task AddCard_fails_without_enough_balance_and_changes_nothing()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(5m);

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().AddCardAsync(kit.User.Id, new AddCardDto { Tier = "Gold", FundingCardId = kit.Standard.Id });

            Assert.False(result.IsSuccess);
            Assert.Equal("That card doesn't have enough balance for the opening fee.", result.Errors[0]);
            Assert.Equal(5m, await app.BalanceAsync(kit.Standard.Id));
            Assert.Equal(2, await app.QueryAsync(db => db.Cards.CountAsync(c => c.UserId == kit.User.Id)));
        }

        [Fact]
        public async Task AddCard_cannot_charge_a_blocked_card()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var blocked = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 500m, blocked: true);

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().AddCardAsync(kit.User.Id, new AddCardDto { Tier = "Gold", FundingCardId = blocked.Id });

            Assert.False(result.IsSuccess);
            Assert.Equal("That card is blocked. Choose another card.", result.Errors[0]);
        }

        [Fact]
        public async Task AddCard_cannot_charge_somebody_elses_card()
        {
            var app = await TestApp.SharedAsync();
            var mine = await app.NewKitAsync();
            var other = await app.NewKitAsync(500m);

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().AddCardAsync(mine.User.Id, new AddCardDto { Tier = "Gold", FundingCardId = other.Standard.Id });

            Assert.False(result.IsSuccess);
            Assert.Equal("Choose one of your own cards to charge.", result.Errors[0]);
            Assert.Equal(500m, await app.BalanceAsync(other.Standard.Id));
        }

        [Fact]
        public async Task AddCard_cannot_open_a_cashback_card()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(100m);

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().AddCardAsync(kit.User.Id, new AddCardDto { Tier = "Cashback", FundingCardId = kit.Standard.Id });

            Assert.False(result.IsSuccess);
            Assert.Equal("Choose Standard, Silver or Gold.", result.Errors[0]);
        }

        [Fact]
        public async Task AddCard_for_an_unknown_user_is_not_found()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().AddCardAsync("nobody", new AddCardDto { Tier = "Gold", FundingCardId = 1 });
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task AddCard_fee_goes_to_the_treasury()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var kit = await app.NewKitAsync(100m);

                using var scope = app.NewScope();
                await scope.ServiceProvider.GetRequiredService<ICardService>().AddCardAsync(kit.User.Id, new AddCardDto { Tier = "Gold", FundingCardId = kit.Standard.Id });

                var entry = Assert.Single(await app.QueryAsync(db => db.TreasuryEntries.AsNoTracking().Where(t => t.Kind != TreasuryEntryKind.Capital).ToListAsync()));
                Assert.Equal(TreasuryEntryKind.CardFee, entry.Kind);
                Assert.Equal(40m, entry.Amount);
            }
        }

        // ---------- Top-up ----------

        [Fact]
        public async Task TopUp_adds_money_and_writes_a_journal_row_with_only_the_last_four_digits()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(10m);

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().TopUpAsync(kit.User.Id, kit.Standard.Id, ValidTopUp(250.50m));

            Assert.True(result.IsSuccess);
            Assert.Equal(260.50m, result.Data!.Balance);
            Assert.Equal(260.50m, await app.BalanceAsync(kit.Standard.Id));

            var row = Assert.Single(await app.TransactionsAsync(kit.Standard.Id));
            Assert.Equal(TransactionType.TopUp, row.Type);
            Assert.True(row.IsIncome);
            Assert.Equal(250.50m, row.Amount);
            Assert.Equal(260.50m, row.BalanceAfter);
            Assert.Equal("Top up from card •••• 1111", row.Description);
            Assert.DoesNotContain("4111", row.Description.Replace("1111", ""));
            Assert.StartsWith("TU-", row.Reference);
        }

        [Fact]
        public async Task TopUp_of_the_cashback_card_is_refused()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().TopUpAsync(kit.User.Id, kit.Cashback.Id, ValidTopUp(10m));

            Assert.False(result.IsSuccess);
            Assert.Equal("The Cashback card can't be topped up. Choose another card.", result.Errors[0]);
            Assert.Equal(0m, await app.BalanceAsync(kit.Cashback.Id));
        }

        [Fact]
        public async Task TopUp_of_a_blocked_card_is_refused()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var blocked = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m, blocked: true);

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().TopUpAsync(kit.User.Id, blocked.Id, ValidTopUp(10m));

            Assert.False(result.IsSuccess);
            Assert.Equal("This card is blocked.", result.Errors[0]);
        }

        [Fact]
        public async Task TopUp_of_somebody_elses_card_is_not_found()
        {
            var app = await TestApp.SharedAsync();
            var mine = await app.NewKitAsync();
            var other = await app.NewKitAsync();

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().TopUpAsync(mine.User.Id, other.Standard.Id, ValidTopUp(10m));

            Assert.True(result.IsNotFound);
            Assert.Equal(0m, await app.BalanceAsync(other.Standard.Id));
        }

        [Fact]
        public async Task TopUp_with_an_invalid_card_is_rejected_before_anything_changes()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var model = ValidTopUp(10m);
            model.SourceCardNumber = "4111111111111112";

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().TopUpAsync(kit.User.Id, kit.Standard.Id, model);

            Assert.False(result.IsSuccess);
            Assert.Contains("Enter a valid card number.", result.Errors);
            Assert.Empty(await app.TransactionsAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task Several_top_ups_add_up()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            await service.TopUpAsync(kit.User.Id, kit.Standard.Id, ValidTopUp(100m));
            await service.TopUpAsync(kit.User.Id, kit.Standard.Id, ValidTopUp(50m));

            Assert.Equal(150m, await app.BalanceAsync(kit.Standard.Id));
            Assert.Equal(2, (await app.TransactionsAsync(kit.Standard.Id)).Count);
        }

        // ---------- Əməliyyatlar ----------

        [Fact]
        public async Task GetTransactions_returns_the_latest_first_and_only_to_the_owner()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var other = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            await service.TopUpAsync(kit.User.Id, kit.Standard.Id, ValidTopUp(10m));
            await service.TopUpAsync(kit.User.Id, kit.Standard.Id, ValidTopUp(20m));

            var mine = await service.GetTransactionsAsync(kit.User.Id, kit.Standard.Id, 10);
            var theirs = await service.GetTransactionsAsync(other.User.Id, kit.Standard.Id, 10);

            Assert.True(mine.IsSuccess);
            Assert.Equal(new[] { 20m, 10m }, mine.Data!.Select(t => t.Amount).ToArray());
            Assert.True(theirs.IsNotFound);
        }

        [Fact]
        public async Task GetTransactions_take_is_clamped_between_one_and_one_hundred()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            for (var i = 0; i < 3; i++)
            {
                await service.TopUpAsync(kit.User.Id, kit.Standard.Id, ValidTopUp(1m));
            }

            Assert.Single((await service.GetTransactionsAsync(kit.User.Id, kit.Standard.Id, 0)).Data!);
            Assert.Single((await service.GetTransactionsAsync(kit.User.Id, kit.Standard.Id, -5)).Data!);
            Assert.Equal(3, (await service.GetTransactionsAsync(kit.User.Id, kit.Standard.Id, 1000)).Data!.Count());
        }

        [Fact]
        public async Task GetRecentActivity_mixes_all_cards_with_labels_newest_first()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var silver = await app.NewCardAsync(kit.User.Id, CardTier.Silver);

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            await service.TopUpAsync(kit.User.Id, kit.Standard.Id, ValidTopUp(10m));
            await service.TopUpAsync(kit.User.Id, silver.Id, ValidTopUp(20m));

            var recent = (await service.GetRecentActivityAsync(kit.User.Id, 10)).ToList();

            Assert.Equal(2, recent.Count);
            Assert.Equal(20m, recent[0].Amount);
            Assert.StartsWith("Silver ••••", recent[0].CardLabel);
            Assert.StartsWith("Standard ••••", recent[1].CardLabel);
        }

        // ---------- Blok və blokdan çıxarma ----------

        [Fact]
        public async Task Block_flow_sends_a_code_then_blocks_the_card_and_notifies_the_owner()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(100m);

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            var sent = await service.SendBlockCodeAsync(kit.User.Id, kit.Standard.Id);

            Assert.True(sent.IsSuccess);
            Assert.Equal(MaskedEmail(kit.User.Email!), sent.Data!.MaskedEmail);
            var code = app.Emails.LastTo(kit.User.Email!)!.Code!;
            Assert.Equal(code, sent.Data.DevCode);

            var blocked = await service.BlockAsync(kit.User.Id, kit.Standard.Id, new BlockCardDto { Code = code });

            Assert.True(blocked.IsSuccess);
            Assert.True(blocked.Data!.IsBlocked);
            Assert.True(await app.QueryAsync(db => db.Cards.AsNoTracking().Where(c => c.Id == kit.Standard.Id).Select(c => c.IsBlocked).FirstAsync()));
            var notification = Assert.Single(await app.NotificationsAsync(kit.User.Id));
            Assert.Equal("Card blocked", notification.Title);
            Assert.Equal(NotificationCategory.Card, notification.Category);
        }

        private static string MaskedEmail(string email)
        {
            var parts = email.Split('@');
            return parts[0][..1] + "****@" + parts[1];
        }

        [Fact]
        public async Task Block_with_a_wrong_code_does_nothing()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            await service.SendBlockCodeAsync(kit.User.Id, kit.Standard.Id);
            var real = app.Emails.LastTo(kit.User.Email!)!.Code!;
            var wrong = real == "000000" ? "111111" : "000000";

            var result = await service.BlockAsync(kit.User.Id, kit.Standard.Id, new BlockCardDto { Code = wrong });

            Assert.False(result.IsSuccess);
            Assert.Equal("The code is incorrect.", result.Errors[0]);
            Assert.False(await app.QueryAsync(db => db.Cards.AsNoTracking().Where(c => c.Id == kit.Standard.Id).Select(c => c.IsBlocked).FirstAsync()));
        }

        [Fact]
        public async Task Block_without_asking_for_a_code_first_fails()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().BlockAsync(kit.User.Id, kit.Standard.Id, new BlockCardDto { Code = "123456" });

            Assert.False(result.IsSuccess);
            Assert.Equal("The code has expired or is invalid. Please request a new one.", result.Errors[0]);
        }

        [Fact]
        public async Task A_code_for_one_card_does_not_block_another_card()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var silver = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m);

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            await service.SendBlockCodeAsync(kit.User.Id, kit.Standard.Id);
            var code = app.Emails.LastTo(kit.User.Email!)!.Code!;

            var result = await service.BlockAsync(kit.User.Id, silver.Id, new BlockCardDto { Code = code });

            Assert.False(result.IsSuccess);
            Assert.False(await app.QueryAsync(db => db.Cards.AsNoTracking().Where(c => c.Id == silver.Id).Select(c => c.IsBlocked).FirstAsync()));
        }

        [Fact]
        public async Task A_block_code_is_used_up_after_one_success()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            await service.SendBlockCodeAsync(kit.User.Id, kit.Standard.Id);
            var code = app.Emails.LastTo(kit.User.Email!)!.Code!;
            await service.BlockAsync(kit.User.Id, kit.Standard.Id, new BlockCardDto { Code = code });

            var again = await service.BlockAsync(kit.User.Id, kit.Standard.Id, new BlockCardDto { Code = code });

            Assert.False(again.IsSuccess);
            Assert.Equal("This card is already blocked.", again.Errors[0]);
        }

        [Fact]
        public async Task The_cashback_card_cannot_be_blocked()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            var sent = await service.SendBlockCodeAsync(kit.User.Id, kit.Cashback.Id);
            var blocked = await service.BlockAsync(kit.User.Id, kit.Cashback.Id, new BlockCardDto { Code = "123456" });

            Assert.False(sent.IsSuccess);
            Assert.Equal("The Cashback card can't be blocked.", sent.Errors[0]);
            Assert.False(blocked.IsSuccess);
            Assert.Equal("The Cashback card can't be blocked.", blocked.Errors[0]);
        }

        [Fact]
        public async Task Blocking_somebody_elses_card_is_not_found()
        {
            var app = await TestApp.SharedAsync();
            var mine = await app.NewKitAsync();
            var other = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            Assert.True((await service.SendBlockCodeAsync(mine.User.Id, other.Standard.Id)).IsNotFound);
            Assert.True((await service.BlockAsync(mine.User.Id, other.Standard.Id, new BlockCardDto { Code = "123456" })).IsNotFound);
            Assert.True((await service.SendUnblockCodeAsync(mine.User.Id, other.Standard.Id)).IsNotFound);
            Assert.True((await service.UnblockAsync(mine.User.Id, other.Standard.Id, new UnblockCardDto { Code = "123456" })).IsNotFound);
        }

        [Fact]
        public async Task A_new_code_cannot_be_requested_within_sixty_seconds()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            var first = await service.SendBlockCodeAsync(kit.User.Id, kit.Standard.Id);
            var second = await service.SendBlockCodeAsync(kit.User.Id, kit.Standard.Id);

            Assert.True(first.IsSuccess);
            Assert.False(second.IsSuccess);
            Assert.StartsWith("Please wait", second.Errors[0]);
        }

        [Fact]
        public async Task Unblock_flow_reopens_a_blocked_card()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var blocked = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m, blocked: true);

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            var sent = await service.SendUnblockCodeAsync(kit.User.Id, blocked.Id);
            Assert.True(sent.IsSuccess);
            var code = app.Emails.LastTo(kit.User.Email!)!.Code!;

            var result = await service.UnblockAsync(kit.User.Id, blocked.Id, new UnblockCardDto { Code = code });

            Assert.True(result.IsSuccess);
            Assert.False(result.Data!.IsBlocked);
            Assert.Equal("Card unblocked", Assert.Single(await app.NotificationsAsync(kit.User.Id)).Title);
        }

        [Fact]
        public async Task Unblock_of_a_card_that_is_not_blocked_is_refused()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            Assert.Equal("This card is not blocked.", (await service.SendUnblockCodeAsync(kit.User.Id, kit.Standard.Id)).Errors[0]);
            Assert.Equal("This card is not blocked.", (await service.UnblockAsync(kit.User.Id, kit.Standard.Id, new UnblockCardDto { Code = "123456" })).Errors[0]);
        }

        [Fact]
        public async Task A_restricted_account_cannot_unblock_its_cards()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();
            var blocked = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m, blocked: true);
            await app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == kit.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true));
                return 0;
            });

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            Assert.Equal("This account is restricted. Contact the bank.", (await service.SendUnblockCodeAsync(kit.User.Id, blocked.Id)).Errors[0]);
            Assert.Equal("This account is restricted. Contact the bank.", (await service.UnblockAsync(kit.User.Id, blocked.Id, new UnblockCardDto { Code = "123456" })).Errors[0]);
        }

        [Fact]
        public async Task Five_wrong_codes_cancel_the_code()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = scope.ServiceProvider.GetRequiredService<ICardService>();
            await service.SendBlockCodeAsync(kit.User.Id, kit.Standard.Id);
            var real = app.Emails.LastTo(kit.User.Email!)!.Code!;
            var wrong = real == "000000" ? "111111" : "000000";

            for (var i = 0; i < 5; i++)
            {
                await service.BlockAsync(kit.User.Id, kit.Standard.Id, new BlockCardDto { Code = wrong });
            }
            var sixth = await service.BlockAsync(kit.User.Id, kit.Standard.Id, new BlockCardDto { Code = wrong });
            var correct = await service.BlockAsync(kit.User.Id, kit.Standard.Id, new BlockCardDto { Code = real });

            Assert.Equal("Too many attempts. Please request a new code.", sixth.Errors[0]);
            Assert.False(correct.IsSuccess);        // kod artıq ləğv olunub, düzgün kod da işləmir
        }

        [Fact]
        public async Task Block_validates_the_code_format()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync();

            using var scope = app.NewScope();
            var result = await scope.ServiceProvider.GetRequiredService<ICardService>().BlockAsync(kit.User.Id, kit.Standard.Id, new BlockCardDto { Code = "12" });

            Assert.False(result.IsSuccess);
            Assert.Equal("The code is 6 digits.", result.Errors[0]);
        }
    }
}
