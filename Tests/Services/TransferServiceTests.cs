using CaspianBank.Tests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository.Data;
using Service.Helpers.DTOs.Transfers;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    public class TransferServiceTests
    {
        private static string NewRequest()
        {
            return Guid.NewGuid().ToString();
        }

        private static async Task<ITransferService> ServiceAsync(IServiceScope scope)
        {
            await Task.CompletedTask;
            return scope.ServiceProvider.GetRequiredService<ITransferService>();
        }

        private static async Task<ExternalCard> NewExternalCardAsync(TestApp app, string holder = "Elnur Məmmədov", string binPrefix = "552209")
        {
            using var scope = app.NewScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var n = TestData.Next();
            var bank = await db.ExternalBanks.FirstOrDefaultAsync(b => b.Bin == binPrefix + "93");
            if (bank is null)
            {
                bank = new ExternalBank { Name = "International Bank of Azerbaijan (ABB)", Bin = binPrefix + "93" };
                db.ExternalBanks.Add(bank);
                await db.SaveChangesAsync();
            }

            var card = new ExternalCard { ExternalBankId = bank.Id, CardNumber = binPrefix + (n % 10000000000L).ToString("D10"), HolderName = holder, IsActive = true };
            db.ExternalCards.Add(card);
            await db.SaveChangesAsync();
            return card;
        }

        // ---------- Öz kartları arasında ----------

        [Fact]
        public async Task Transfer_between_own_cards_moves_the_money_without_commission_inside_the_limit()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(1000m);
            var silver = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = silver.Id, Amount = 100m, RequestId = NewRequest() });

            Assert.True(result.IsSuccess);
            Assert.Equal(100m, result.Data!.Amount);
            Assert.Equal(0m, result.Data.Commission);
            Assert.Equal(900m, result.Data.FromBalanceAfter);
            Assert.Equal(900m, await app.BalanceAsync(kit.Standard.Id));
            Assert.Equal(100m, await app.BalanceAsync(silver.Id));
        }

        [Fact]
        public async Task Transfer_writes_matching_out_and_in_rows_with_the_same_reference()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(500m);
            var silver = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = silver.Id, Amount = 40m, Note = "  rent  ", RequestId = NewRequest() });

            var outRows = await app.TransactionsAsync(kit.Standard.Id);
            var inRows = await app.TransactionsAsync(silver.Id);
            var outRow = Assert.Single(outRows);
            var inRow = Assert.Single(inRows);
            Assert.Equal(TransactionType.TransferOut, outRow.Type);
            Assert.Equal(TransactionType.TransferIn, inRow.Type);
            Assert.False(outRow.IsIncome);
            Assert.True(inRow.IsIncome);
            Assert.Equal(outRow.Reference, inRow.Reference);
            Assert.Equal(result.Data!.Reference, outRow.Reference);
            Assert.Equal("rent", outRow.Note);
            Assert.Equal(460m, outRow.BalanceAfter);
            Assert.Equal(40m, inRow.BalanceAfter);
        }

        [Fact]
        public async Task Transfer_between_own_cards_sends_no_notification()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(500m);
            var silver = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m);

            using var scope = app.NewScope();
            await (await ServiceAsync(scope)).TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = silver.Id, Amount = 10m, RequestId = NewRequest() });

            Assert.Empty(await app.NotificationsAsync(kit.User.Id));
        }

        [Fact]
        public async Task Cashback_card_can_send_to_another_own_card_without_commission()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(0m);
            await app.SetBalanceAsync(kit.Cashback.Id, 25m);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Cashback.Id, ToCardId = kit.Standard.Id, Amount = 25m, RequestId = NewRequest() });

            Assert.True(result.IsSuccess);
            Assert.Equal(0m, result.Data!.Commission);
            Assert.Equal(25m, await app.BalanceAsync(kit.Standard.Id));
            Assert.Equal(0m, await app.BalanceAsync(kit.Cashback.Id));
        }

        [Fact]
        public async Task Nothing_can_be_transferred_to_the_cashback_card()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(100m);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = kit.Cashback.Id, Amount = 10m });

            Assert.False(result.IsSuccess);
            Assert.Contains("Cashback card only collects cashback", result.Errors[0]);
            Assert.Equal(100m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task Another_customers_cashback_card_cannot_receive_a_transfer_either()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(100m);
            var other = await app.NewKitAsync();

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = other.Cashback.CardNumber, Amount = 10m });

            Assert.False(result.IsSuccess);
            Assert.Equal("This card can't receive transfers.", result.Errors[0]);
        }

        [Fact]
        public async Task Transfer_to_the_same_card_is_rejected()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(100m);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = kit.Standard.Id, Amount = 10m });

            Assert.False(result.IsSuccess);
            Assert.Equal("Choose a different card from the one you're sending from.", result.Errors[0]);
        }

        // ---------- Başqa Caspian müştərisinə ----------

        [Fact]
        public async Task Transfer_to_another_customer_credits_their_card_and_notifies_them()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(300m, "Nurlan", "Efendiyev");
            var recipient = await app.NewKitAsync();

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = 50m, RequestId = NewRequest() });

            Assert.True(result.IsSuccess);
            Assert.Equal(250m, await app.BalanceAsync(sender.Standard.Id));
            Assert.Equal(50m, await app.BalanceAsync(recipient.Standard.Id));

            var notification = Assert.Single(await app.NotificationsAsync(recipient.User.Id));
            Assert.Equal("Money received", notification.Title);
            Assert.Equal(NotificationCategory.Transfer, notification.Category);
            Assert.Contains("50.00 AZN", notification.Message);
            Assert.Contains("Nurlan E.", notification.Message);              // soyad qısaldılır
            Assert.DoesNotContain("Efendiyev", notification.Message);
            Assert.Equal($"/App/Card/{recipient.Standard.Id}", notification.LinkUrl);
        }

        [Fact]
        public async Task Receipt_masks_the_recipient_name_and_card_number()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(300m);
            var recipient = await app.NewKitAsync(0m, "Aysel", "Məmmədova");

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = 5m });

            Assert.Equal("Aysel M.", result.Data!.ToHolder);
            Assert.Equal("•••• " + recipient.Standard.CardNumber[^4..], result.Data.ToLabel);
            Assert.False(result.Data.IsExternal);
        }

        [Fact]
        public async Task Card_number_with_spaces_is_accepted()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(300m);
            var recipient = await app.NewKitAsync();
            var number = recipient.Standard.CardNumber;
            var spaced = $"{number[..4]} {number.Substring(4, 4)} {number.Substring(8, 4)} {number.Substring(12, 4)}";

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = spaced, Amount = 5m });

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task Unknown_card_number_gives_a_not_found_message()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(300m);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = "1111222233334444", Amount = 5m });

            Assert.False(result.IsSuccess);
            Assert.Equal("We couldn't find a card with that number.", result.Errors[0]);
        }

        // ---------- Komissiya ----------

        [Fact]
        public async Task Commission_applies_only_above_the_monthly_limit()
        {
            // Standard: aylıq limit 500, komissiya 1%
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(2000m);
            var recipient = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = await ServiceAsync(scope);
            var result = await service.TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = 600m, RequestId = NewRequest() });

            Assert.True(result.IsSuccess);
            Assert.Equal(1.00m, result.Data!.Commission);
            Assert.Equal(601m, result.Data.Total);
            Assert.Equal(2000m - 601m, await app.BalanceAsync(sender.Standard.Id));
            Assert.Equal(600m, await app.BalanceAsync(recipient.Standard.Id));

            var rows = await app.TransactionsAsync(sender.Standard.Id);
            var commission = Assert.Single(rows, r => r.Type == TransactionType.Commission);
            Assert.Equal(1.00m, commission.Amount);
        }

        [Fact]
        public async Task After_the_limit_is_used_every_transfer_is_fully_charged()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(2000m);
            var recipient = await app.NewKitAsync();

            using var scope = app.NewScope();
            var service = await ServiceAsync(scope);
            await service.TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = 500m, RequestId = NewRequest() });
            var second = await service.TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = 200m, RequestId = NewRequest() });

            Assert.Equal(2.00m, second.Data!.Commission);
        }

        [Fact]
        public async Task Own_card_transfers_count_toward_the_monthly_limit_too()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(2000m);
            var silver = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m);

            using var scope = app.NewScope();
            var service = await ServiceAsync(scope);
            await service.TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = silver.Id, Amount = 450m, RequestId = NewRequest() });
            var second = await service.TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = silver.Id, Amount = 100m, RequestId = NewRequest() });

            Assert.Equal(0.50m, second.Data!.Commission);       // 50 limit içində, 50 artıq
        }

        [Fact]
        public async Task Preview_shows_the_commission_and_does_not_move_money()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(2000m);
            var recipient = await app.NewKitAsync();

            using var scope = app.NewScope();
            var preview = await (await ServiceAsync(scope)).PreviewAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = 700m });

            Assert.True(preview.IsSuccess);
            Assert.Equal(2.00m, preview.Data!.Commission);
            Assert.Equal(702m, preview.Data.Total);
            Assert.Equal(500m, preview.Data.MonthlyLimit);
            Assert.Equal(0m, preview.Data.UsedThisMonth);
            Assert.Equal(200m, preview.Data.ChargedAmount);
            Assert.Equal(2000m, preview.Data.FromBalance);
            Assert.Equal(2000m, await app.BalanceAsync(sender.Standard.Id));
            Assert.Empty(await app.TransactionsAsync(sender.Standard.Id));
        }

        [Fact]
        public async Task Commission_is_written_to_the_treasury_in_the_same_save()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var sender = await app.NewKitAsync(2000m);
                var recipient = await app.NewKitAsync();

                using var scope = app.NewScope();
                await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = 600m, RequestId = NewRequest() });

                var entries = await app.QueryAsync(db => db.TreasuryEntries.AsNoTracking().Where(t => t.Kind != TreasuryEntryKind.Capital).ToListAsync());
                var entry = Assert.Single(entries);
                Assert.Equal(TreasuryEntryKind.Commission, entry.Kind);
                Assert.Equal(1.00m, entry.Amount);
            }
        }

        // ---------- Balans və kartların vəziyyəti ----------

        [Fact]
        public async Task Insufficient_funds_for_amount_plus_commission_is_rejected()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(600m);
            var recipient = await app.NewKitAsync();

            using var scope = app.NewScope();
            // 600 + 1 komissiya = 601 > 600
            var result = await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = 600m });

            Assert.False(result.IsSuccess);
            Assert.Equal("The source card doesn't cover the amount and commission.", result.Errors[0]);
            Assert.Equal(600m, await app.BalanceAsync(sender.Standard.Id));
            Assert.Equal(0m, await app.BalanceAsync(recipient.Standard.Id));
        }

        [Fact]
        public async Task Blocked_source_card_cannot_send()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(100m);
            var blocked = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 100m, blocked: true);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(kit.User.Id, new TransferDto { FromCardId = blocked.Id, ToCardId = kit.Standard.Id, Amount = 10m });

            Assert.False(result.IsSuccess);
            Assert.Equal("The source card is blocked.", result.Errors[0]);
        }

        [Fact]
        public async Task Blocked_destination_card_cannot_receive()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(100m);
            var recipient = await app.NewKitAsync();
            var blocked = await app.NewCardAsync(recipient.User.Id, CardTier.Silver, 0m, blocked: true);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = blocked.CardNumber, Amount = 10m });

            Assert.False(result.IsSuccess);
            Assert.Equal("The destination card is blocked.", result.Errors[0]);
        }

        [Fact]
        public async Task Cannot_send_from_somebody_elses_card()
        {
            var app = await TestApp.SharedAsync();
            var owner = await app.NewKitAsync(100m);
            var thief = await app.NewKitAsync(100m);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(thief.User.Id, new TransferDto { FromCardId = owner.Standard.Id, ToCardId = thief.Standard.Id, Amount = 10m });

            Assert.False(result.IsSuccess);
            Assert.Equal("Choose one of your own cards to send from.", result.Errors[0]);
            Assert.Equal(100m, await app.BalanceAsync(owner.Standard.Id));
        }

        [Fact]
        public async Task Cannot_choose_somebody_elses_card_as_an_own_destination()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(100m);
            var other = await app.NewKitAsync();

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardId = other.Standard.Id, Amount = 10m });

            Assert.False(result.IsSuccess);
            Assert.Equal("Choose another of your own cards, or send to another customer.", result.Errors[0]);
        }

        [Fact]
        public async Task Restricted_account_cannot_transfer()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(100m);
            await app.QueryAsync(async db =>
            {
                await db.Users.Where(u => u.Id == kit.User.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRestricted, true));
                return 0;
            });
            var silver = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = silver.Id, Amount = 10m });

            Assert.False(result.IsSuccess);
            Assert.Equal("This account is restricted. Contact the bank.", result.Errors[0]);
        }

        [Fact]
        public async Task Unknown_user_is_asked_to_log_in_again()
        {
            var app = await TestApp.SharedAsync();
            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync("no-such-user", new TransferDto { FromCardId = 1, ToCardId = 2, Amount = 10m });
            Assert.Equal("Please log in again.", result.Errors[0]);
        }

        [Fact]
        public async Task Invalid_input_is_reported_by_the_validator()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(100m);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = kit.Cashback.Id, Amount = -5m });

            Assert.False(result.IsSuccess);
            Assert.Contains("Enter an amount greater than zero.", result.Errors);
        }

        // ---------- Təkrar sorğu ----------

        [Fact]
        public async Task Same_request_id_does_not_move_the_money_twice()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(300m);
            var recipient = await app.NewKitAsync();
            var request = NewRequest();
            var model = new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = recipient.Standard.CardNumber, Amount = 50m, RequestId = request };

            using var first = app.NewScope();
            var one = await (await ServiceAsync(first)).TransferAsync(sender.User.Id, model);
            using var second = app.NewScope();
            var two = await (await ServiceAsync(second)).TransferAsync(sender.User.Id, model);

            Assert.True(one.IsSuccess);
            Assert.True(two.IsSuccess);
            Assert.Equal(one.Data!.Reference, two.Data!.Reference);
            Assert.Equal(250m, await app.BalanceAsync(sender.Standard.Id));
            Assert.Equal(50m, await app.BalanceAsync(recipient.Standard.Id));
            Assert.Single(await app.NotificationsAsync(recipient.User.Id));
        }

        [Fact]
        public async Task Reference_comes_from_the_request_id()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(100m);
            var silver = await app.NewCardAsync(kit.User.Id, CardTier.Silver);
            var request = Guid.NewGuid();

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = silver.Id, Amount = 10m, RequestId = request.ToString() });

            Assert.Equal("TR-" + request.ToString("N")[..12].ToUpperInvariant(), result.Data!.Reference);
        }

        [Fact]
        public async Task Another_user_cannot_read_a_receipt_through_the_same_request_id()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(100m);
            var silver = await app.NewCardAsync(sender.User.Id, CardTier.Silver);
            var attacker = await app.NewKitAsync(100m);
            var attackerSilver = await app.NewCardAsync(attacker.User.Id, CardTier.Silver);
            var request = NewRequest();

            using (var scope = app.NewScope())
            {
                await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardId = silver.Id, Amount = 10m, RequestId = request });
            }

            using var other = app.NewScope();
            var result = await (await ServiceAsync(other)).TransferAsync(attacker.User.Id, new TransferDto { FromCardId = attacker.Standard.Id, ToCardId = attackerSilver.Id, Amount = 5m, RequestId = request });

            // Reference eynidir, amma qəbz yalnız göndərənin özünə qaytarılır: bu istifadəçi öz köçürməsini edir ya da rədd olunur, başqasının qəbzini almır
            if (result.IsSuccess)
            {
                Assert.Equal(5m, result.Data!.Amount);
            }
            Assert.Equal(90m, await app.BalanceAsync(sender.Standard.Id));
        }

        // ---------- Başqa banka ----------

        [Fact]
        public async Task Transfer_to_another_bank_debits_the_source_and_records_the_external_transfer()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(1000m);
            var external = await NewExternalCardAsync(app);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = external.CardNumber, Amount = 100m, RequestId = NewRequest() });

            Assert.True(result.IsSuccess);
            Assert.True(result.Data!.IsExternal);
            Assert.Equal("ABB", result.Data.ToBankName);
            Assert.Equal("Elnur M.", result.Data.ToHolder);
            Assert.Equal(900m, await app.BalanceAsync(sender.Standard.Id));

            var records = await app.QueryAsync(db => db.ExternalTransfers.AsNoTracking().Where(t => t.UserId == sender.User.Id).ToListAsync());
            var record = Assert.Single(records);
            Assert.Equal(100m, record.Amount);
            Assert.Equal(external.CardNumber[^4..], record.CardLast4);
        }

        [Fact]
        public async Task Transfer_to_another_bank_pays_the_same_monthly_limit_commission()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(1000m);
            var external = await NewExternalCardAsync(app);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = external.CardNumber, Amount = 600m, RequestId = NewRequest() });

            Assert.Equal(1.00m, result.Data!.Commission);
            Assert.Equal(399m, await app.BalanceAsync(sender.Standard.Id));
        }

        [Fact]
        public async Task Transfer_to_another_bank_sends_no_notification_to_anyone()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(1000m);
            var external = await NewExternalCardAsync(app);

            using var scope = app.NewScope();
            await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = external.CardNumber, Amount = 50m });

            Assert.Empty(await app.NotificationsAsync(sender.User.Id));
        }

        [Fact]
        public async Task Same_request_id_to_another_bank_is_not_paid_twice()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(1000m);
            var external = await NewExternalCardAsync(app);
            var model = new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = external.CardNumber, Amount = 100m, RequestId = NewRequest() };

            using var first = app.NewScope();
            await (await ServiceAsync(first)).TransferAsync(sender.User.Id, model);
            using var second = app.NewScope();
            var again = await (await ServiceAsync(second)).TransferAsync(sender.User.Id, model);

            Assert.True(again.IsSuccess);
            Assert.True(again.Data!.IsExternal);
            Assert.Equal(900m, await app.BalanceAsync(sender.Standard.Id));
        }

        [Fact]
        public async Task Transfer_to_another_bank_is_limited_by_the_bank_rule()
        {
            var app = await TestApp.CreateAsync();
            await using (app)
            {
                var sender = await app.NewKitAsync(50000m);
                var external = await NewExternalCardAsync(app);
                await app.QueryAsync(async db =>
                {
                    var bank = await TestData.EnsureOwnBankAsync(db);
                    bank.MaxInterbankAmount = 1000m;
                    await db.SaveChangesAsync();
                    return 0;
                });

                using var scope = app.NewScope();
                var service = await ServiceAsync(scope);
                var tooMuch = await service.TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = external.CardNumber, Amount = 1000.01m });
                var allowed = await service.TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = external.CardNumber, Amount = 1000m });

                Assert.False(tooMuch.IsSuccess);
                Assert.Equal("You can send at most 1,000.00 AZN to another bank at once.", tooMuch.Errors[0]);
                Assert.True(allowed.IsSuccess);
            }
        }

        [Fact]
        public async Task Unknown_card_of_a_known_bank_names_the_bank_in_the_message()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(100m);
            await NewExternalCardAsync(app);

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = "5522090000000000", Amount = 5m });

            Assert.False(result.IsSuccess);
            Assert.Equal("We couldn't find a card with that number at ABB.", result.Errors[0]);
        }

        [Fact]
        public async Task Inactive_external_card_is_not_found()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(100m);
            var external = await NewExternalCardAsync(app);
            await app.QueryAsync(async db =>
            {
                await db.ExternalCards.Where(c => c.Id == external.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false));
                return 0;
            });

            using var scope = app.NewScope();
            var result = await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto { FromCardId = sender.Standard.Id, ToCardNumber = external.CardNumber, Amount = 5m });

            Assert.False(result.IsSuccess);
        }

        // ---------- Limitlər ----------

        [Fact]
        public async Task GetLimits_shows_the_used_amount_for_each_card()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(1000m);
            var silver = await app.NewCardAsync(kit.User.Id, CardTier.Silver, 0m);

            using var scope = app.NewScope();
            var service = await ServiceAsync(scope);
            await service.TransferAsync(kit.User.Id, new TransferDto { FromCardId = kit.Standard.Id, ToCardId = silver.Id, Amount = 120m, RequestId = NewRequest() });

            var limits = (await service.GetLimitsAsync(kit.User.Id)).ToList();

            var standard = Assert.Single(limits, l => l.CardId == kit.Standard.Id);
            Assert.Equal(120m, standard.UsedThisMonth);
            Assert.Equal(500m, standard.MonthlyLimit);
            Assert.Equal(1m, standard.CommissionPercent);
            Assert.Equal(0m, Assert.Single(limits, l => l.CardId == silver.Id).UsedThisMonth);
            Assert.Equal(3, limits.Count);
        }

        // ---------- Eyni anda gələn sorğular ----------

        [Fact]
        public async Task Parallel_transfers_from_one_card_never_overdraw_it()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(300m);
            var recipient = await app.NewKitAsync();

            // 10 sorğu x 100 AZN, balans 300 (komissiya 500-ə qədər yoxdur): ən çox 3-ü keçməlidir
            var tasks = Enumerable.Range(0, 10).Select(async _ =>
            {
                using var scope = app.NewScope();
                return await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto
                {
                    FromCardId = sender.Standard.Id,
                    ToCardNumber = recipient.Standard.CardNumber,
                    Amount = 100m,
                    RequestId = NewRequest()
                });
            }).ToList();
            var results = await Task.WhenAll(tasks);

            var succeeded = results.Count(r => r.IsSuccess);
            Assert.Equal(3, succeeded);
            Assert.Equal(0m, await app.BalanceAsync(sender.Standard.Id));
            Assert.Equal(300m, await app.BalanceAsync(recipient.Standard.Id));
        }

        [Fact]
        public async Task Parallel_requests_with_the_same_request_id_move_the_money_once()
        {
            var app = await TestApp.SharedAsync();
            var sender = await app.NewKitAsync(500m);
            var recipient = await app.NewKitAsync();
            var request = NewRequest();

            var tasks = Enumerable.Range(0, 6).Select(async _ =>
            {
                using var scope = app.NewScope();
                return await (await ServiceAsync(scope)).TransferAsync(sender.User.Id, new TransferDto
                {
                    FromCardId = sender.Standard.Id,
                    ToCardNumber = recipient.Standard.CardNumber,
                    Amount = 100m,
                    RequestId = request
                });
            }).ToList();
            var results = await Task.WhenAll(tasks);

            Assert.All(results, r => Assert.True(r.IsSuccess, string.Join(";", r.Errors)));
            Assert.Equal(400m, await app.BalanceAsync(sender.Standard.Id));
            Assert.Equal(100m, await app.BalanceAsync(recipient.Standard.Id));
        }
    }
}
