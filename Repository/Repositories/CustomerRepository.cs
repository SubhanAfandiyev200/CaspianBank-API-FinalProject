using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;

        public CustomerRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<Customer?> GetByIdAsync(int id)
        {
            return _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public Task<Customer?> GetWithHistoryAsync(int id)
        {
            return _context.Customers
                .AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.Employments)
                    .ThenInclude(e => e.Workplace)
                .Include(c => c.ExternalLoans)
                    .ThenInclude(l => l.ExternalBank)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public Task<Customer?> GetByFinAndIdCardAsync(string fin, string idCardNumber)
        {
            return _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Fin == fin && c.IdCardNumber == idCardNumber);
        }
    }
}
