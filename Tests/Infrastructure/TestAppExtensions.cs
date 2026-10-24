using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository.Data;

namespace CaspianBank.Tests.Infrastructure
{
    // Yeni scope (yeni DbContext) ilə bazadan təzə oxuyan köməkçilər: servisin öz context-indəki köhnə məlumata baxmamaq üçün
    public static class TestAppExtensions
    {
        public static async Task<T> QueryAsync<T>(this TestApp app, Func<AppDbContext, Task<T>> query)
        {
            using var scope = app.NewScope();
            return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
        }

        public static async Task<decimal> BalanceAsync(this TestApp app, int cardId)
        {
            return await app.QueryAsync(db => db.Cards.AsNoTracking().Where(c => c.Id == cardId).Select(c => c.Balance).FirstAsync());
        }

        public static async Task<List<Transaction>> TransactionsAsync(this TestApp app, int cardId)
        {
            return await app.QueryAsync(db => db.Transactions.AsNoTracking().Where(t => t.CardId == cardId).OrderBy(t => t.Id).ToListAsync());
        }

        public static async Task<List<Notification>> NotificationsAsync(this TestApp app, string userId)
        {
            return await app.QueryAsync(db => db.Notifications.AsNoTracking().Where(n => n.UserId == userId).OrderBy(n => n.Id).ToListAsync());
        }

        public static async Task<List<AuditLog>> AuditAsync(this TestApp app, string action)
        {
            return await app.QueryAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.Action == action).OrderBy(a => a.Id).ToListAsync());
        }

        public static async Task SetBalanceAsync(this TestApp app, int cardId, decimal balance)
        {
            await app.QueryAsync(async db =>
            {
                await db.Cards.Where(c => c.Id == cardId).ExecuteUpdateAsync(s => s.SetProperty(c => c.Balance, balance));
                return 0;
            });
        }

        // Yeni müştəri: bir scope-da yaradılır, sonra scope bağlanır
        public static async Task<CustomerKit> NewKitAsync(this TestApp app, decimal standardBalance = 0m, string name = "Test", string surname = "Customer",
                                                          DateTime? birthDate = null, string? email = null)
        {
            using var scope = app.NewScope();
            return await TestData.NewCustomerKitAsync(scope.ServiceProvider, standardBalance, name, surname, birthDate, email);
        }

        public static async Task<Card> NewCardAsync(this TestApp app, string userId, CardTier tier = CardTier.Standard, decimal balance = 0m, bool blocked = false)
        {
            using var scope = app.NewScope();
            return await TestData.NewCardAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), userId, tier, balance, blocked);
        }
    }
}
