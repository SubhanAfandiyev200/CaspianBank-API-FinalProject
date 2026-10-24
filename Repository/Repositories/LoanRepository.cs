using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Repository.Repositories.Interfaces;
using Repository.Results;

namespace Repository.Repositories
{
    public class LoanRepository : ILoanRepository
    {
        private const int MaxAttempts = 8;

        private readonly AppDbContext _dbContext;

        public LoanRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ---------- Oxuma ----------

        public async Task<IReadOnlyList<Loan>> GetByBorrowerAsync(string userId)
        {
            return await _dbContext.Loans
                .AsNoTracking()
                .Include(l => l.Installments)
                .Where(l => l.BorrowerUserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ThenByDescending(l => l.Id)
                .ToListAsync();
        }

        public async Task<Loan?> GetDetailForBorrowerAsync(int loanId, string userId)
        {
            return await _dbContext.Loans
                .AsNoTracking()
                .AsSplitQuery()
                .Include(l => l.Installments)
                .Include(l => l.PaymentRecords)
                .Include(l => l.TargetCard)
                .Include(l => l.OwnBank)
                .FirstOrDefaultAsync(l => l.Id == loanId && l.BorrowerUserId == userId);
        }

        public async Task<Loan?> GetDetailAsync(int loanId)
        {
            return await _dbContext.Loans
                .AsNoTracking()
                .AsSplitQuery()
                .Include(l => l.Borrower)
                .Include(l => l.Installments)
                .Include(l => l.PaymentRecords)
                .Include(l => l.TargetCard)
                .Include(l => l.OwnBank)
                .FirstOrDefaultAsync(l => l.Id == loanId);
        }

        public async Task<Loan?> GetByContractNumberAsync(string contractNumber)
        {
            return await _dbContext.Loans
                .AsNoTracking()
                .Include(l => l.Borrower)
                .Include(l => l.Installments)
                .Include(l => l.OwnBank)
                .FirstOrDefaultAsync(l => l.ContractNumber == contractNumber);
        }

        public async Task<int> CountOpenAsync(string userId)
        {
            return await _dbContext.Loans
                .AsNoTracking()
                .CountAsync(l => l.BorrowerUserId == userId
                                 && (l.Status == LoanStatus.Pending
                                     || l.Status == LoanStatus.AwaitingSuperAdmin
                                     || l.Status == LoanStatus.Offered
                                     || l.Status == LoanStatus.Active
                                     || l.Status == LoanStatus.Court));
        }

        public async Task<decimal> SumActiveMonthlyPaymentsAsync(string userId)
        {
            var total = await _dbContext.Loans
                .AsNoTracking()
                .Where(l => l.BorrowerUserId == userId && (l.Status == LoanStatus.Active || l.Status == LoanStatus.Court))
                .SumAsync(l => (decimal?)l.MonthlyPayment);

            return total ?? 0m;
        }

        public async Task<IReadOnlyList<Loan>> GetAllByBorrowerWithInstallmentsAsync(string userId)
        {
            return await _dbContext.Loans
                .AsNoTracking()
                .Include(l => l.Installments)
                .Where(l => l.BorrowerUserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<LoanReviewPage> GetReviewPageAsync(LoanStatus? status, string? search, int skip, int take)
        {
            var query = _dbContext.Loans
                .AsNoTracking()
                .Include(l => l.Borrower)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(l => l.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var text = search.Trim();
                query = query.Where(l => l.ContractNumber.Contains(text)
                                         || l.Borrower.Name.Contains(text)
                                         || l.Borrower.Surname.Contains(text)
                                         || (l.Borrower.FinKod != null && l.Borrower.FinKod.Contains(text)));
            }

            var total = await query.CountAsync();

            // Gözləyən müraciətlər əvvəldə (işçinin baxacağı), qalanlar yeni əvvəldə
            var items = await query
                .OrderBy(l => l.Status == LoanStatus.Pending || l.Status == LoanStatus.AwaitingSuperAdmin ? 0 : 1)
                .ThenByDescending(l => l.CreatedAt)
                .ThenByDescending(l => l.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return new LoanReviewPage
            {
                Items = items,
                TotalCount = total
            };
        }

        // ---------- Yazma ----------

        public async Task<Loan> CreateAsync(Loan loan, DateTime utcNow)
        {
            var year = LoanEngine.BakuToday(utcNow).Year;

            // Ödəniş kodunu eyni anda iki müraciət götürsə (unikal indeks) təzədən cəhd edilir
            for (var attempt = 0; attempt < 5; attempt++)
            {
                loan.ContractNumber = await NextContractNumberAsync(year);
                _dbContext.Loans.Add(loan);

                try
                {
                    await _dbContext.SaveChangesAsync();
                    return loan;
                }
                catch (DbUpdateException) when (attempt < 4)
                {
                    _dbContext.Entry(loan).State = EntityState.Detached;
                    await Task.Delay(Random.Shared.Next(10, 60));
                }
            }

            throw new InvalidOperationException("The loan could not be saved.");
        }

        // KR-2026-000123: ildəki ən böyük sıra nömrəsindən sonrakı. 100-dən başlayır (aşağı nömrələr əvvəlcədən toxumlanmış kreditlərə aiddir)
        private async Task<string> NextContractNumberAsync(int year)
        {
            var prefix = "KR-" + year + "-";
            var numbers = await _dbContext.Loans
                .AsNoTracking()
                .Where(l => l.ContractNumber.StartsWith(prefix))
                .Select(l => l.ContractNumber)
                .ToListAsync();

            var max = 0;
            foreach (var number in numbers)
            {
                if (int.TryParse(number.Substring(prefix.Length), out var value) && value > max)
                {
                    max = value;
                }
            }

            return prefix + Math.Max(max + 1, 100).ToString("D6");
        }

        public async Task<LoanResult> FirstApproveAsync(int loanId, string staffUserId, decimal approvedAmount, DateTime utcNow)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                _dbContext.ChangeTracker.Clear();

                var loan = await _dbContext.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
                if (loan is null)
                {
                    return LoanResult.Of(LoanOutcome.NotFound);
                }
                if (loan.Status != LoanStatus.Pending)
                {
                    return LoanResult.Of(LoanOutcome.WrongStatus, "This application has already been reviewed.");
                }

                // Pul düşmür: təsdiq məbləği saxlanır, son qərarı Super Admin verir
                loan.RequiresSuperAdminApproval = true;
                loan.FirstApprovedByUserId = staffUserId;
                loan.FirstApprovedAt = utcNow;
                loan.FirstApprovedAmount = approvedAmount;
                loan.Status = LoanStatus.AwaitingSuperAdmin;

                var outcome = await TrySaveAsync(loan);
                if (outcome == SaveOutcome.Saved)
                {
                    return Done(loan);
                }
            }

            _dbContext.ChangeTracker.Clear();
            return LoanResult.Of(LoanOutcome.Conflict);
        }

        public async Task<LoanResult> ApproveAsync(int loanId, string staffUserId, decimal approvedAmount, DateTime utcNow)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                _dbContext.ChangeTracker.Clear();

                var loan = await _dbContext.Loans.Include(l => l.Installments).FirstOrDefaultAsync(l => l.Id == loanId);
                if (loan is null)
                {
                    return LoanResult.Of(LoanOutcome.NotFound);
                }
                // Qara siyahıdakı kredit: kredit şöbəsi FirstApproveAsync ilə göndərir, Super Admin ya göndərilmişi, ya da birbaşa Pending krediti təsdiq edə bilər.
                // Kimin nə edə biləcəyini servis yoxlayır (LoanReviewService)
                if (loan.Status != LoanStatus.Pending && loan.Status != LoanStatus.AwaitingSuperAdmin)
                {
                    return LoanResult.Of(LoanOutcome.WrongStatus, "This application has already been reviewed.");
                }

                loan.ApprovedAmount = approvedAmount;
                loan.ReviewedByUserId = staffUserId;
                loan.ReviewedAt = utcNow;

                if (approvedAmount < loan.RequestedAmount)
                {
                    // Azaldılmış məbləğ: pul düşmür, müştəri razılaşmalıdır. Aylıq ödəniş təsdiq olunan məbləğə görə hesablanır
                    var bank = await _dbContext.OwnBanks.AsNoTracking().FirstAsync(b => b.Id == loan.OwnBankId);
                    var quote = LoanEngine.Quote(approvedAmount, loan.AnnualInterestRate, loan.TermMonths);
                    loan.MonthlyPayment = quote.MonthlyPayment;
                    loan.TotalRepayment = quote.TotalRepayment;
                    loan.OfferExpiresAt = utcNow.AddDays(bank.OfferValidDays);
                    loan.Status = LoanStatus.Offered;
                }
                else
                {
                    var failure = await DisburseAsync(loan, utcNow);
                    if (failure is not null)
                    {
                        _dbContext.ChangeTracker.Clear();
                        return failure;
                    }
                }

                var outcome = await TrySaveAsync(loan);
                if (outcome == SaveOutcome.Saved)
                {
                    return Done(loan);
                }
                if (outcome == SaveOutcome.Duplicate)
                {
                    return LoanResult.Of(LoanOutcome.WrongStatus, "This loan has already been paid out.");
                }
            }

            _dbContext.ChangeTracker.Clear();
            return LoanResult.Of(LoanOutcome.Conflict);
        }

        public async Task<LoanResult> RejectAsync(int loanId, string staffUserId, string reason, DateTime utcNow)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                _dbContext.ChangeTracker.Clear();

                var loan = await _dbContext.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
                if (loan is null)
                {
                    return LoanResult.Of(LoanOutcome.NotFound);
                }
                if (loan.Status != LoanStatus.Pending && loan.Status != LoanStatus.AwaitingSuperAdmin)
                {
                    return LoanResult.Of(LoanOutcome.WrongStatus, "This application has already been reviewed.");
                }

                loan.Status = LoanStatus.Rejected;
                loan.RejectReason = reason;
                loan.ReviewedByUserId = staffUserId;
                loan.ReviewedAt = utcNow;

                var outcome = await TrySaveAsync(loan);
                if (outcome == SaveOutcome.Saved)
                {
                    return Done(loan);
                }
            }

            _dbContext.ChangeTracker.Clear();
            return LoanResult.Of(LoanOutcome.Conflict);
        }

        public async Task<LoanResult> AcceptOfferAsync(int loanId, string userId, int? targetCardId, DateTime utcNow)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                _dbContext.ChangeTracker.Clear();

                var loan = await _dbContext.Loans.Include(l => l.Installments).FirstOrDefaultAsync(l => l.Id == loanId);

                // Başqasının krediti üçün "tapılmadı" deyilir (varlığı bilinməsin)
                if (loan is null || loan.BorrowerUserId != userId)
                {
                    return LoanResult.Of(LoanOutcome.NotFound);
                }
                if (loan.Status != LoanStatus.Offered)
                {
                    return LoanResult.Of(LoanOutcome.WrongStatus, "This offer is no longer open.");
                }

                if (loan.OfferExpiresAt is not null && loan.OfferExpiresAt < utcNow)
                {
                    loan.Status = LoanStatus.Declined;
                    loan.RejectReason = "The offer expired.";
                    await TrySaveAsync(loan);
                    return LoanResult.Of(LoanOutcome.OfferExpired, "This offer has expired.");
                }

                // Müştəri pulun düşəcəyi kartı razılaşma anında dəyişə bilər (öz kartı olmalıdır)
                if (targetCardId.HasValue && targetCardId.Value != loan.TargetCardId)
                {
                    var chosen = await _dbContext.Cards.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == targetCardId.Value && c.UserId == userId);
                    if (chosen is null)
                    {
                        return LoanResult.Of(LoanOutcome.TargetCardUnavailable, "Choose one of your own cards.");
                    }
                    loan.TargetCardId = chosen.Id;
                }

                loan.AcceptedAt = utcNow;

                var failure = await DisburseAsync(loan, utcNow);
                if (failure is not null)
                {
                    _dbContext.ChangeTracker.Clear();
                    return failure;
                }

                var outcome = await TrySaveAsync(loan);
                if (outcome == SaveOutcome.Saved)
                {
                    return Done(loan);
                }
                if (outcome == SaveOutcome.Duplicate)
                {
                    return LoanResult.Of(LoanOutcome.WrongStatus, "This loan has already been paid out.");
                }
            }

            _dbContext.ChangeTracker.Clear();
            return LoanResult.Of(LoanOutcome.Conflict);
        }

        public async Task<LoanResult> DeclineOfferAsync(int loanId, string userId, DateTime utcNow)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                _dbContext.ChangeTracker.Clear();

                var loan = await _dbContext.Loans.FirstOrDefaultAsync(l => l.Id == loanId);
                if (loan is null || loan.BorrowerUserId != userId)
                {
                    return LoanResult.Of(LoanOutcome.NotFound);
                }
                if (loan.Status != LoanStatus.Offered)
                {
                    return LoanResult.Of(LoanOutcome.WrongStatus, "This offer is no longer open.");
                }

                loan.Status = LoanStatus.Declined;
                loan.RejectReason = "The customer declined the offer.";

                var outcome = await TrySaveAsync(loan);
                if (outcome == SaveOutcome.Saved)
                {
                    return Done(loan);
                }
            }

            _dbContext.ChangeTracker.Clear();
            return LoanResult.Of(LoanOutcome.Conflict);
        }

        // Krediti aktivləşdirir (aylıq ödənişlər yaranır), məbləği pulun düşəcəyi karta əlavə edir və jurnal sətrini yazır.
        // Kart yararsızdırsa uğursuzluq nəticəsi qaytarır (heç nə dəyişmir), uğurlu olarsa null. Saxlama çağıran metodun işidir
        private async Task<LoanResult?> DisburseAsync(Loan loan, DateTime utcNow)
        {
            var card = await _dbContext.Cards.FirstOrDefaultAsync(c => c.Id == loan.TargetCardId && c.UserId == loan.BorrowerUserId);
            if (card is null || card.IsBlocked || card.Tier == CardTier.Cashback)
            {
                return LoanResult.Of(LoanOutcome.TargetCardUnavailable,
                    "The card chosen for the loan can't receive the money (it is missing, blocked or a Cashback card).");
            }

            var amount = loan.ApprovedAmount ?? loan.RequestedAmount;

            // Bankın xəzinəsi: kredit verildikdən sonra qalan vəsait məcburi ehtiyatdan az olmamalıdır (ehtiyat = müştəri kartları və depozitlərinin ReservePercent-i)
            var bank = await _dbContext.OwnBanks.AsNoTracking().FirstAsync(b => b.Id == loan.OwnBankId);
            var treasury = await _dbContext.TreasuryEntries.AsNoTracking().SumAsync(t => (decimal?)t.Amount) ?? 0m;
            var onCards = await _dbContext.Cards.AsNoTracking().SumAsync(c => (decimal?)c.Balance) ?? 0m;
            var inDeposits = await _dbContext.Deposits.AsNoTracking().Where(d => d.Status == DepositStatus.Active).SumAsync(d => (decimal?)d.Principal) ?? 0m;
            var reserve = Math.Round((onCards + inDeposits) * bank.ReservePercent / 100m, 2, MidpointRounding.AwayFromZero);
            if (treasury - amount < reserve)
            {
                return LoanResult.Of(LoanOutcome.TreasuryLow,
                    "The bank can't pay out this loan right now. Please try again later.");
            }

            LoanEngine.Activate(loan, utcNow);

            card.Balance += amount;
            _dbContext.Transactions.Add(new Transaction
            {
                CardId = card.Id,
                Type = TransactionType.LoanDisbursement,
                IsIncome = true,
                Amount = amount,
                BalanceAfter = card.Balance,
                Description = "Loan " + loan.ContractNumber + " paid out",
                Reference = "LD-" + loan.ContractNumber
            });

            // Kredit sətri də "dəyişdi" sayılsın ki, eyni anda iki əməliyyat (RowVersion) üst-üstə düşməsin
            _dbContext.Entry(loan).State = EntityState.Modified;
            return null;
        }

        // ---------- Ödəniş ----------

        public async Task<LoanResult> PayAsync(LoanPaymentPlan plan)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                _dbContext.ChangeTracker.Clear();
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                // Hər şey bu tranzaksiyada təzədən oxunur: servisdən gələn rəqəmlərə güvənmirik
                var loan = await _dbContext.Loans
                    .Include(l => l.Installments)
                    .Include(l => l.OwnBank)
                    .FirstOrDefaultAsync(l => l.Id == plan.LoanId);
                if (loan is null)
                {
                    return LoanResult.Of(LoanOutcome.NotFound);
                }

                if (plan.OwnerOnly && loan.BorrowerUserId != plan.PayerUserId)
                {
                    return LoanResult.Of(LoanOutcome.NotAllowed, "Only the borrower can make this payment.");
                }

                if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Court)
                {
                    return LoanResult.Of(LoanOutcome.WrongStatus, "This loan is not open for payments.");
                }

                // Ödəniş ödəyənin öz kartından çıxır (kredit başqasının olsa belə)
                var card = await _dbContext.Cards.FirstOrDefaultAsync(c => c.Id == plan.CardId && c.UserId == plan.PayerUserId);
                if (card is null)
                {
                    return LoanResult.Of(LoanOutcome.CardNotFound);
                }
                if (card.IsBlocked)
                {
                    return LoanResult.Of(LoanOutcome.CardBlocked);
                }

                var today = LoanEngine.BakuToday(plan.UtcNow);
                var records = new List<LoanPaymentRecord>();
                decimal total;
                decimal? newRate = null;

                switch (plan.Type)
                {
                    case LoanPaymentType.Installment:
                        var due = LoanEngine.GetDue(loan, today);
                        if (due.Items.Count == 0 || due.Total <= 0m)
                        {
                            return LoanResult.Of(LoanOutcome.NothingDue, "There is nothing to pay right now.");
                        }
                        // Məbləğ yazılıbsa və borcdan azdırsa hissə-hissə ödəniş: qalanı başqa kartdan ödənilə bilər
                        if (plan.Amount > due.Total)
                        {
                            return LoanResult.Of(LoanOutcome.NotPayable, "That is more than what is due now (" + due.Total.ToString("N2", System.Globalization.CultureInfo.InvariantCulture) + " AZN).");
                        }
                        total = plan.Amount > 0m ? plan.Amount : due.Total;
                        if (card.Balance < total)
                        {
                            return LoanResult.Of(LoanOutcome.InsufficientFunds);
                        }
                        records = LoanEngine.ApplyInstallmentPayment(loan, due, plan.UtcNow, card.CardNumber, plan.Reference, plan.Amount);
                        break;

                    case LoanPaymentType.PartialEarly:
                        var problem = LoanEngine.CheckPartialEarly(loan, plan.Amount, today, loan.OwnBank);
                        if (problem is not null)
                        {
                            return LoanResult.Of(LoanOutcome.NotPayable, problem);
                        }
                        total = plan.Amount;
                        if (card.Balance < total)
                        {
                            return LoanResult.Of(LoanOutcome.InsufficientFunds);
                        }
                        var record = LoanEngine.ApplyPartialEarly(loan, plan.Amount, plan.UtcNow, card.CardNumber, plan.Reference, loan.OwnBank);
                        records.Add(record);
                        newRate = record.InterestRateAfter;
                        break;

                    default:
                        total = LoanEngine.GetPayoff(loan, today);
                        if (total <= 0m)
                        {
                            return LoanResult.Of(LoanOutcome.NothingDue, "There is nothing to pay right now.");
                        }
                        if (card.Balance < total)
                        {
                            return LoanResult.Of(LoanOutcome.InsufficientFunds);
                        }
                        records = LoanEngine.ApplyPayoff(loan, plan.UtcNow, card.CardNumber, plan.Reference);
                        break;
                }

                card.Balance -= total;
                _dbContext.Transactions.Add(new Transaction
                {
                    CardId = card.Id,
                    Type = TransactionType.LoanPayment,
                    IsIncome = false,
                    Amount = total,
                    BalanceAfter = card.Balance,
                    Description = Describe(loan, plan.Type),
                    Reference = plan.Reference
                });
                _dbContext.LoanPaymentRecords.AddRange(records);
                _dbContext.Entry(loan).State = EntityState.Modified;

                try
                {
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new LoanResult
                    {
                        Outcome = LoanOutcome.Success,
                        LoanId = loan.Id,
                        ContractNumber = loan.ContractNumber,
                        Status = loan.Status,
                        Type = plan.Type,
                        Amount = total,
                        PenaltyPart = records.Sum(r => r.PenaltyPart),
                        InterestPart = records.Sum(r => r.InterestPart),
                        PrincipalPart = records.Sum(r => r.PrincipalPart),
                        CardBalanceAfter = card.Balance,
                        CardNumber = card.CardNumber,
                        Reference = plan.Reference,
                        LoanClosed = loan.Status == LoanStatus.Closed,
                        NewInterestRate = newRate,
                        PaidAt = plan.UtcNow
                    };
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();   // növbəti cəhd yenidən oxuyub təkrar yoxlayır
                    await Task.Delay(Random.Shared.Next(10, 60));
                }
                catch (DbUpdateException)
                {
                    // (Reference, Type) unikal indeksi: bu ödəniş artıq yazılıb
                    await transaction.RollbackAsync();
                    _dbContext.ChangeTracker.Clear();
                    return LoanResult.Of(LoanOutcome.Duplicate);
                }
            }

            _dbContext.ChangeTracker.Clear();
            return LoanResult.Of(LoanOutcome.Conflict);
        }

        public async Task<LoanResult?> FindPaymentAsync(string reference, string userId)
        {
            var row = await _dbContext.Transactions
                .AsNoTracking()
                .Include(t => t.Card)
                .FirstOrDefaultAsync(t => t.Reference == reference && t.Type == TransactionType.LoanPayment && t.Card.UserId == userId);
            if (row is null)
            {
                return null;
            }

            var records = await _dbContext.LoanPaymentRecords
                .AsNoTracking()
                .Where(r => r.TransactionReference == reference)
                .ToListAsync();
            if (records.Count == 0)
            {
                return null;
            }

            var loan = await _dbContext.Loans.AsNoTracking().FirstAsync(l => l.Id == records[0].LoanId);

            var type = LoanPaymentType.Installment;
            if (records.Any(r => r.Type == LoanPaymentType.FullEarly))
            {
                type = LoanPaymentType.FullEarly;
            }
            else if (records.Any(r => r.Type == LoanPaymentType.PartialEarly))
            {
                type = LoanPaymentType.PartialEarly;
            }

            return new LoanResult
            {
                Outcome = LoanOutcome.Success,
                LoanId = loan.Id,
                ContractNumber = loan.ContractNumber,
                Status = loan.Status,
                Type = type,
                Amount = row.Amount,
                PenaltyPart = records.Sum(r => r.PenaltyPart),
                InterestPart = records.Sum(r => r.InterestPart),
                PrincipalPart = records.Sum(r => r.PrincipalPart),
                CardBalanceAfter = row.BalanceAfter,
                CardNumber = row.Card.CardNumber,
                Reference = reference,
                LoanClosed = loan.Status == LoanStatus.Closed,
                NewInterestRate = records.Where(r => r.InterestRateAfter.HasValue).Select(r => r.InterestRateAfter).FirstOrDefault(),
                PaidAt = row.CreatedAt
            };
        }

        private static string Describe(Loan loan, LoanPaymentType type)
        {
            switch (type)
            {
                case LoanPaymentType.PartialEarly:
                    return "Early payment on loan " + loan.ContractNumber;
                case LoanPaymentType.FullEarly:
                    return "Loan " + loan.ContractNumber + " repaid in full";
                default:
                    return "Payment on loan " + loan.ContractNumber;
            }
        }

        // ---------- Gündəlik iş ----------

        // Ödənişə neçə gün qalmış xatırlatma bildirişi yazılır
        private const int DueSoonDays = 3;

        public async Task<LoanMaintenanceResult> RunMaintenanceAsync(DateTime utcNow)
        {
            _dbContext.ChangeTracker.Clear();
            var today = LoanEngine.BakuToday(utcNow);
            var notices = new List<LoanNotice>();

            // Vaxtı bitmiş təkliflər
            var expired = await _dbContext.Loans
                .Where(l => l.Status == LoanStatus.Offered && l.OfferExpiresAt != null && l.OfferExpiresAt < utcNow)
                .ToListAsync();
            foreach (var loan in expired)
            {
                loan.Status = LoanStatus.Declined;
                loan.RejectReason = "The offer expired.";
                notices.Add(new LoanNotice(LoanNoticeKind.OfferExpired, loan.BorrowerUserId, loan.Id, loan.ContractNumber,
                    "offer-expired:" + loan.Id, loan.ApprovedAmount ?? loan.RequestedAmount, null, 0));
            }

            // Açıq kreditlər: cərimə yaz, 90 gün gecikənləri Court-a keç
            var open = await _dbContext.Loans
                .Include(l => l.Installments)
                .Include(l => l.OwnBank)
                .Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Court)
                .ToListAsync();
            foreach (var loan in open)
            {
                LoanEngine.AccrueAll(loan, today);
                if (LoanEngine.MoveToCourtIfNeeded(loan, utcNow, loan.OwnBank.CourtAfterDays))
                {
                    notices.Add(new LoanNotice(LoanNoticeKind.Court, loan.BorrowerUserId, loan.Id, loan.ContractNumber,
                        "court:" + loan.Id, 0m, null, loan.OwnBank.CourtAfterDays));
                }

                // Müştəriyə deyiləcəklər: gecikmiş ödənişlər və tezliklə gələnlər (eyni hadisə təkrar yazılmır: servis Key ilə yoxlayır)
                foreach (var installment in loan.Installments.Where(i => !i.IsCancelled).OrderBy(i => i.Number))
                {
                    var unpaid = LoanEngine.InterestLeft(installment) + LoanEngine.PrincipalLeft(installment);
                    if (unpaid <= 0m)
                    {
                        continue;
                    }

                    var due = installment.DueDate.Date;
                    if (due < today)
                    {
                        notices.Add(new LoanNotice(LoanNoticeKind.Overdue, loan.BorrowerUserId, loan.Id, loan.ContractNumber,
                            "late:" + installment.Id, unpaid + LoanEngine.PenaltyLeft(installment), due, (today - due).Days));
                    }
                    else if (due <= today.AddDays(DueSoonDays))
                    {
                        notices.Add(new LoanNotice(LoanNoticeKind.DueSoon, loan.BorrowerUserId, loan.Id, loan.ContractNumber,
                            "due:" + installment.Id, unpaid, due, (due - today).Days));
                    }
                }

                // Aylıq ödənişi dəyişən kredit sətri də "dəyişdi" sayılır: eyni anda müştəri ödəniş edibsə (RowVersion) yazma uğursuz olur və təkrar baxılır
                _dbContext.ChangeTracker.DetectChanges();
                if (loan.Installments.Any(i => _dbContext.Entry(i).State == EntityState.Modified))
                {
                    _dbContext.Entry(loan).State = EntityState.Modified;
                }
            }

            _dbContext.ChangeTracker.DetectChanges();
            var changed = _dbContext.ChangeTracker.Entries().Count(e => e.State == EntityState.Modified && (e.Entity is Loan));

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Eyni anda müştəri ödəniş edib: növbəti işə düşmədə təkrar baxılacaq (bildirişlər də o zaman)
                _dbContext.ChangeTracker.Clear();
                return new LoanMaintenanceResult();
            }
            finally
            {
                _dbContext.ChangeTracker.Clear();
            }

            return new LoanMaintenanceResult
            {
                Changed = changed,
                Notices = notices
            };
        }

        // ---------- Köməkçilər ----------

        private enum SaveOutcome
        {
            Saved,
            Duplicate,
            Retry
        }

        private async Task<SaveOutcome> TrySaveAsync(Loan loan)
        {
            try
            {
                await _dbContext.SaveChangesAsync();
                return SaveOutcome.Saved;
            }
            catch (DbUpdateConcurrencyException)
            {
                await Task.Delay(Random.Shared.Next(10, 60));
                return SaveOutcome.Retry;
            }
            catch (DbUpdateException)
            {
                _dbContext.ChangeTracker.Clear();
                return SaveOutcome.Duplicate;
            }
        }

        private static LoanResult Done(Loan loan)
        {
            return new LoanResult
            {
                Outcome = LoanOutcome.Success,
                LoanId = loan.Id,
                ContractNumber = loan.ContractNumber,
                Status = loan.Status
            };
        }
    }
}
