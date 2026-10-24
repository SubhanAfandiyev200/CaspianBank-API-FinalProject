using CaspianBank.Tests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Helpers.DTOs.Cards;
using Service.Services.Interfaces;
using Xunit;

namespace CaspianBank.Tests.Services
{
    // Top-up pulu yalnız həqiqi kartdan götürür: Caspian kartından (ondan çıxır) və ya bazadakı başqa bank kartından (çıxmır)
    public class TopUpFromCardTests
    {
        private static TopUpDto FromCard(Card source, decimal amount, string? expiry = null)
        {
            return new TopUpDto { Amount = amount, SourceCardNumber = source.CardNumber, Expiry = expiry ?? source.ExpiryDate.ToString("MM/yy"), Cvv = "123" };
        }

        private static async Task<Service.Helpers.Responses.ServiceResult<CardDto>> TopUpAsync(TestApp app, CustomerKit receiver, TopUpDto model)
        {
            using var scope = app.NewScope();
            return await scope.ServiceProvider.GetRequiredService<ICardService>().TopUpAsync(receiver.User.Id, receiver.Standard.Id, model);
        }

        [Fact]
        public async Task TopUp_from_a_Caspian_card_takes_the_money_from_it_and_writes_both_rows()
        {
            var app = await TestApp.SharedAsync();
            var payer = await app.NewKitAsync(500m);
            var receiver = await app.NewKitAsync(10m);

            var result = await TopUpAsync(app, receiver, FromCard(payer.Standard, 120m));

            Assert.True(result.IsSuccess);
            Assert.Equal(130m, await app.BalanceAsync(receiver.Standard.Id));
            Assert.Equal(380m, await app.BalanceAsync(payer.Standard.Id));

            var outRow = Assert.Single(await app.TransactionsAsync(payer.Standard.Id));
            Assert.Equal(TransactionType.TopUpOut, outRow.Type);
            Assert.False(outRow.IsIncome);
            Assert.Equal(380m, outRow.BalanceAfter);
            Assert.EndsWith(receiver.Standard.CardNumber[^4..], outRow.Description);

            var inRow = Assert.Single(await app.TransactionsAsync(receiver.Standard.Id));
            Assert.Equal(TransactionType.TopUp, inRow.Type);
            Assert.True(inRow.IsIncome);
            Assert.EndsWith(payer.Standard.CardNumber[^4..], inRow.Description);
            Assert.Equal(outRow.Reference, inRow.Reference);
        }

        [Fact]
        public async Task TopUp_from_a_Caspian_card_without_enough_money_changes_nothing()
        {
            var app = await TestApp.SharedAsync();
            var payer = await app.NewKitAsync(50m);
            var receiver = await app.NewKitAsync(10m);

            var result = await TopUpAsync(app, receiver, FromCard(payer.Standard, 50.01m));

            Assert.False(result.IsSuccess);
            Assert.Equal("That card doesn't have enough balance for this payment.", result.Errors[0]);
            Assert.Equal(50m, await app.BalanceAsync(payer.Standard.Id));
            Assert.Equal(10m, await app.BalanceAsync(receiver.Standard.Id));
            Assert.Empty(await app.TransactionsAsync(payer.Standard.Id));
        }

        [Fact]
        public async Task TopUp_of_the_exact_balance_empties_the_source_card()
        {
            var app = await TestApp.SharedAsync();
            var payer = await app.NewKitAsync(75.25m);
            var receiver = await app.NewKitAsync(0m);

            var result = await TopUpAsync(app, receiver, FromCard(payer.Standard, 75.25m));

            Assert.True(result.IsSuccess);
            Assert.Equal(0m, await app.BalanceAsync(payer.Standard.Id));
            Assert.Equal(75.25m, await app.BalanceAsync(receiver.Standard.Id));
        }

        [Fact]
        public async Task TopUp_from_a_Caspian_card_with_the_wrong_expiry_is_refused()
        {
            var app = await TestApp.SharedAsync();
            var payer = await app.NewKitAsync(500m);
            var receiver = await app.NewKitAsync(10m);

            var result = await TopUpAsync(app, receiver, FromCard(payer.Standard, 10m, DateTime.UtcNow.AddYears(9).ToString("MM/yy")));

            Assert.False(result.IsSuccess);
            Assert.Equal("The card details are not correct.", result.Errors[0]);
            Assert.Equal(500m, await app.BalanceAsync(payer.Standard.Id));
        }

        [Fact]
        public async Task TopUp_from_a_blocked_Caspian_card_is_refused()
        {
            var app = await TestApp.SharedAsync();
            var payer = await app.NewKitAsync(500m);
            var receiver = await app.NewKitAsync(10m);
            await app.QueryAsync(async db =>
            {
                var card = await db.Cards.FirstAsync(c => c.Id == payer.Standard.Id);
                card.IsBlocked = true;
                await db.SaveChangesAsync();
                return 0;
            });

            var result = await TopUpAsync(app, receiver, FromCard(payer.Standard, 10m));

            Assert.False(result.IsSuccess);
            Assert.Equal("That card is blocked. Choose another card.", result.Errors[0]);
            Assert.Equal(500m, await app.BalanceAsync(payer.Standard.Id));
        }

        [Fact]
        public async Task TopUp_from_the_same_card_is_refused()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(500m);

            var result = await TopUpAsync(app, kit, FromCard(kit.Standard, 10m));

            Assert.False(result.IsSuccess);
            Assert.Equal("Choose a different card from the one you're topping up.", result.Errors[0]);
            Assert.Equal(500m, await app.BalanceAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task TopUp_from_a_Cashback_card_works_like_any_Caspian_card()
        {
            var app = await TestApp.SharedAsync();
            var payer = await app.NewKitAsync(0m);
            await app.QueryAsync(async db =>
            {
                var card = await db.Cards.FirstAsync(c => c.Id == payer.Cashback.Id);
                card.Balance = 40m;
                await db.SaveChangesAsync();
                return 0;
            });
            var receiver = await app.NewKitAsync(0m);

            var result = await TopUpAsync(app, receiver, FromCard(payer.Cashback, 15m));

            Assert.True(result.IsSuccess);
            Assert.Equal(25m, await app.BalanceAsync(payer.Cashback.Id));
            Assert.Equal(15m, await app.BalanceAsync(receiver.Standard.Id));
        }

        [Fact]
        public async Task TopUp_from_a_card_that_does_not_exist_is_refused()
        {
            var app = await TestApp.SharedAsync();
            var kit = await app.NewKitAsync(10m);
            // Luhn-dan keçən, amma nə Caspian, nə başqa bank kartı olmayan nömrə
            var model = new TopUpDto { Amount = 10m, SourceCardNumber = "5555 5555 5555 4444", Expiry = DateTime.UtcNow.AddYears(2).ToString("MM/yy"), Cvv = "123" };

            var result = await TopUpAsync(app, kit, model);

            Assert.False(result.IsSuccess);
            Assert.Equal("We couldn't find a card with that number.", result.Errors[0]);
            Assert.Equal(10m, await app.BalanceAsync(kit.Standard.Id));
            Assert.Empty(await app.TransactionsAsync(kit.Standard.Id));
        }

        [Fact]
        public async Task The_owner_of_the_source_card_gets_a_notification()
        {
            var app = await TestApp.SharedAsync();
            var payer = await app.NewKitAsync(500m);
            var receiver = await app.NewKitAsync(10m);

            await TopUpAsync(app, receiver, FromCard(payer.Standard, 20m));

            var count = await app.QueryAsync(db => db.Notifications.AsNoTracking().CountAsync(n => n.UserId == payer.User.Id && n.Title == "Money left your card"));
            Assert.Equal(1, count);
        }
    }
}
