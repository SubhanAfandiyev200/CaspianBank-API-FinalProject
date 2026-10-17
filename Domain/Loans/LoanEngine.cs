using Domain.Entities;
using Domain.Enums;

namespace Domain.Loans
{
    // Kredit hesablamalarının hamısı bir yerdə (bazaya toxunmayan saf kod).
    // Servis, repository və seed eyni qaydanı istifadə edir, ona görə rəqəmlər hər yerdə eyni çıxır.
    //
    // Qaydalar:
    //  - annuitet: hər ay eyni ödəniş (əsas borc + faiz), faiz qalan borca görə hesablanır
    //  - ödəniş günü hər ayın son günü; ilk ödəniş pulun verildiyi aydan SONRAKI ayın son günüdür
    //  - cərimə: gecikmənin 1-ci günündən sabit məbləğ + hər gün ödənilməmiş məbləğin faizi (cərimənin üstünə cərimə gəlmir)
    //  - ödəniş sırası: əvvəl cərimə, sonra faiz, sonra əsas borc
    //  - hissəvi erkən ödənişdə qalan aylar eyni qalır, aylıq ödəniş azalır, faiz dərəcəsi də düşə bilər
    public static class LoanEngine
    {
        // ---------- Ümumi köməkçilər ----------

        public static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        // Bazada vaxt UTC saxlanılır, istifadəçi isə Bakı günü ilə düşünür (UTC+4, yay vaxtı yoxdur)
        public static DateTime BakuToday(DateTime utcNow)
        {
            return utcNow.AddHours(4).Date;
        }

        public static DateTime LastDayOfMonth(DateTime date)
        {
            return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
        }

        // İlk ödəniş: pulun verildiyi aydan sonrakı ayın son günü
        public static DateTime FirstDueDate(DateTime disbursedOn)
        {
            var nextMonth = new DateTime(disbursedOn.Year, disbursedOn.Month, 1).AddMonths(1);
            return LastDayOfMonth(nextMonth);
        }

        public static int AgeOn(DateTime birthDate, DateTime date)
        {
            var age = date.Year - birthDate.Year;
            if (birthDate.Date > date.Date.AddYears(-age))
            {
                age--;
            }
            return age;
        }

        // ---------- Annuitet ----------

        // (1 + aylıq faiz) ^ ay
        private static decimal Growth(decimal monthlyRate, int months)
        {
            var factor = 1m;
            for (var i = 0; i < months; i++)
            {
                factor *= 1m + monthlyRate;
            }
            return factor;
        }

        public static decimal MonthlyPayment(decimal principal, decimal annualRatePercent, int months)
        {
            if (months <= 0)
            {
                return 0m;
            }

            var monthlyRate = annualRatePercent / 12m / 100m;
            if (monthlyRate == 0m)
            {
                return Round(principal / months);
            }

            var growth = Growth(monthlyRate, months);
            return Round(principal * monthlyRate * growth / (growth - 1m));
        }

        // Aylıq ödənişə görə verilə biləcək ən çox əsas borc (MonthlyPayment-in tərsi)
        public static decimal PrincipalFromPayment(decimal payment, decimal annualRatePercent, int months)
        {
            if (months <= 0 || payment <= 0m)
            {
                return 0m;
            }

            var monthlyRate = annualRatePercent / 12m / 100m;
            if (monthlyRate == 0m)
            {
                return Round(payment * months);
            }

            var growth = Growth(monthlyRate, months);
            return Round(payment * (growth - 1m) / (monthlyRate * growth));
        }

        public static List<ScheduleLine> BuildSchedule(decimal principal, decimal annualRatePercent, int months, DateTime firstDueDate)
        {
            var lines = new List<ScheduleLine>();
            var payment = MonthlyPayment(principal, annualRatePercent, months);
            var monthlyRate = annualRatePercent / 12m / 100m;
            var balance = principal;

            for (var i = 1; i <= months; i++)
            {
                var interest = Round(balance * monthlyRate);
                decimal principalPart;
                if (i == months)
                {
                    // Son ay yuvarlaqlaşdırma fərqini özünə götürür: borc tam sıfıra enir
                    principalPart = balance;
                }
                else
                {
                    principalPart = Round(payment - interest);
                    if (principalPart > balance)
                    {
                        principalPart = balance;
                    }
                    if (principalPart < 0m)
                    {
                        principalPart = 0m;
                    }
                }

                balance = Round(balance - principalPart);
                lines.Add(new ScheduleLine
                {
                    Number = i,
                    DueDate = LastDayOfMonth(firstDueDate.AddMonths(i - 1)),
                    PrincipalAmount = principalPart,
                    InterestAmount = interest
                });
            }

            return lines;
        }

        public static LoanQuote Quote(decimal principal, decimal annualRatePercent, int months)
        {
            var lines = BuildSchedule(principal, annualRatePercent, months, FirstDueDate(DateTime.UtcNow));
            var total = Round(lines.Sum(l => l.PrincipalAmount + l.InterestAmount));
            return new LoanQuote
            {
                MonthlyPayment = MonthlyPayment(principal, annualRatePercent, months),
                TotalRepayment = total,
                TotalInterest = Round(total - principal)
            };
        }

        // ---------- Kredit məbləği qaydaları ----------

        // Başqa banklardakı gecikmələrin sayına görə maaş neçə qat götürülür: 0 gecikmə 24, 1-2 gecikmə 12, 3 və çox 4
        public static int DelayMultiplier(int delayCount)
        {
            if (delayCount <= 0)
            {
                return 24;
            }
            if (delayCount <= 2)
            {
                return 12;
            }
            return 4;
        }

        // Maaşa görə ən çox: min(bankın maksimumu, maaş x qat)
        public static decimal MaxByIncome(decimal monthlyIncome, int delayCount, decimal maxLoanAmount)
        {
            var byIncome = monthlyIncome * DelayMultiplier(delayCount);
            return Math.Floor(Math.Min(maxLoanAmount, byIncome));
        }

        // Aylıq ödənişlərin cəmi maaşın verilən faizini keçməsin: mövcud ödənişlər çıxıldıqdan sonra qalan limitə görə ən çox məbləğ
        public static decimal MaxByPaymentLimit(decimal monthlyIncome, decimal limitPercent, decimal existingMonthlyPayments,
                                                decimal annualRatePercent, int months)
        {
            var allowedPayment = monthlyIncome * limitPercent / 100m - existingMonthlyPayments;
            if (allowedPayment <= 0m)
            {
                return 0m;
            }
            return Math.Floor(PrincipalFromPayment(allowedPayment, annualRatePercent, months));
        }

        // ---------- Aylıq ödənişin vəziyyəti ----------

        public static decimal InterestLeft(LoanInstallment installment)
        {
            var paidToInterest = Math.Min(installment.PaidAmount, installment.InterestAmount);
            return installment.InterestAmount - paidToInterest;
        }

        public static decimal PrincipalLeft(LoanInstallment installment)
        {
            var paidToPrincipal = Math.Max(0m, installment.PaidAmount - installment.InterestAmount);
            return Math.Max(0m, installment.PrincipalAmount - paidToPrincipal);
        }

        public static decimal PenaltyLeft(LoanInstallment installment)
        {
            return Math.Max(0m, installment.FixedPenaltyAmount + installment.AccruedPenalty - installment.PaidPenalty);
        }

        // Ləğv olunub və ya heç nə qalmayıb
        public static bool IsSettled(LoanInstallment installment)
        {
            if (installment.IsCancelled)
            {
                return true;
            }
            return InterestLeft(installment) + PrincipalLeft(installment) + PenaltyLeft(installment) == 0m;
        }

        // Ödənilməmiş aylıq ödənişlər, sıra ilə
        private static List<LoanInstallment> Unsettled(Loan loan)
        {
            return loan.Installments
                .Where(i => !IsSettled(i))
                .OrderBy(i => i.Number)
                .ToList();
        }

        // ---------- Cərimə ----------

        // Gecikmiş aylıq ödənişlərə cərimə yazır (asOf günü daxil). Nəticə kreditin özündə saxlanılır:
        // oxuma zamanı izlənməyən surətdə çağırılırsa bazaya yazılmır, ödəniş və gündəlik iş zamanı isə yazılır.
        public static void AccrueAll(Loan loan, DateTime asOf)
        {
            var today = asOf.Date;

            foreach (var installment in loan.Installments)
            {
                if (installment.IsCancelled || today <= installment.DueDate.Date)
                {
                    continue;
                }

                var unpaid = InterestLeft(installment) + PrincipalLeft(installment);

                if (installment.PenaltyUpdatedAt is null)
                {
                    // Vaxtında ödənibsə (gecikmə yoxdur) cərimə də yoxdur
                    if (unpaid <= 0m)
                    {
                        continue;
                    }

                    installment.FixedPenaltyAmount = loan.FixedPenalty;
                    installment.PenaltyUpdatedAt = installment.DueDate.Date;
                }

                var from = installment.PenaltyUpdatedAt!.Value.Date;
                var days = (today - from).Days;
                if (days <= 0)
                {
                    continue;
                }

                if (unpaid > 0m)
                {
                    installment.AccruedPenalty = Round(installment.AccruedPenalty + unpaid * loan.DailyPenaltyPercent / 100m * days);
                }
                installment.PenaltyUpdatedAt = today;
            }
        }

        // ---------- İndi ödənilməli məbləğ ----------

        // Gecikmiş aylıq ödənişlər + növbəti ödəniş (cərimə ilə). AccrueAll əvvəlcə çağırılır
        public static LoanDue GetDue(Loan loan, DateTime asOf)
        {
            AccrueAll(loan, asOf);

            var today = asOf.Date;
            var due = new LoanDue();
            var upcomingTaken = false;

            foreach (var installment in Unsettled(loan))
            {
                var overdue = installment.DueDate.Date < today;
                if (!overdue)
                {
                    if (upcomingTaken)
                    {
                        break;
                    }
                    upcomingTaken = true;
                }

                due.Items.Add(new InstallmentDue
                {
                    Installment = installment,
                    Penalty = PenaltyLeft(installment),
                    Interest = InterestLeft(installment),
                    Principal = PrincipalLeft(installment),
                    IsOverdue = overdue,
                    DaysOverdue = overdue ? (today - installment.DueDate.Date).Days : 0
                });
            }

            due.PenaltyTotal = Round(due.Items.Sum(i => i.Penalty));
            due.InterestTotal = Round(due.Items.Sum(i => i.Interest));
            due.PrincipalTotal = Round(due.Items.Sum(i => i.Principal));
            due.Total = Round(due.PenaltyTotal + due.InterestTotal + due.PrincipalTotal);
            due.OverdueTotal = Round(due.Items.Where(i => i.IsOverdue).Sum(i => i.Total));
            due.OverdueCount = due.Items.Count(i => i.IsOverdue);
            return due;
        }

        // Aylıq ödənişləri ödə (hamısı birdən). Hər aylıq ödəniş üçün ayrıca qeyd qaytarır
        public static List<LoanPaymentRecord> ApplyInstallmentPayment(Loan loan, LoanDue due, DateTime asOf, string cardNumber, string reference)
        {
            var records = new List<LoanPaymentRecord>();
            foreach (var item in due.Items)
            {
                records.Add(PayInstallment(item, asOf, cardNumber, reference));
            }

            Refresh(loan, asOf);
            return records;
        }

        private static LoanPaymentRecord PayInstallment(InstallmentDue item, DateTime asOf, string cardNumber, string reference)
        {
            var installment = item.Installment;
            installment.PaidPenalty += item.Penalty;
            installment.PaidAmount += item.Interest + item.Principal;
            if (IsSettled(installment))
            {
                installment.FullyPaidAt = asOf.Date;
            }

            return new LoanPaymentRecord
            {
                LoanId = installment.LoanId,
                InstallmentId = installment.Id == 0 ? null : installment.Id,
                Type = LoanPaymentType.Installment,
                Amount = Round(item.Total),
                PenaltyPart = item.Penalty,
                InterestPart = item.Interest,
                PrincipalPart = item.Principal,
                PaidAt = asOf,
                CardNumber = cardNumber,
                TransactionReference = reference
            };
        }

        // ---------- Vaxtından əvvəl hissəvi ödəniş ----------

        // Uyğun deyilsə səbəbi (müştəriyə göstəriləcək mətn), uyğundursa null
        public static string? CheckPartialEarly(Loan loan, decimal amount, DateTime asOf, OwnBank bank)
        {
            if (loan.Status != LoanStatus.Active)
            {
                return "Only an active loan can be paid early.";
            }

            var due = GetDue(loan, asOf);
            if (due.HasOverdue)
            {
                return "Pay the overdue installments first.";
            }

            var open = Unsettled(loan);
            if (open.Count == 0)
            {
                return "This loan has nothing left to pay.";
            }
            if (open.Any(i => i.PaidAmount > 0m || i.PaidPenalty > 0m))
            {
                return "Pay the current installment first.";
            }

            if (amount < bank.MinEarlyPayment)
            {
                return "The minimum early payment is " + bank.MinEarlyPayment.ToString("N2", System.Globalization.CultureInfo.InvariantCulture) + " AZN.";
            }

            var balance = Round(open.Sum(i => PrincipalLeft(i)));
            if (amount >= balance)
            {
                return "This amount covers the whole remaining balance. Use the full repayment instead.";
            }

            return null;
        }

        // Əsas borcdan amount çıxılır, qalan aylar eyni qalır, aylıq ödəniş azalır. Əsas borcun kifayət qədər hissəsi ödənərsə faiz də düşür
        public static LoanPaymentRecord ApplyPartialEarly(Loan loan, decimal amount, DateTime asOf, string cardNumber, string reference, OwnBank bank)
        {
            var open = Unsettled(loan);
            var balance = Round(open.Sum(i => PrincipalLeft(i)));
            var newBalance = Round(balance - amount);

            var rate = loan.AnnualInterestRate;
            var newRate = rate;
            if (amount >= Round(balance * bank.EarlyRepaymentMinPercent / 100m) && rate > bank.MinAnnualInterestRate)
            {
                newRate = Math.Max(bank.MinAnnualInterestRate, rate - bank.EarlyRepaymentRateReduction);
            }

            var schedule = BuildSchedule(newBalance, newRate, open.Count, open[0].DueDate);
            for (var i = 0; i < open.Count; i++)
            {
                open[i].PrincipalAmount = schedule[i].PrincipalAmount;
                open[i].InterestAmount = schedule[i].InterestAmount;
            }

            loan.AnnualInterestRate = newRate;
            loan.MonthlyPayment = MonthlyPayment(newBalance, newRate, open.Count);
            loan.OutstandingPrincipal = newBalance;
            loan.TotalRepayment = Round(loan.Installments.Where(i => !i.IsCancelled).Sum(i => i.PrincipalAmount + i.InterestAmount));

            return new LoanPaymentRecord
            {
                LoanId = loan.Id,
                InstallmentId = null,
                Type = LoanPaymentType.PartialEarly,
                Amount = amount,
                PenaltyPart = 0m,
                InterestPart = 0m,
                PrincipalPart = amount,
                InterestRateAfter = newRate,
                PaidAt = asOf,
                CardNumber = cardNumber,
                TransactionReference = reference
            };
        }

        // ---------- Vaxtından əvvəl tam bağlama ----------

        // Gecikmiş və vaxtı çatmış aylıq ödənişlər tam (cərimə, faiz, əsas borc), gələcək aylar üçün yalnız əsas borc
        // və cari ayın keçən günlərinə düşən faiz (günlərə görə hissə-hissə)
        public static decimal GetPayoff(Loan loan, DateTime asOf)
        {
            AccrueAll(loan, asOf);

            var today = asOf.Date;
            var total = 0m;
            LoanInstallment? next = null;

            foreach (var installment in Unsettled(loan))
            {
                if (installment.DueDate.Date <= today)
                {
                    total += PenaltyLeft(installment) + InterestLeft(installment) + PrincipalLeft(installment);
                }
                else
                {
                    total += PrincipalLeft(installment);
                    if (next is null)
                    {
                        next = installment;
                    }
                }
            }

            if (next is not null)
            {
                total += AccruedInterestOfNext(loan, next, today);
            }

            return Round(total);
        }

        private static decimal AccruedInterestOfNext(Loan loan, LoanInstallment next, DateTime today)
        {
            var previous = loan.Installments.FirstOrDefault(i => i.Number == next.Number - 1);
            var periodStart = previous is not null
                ? previous.DueDate.Date
                : (loan.DisbursedAt?.Date ?? next.DueDate.Date.AddMonths(-1));

            var periodDays = (next.DueDate.Date - periodStart).Days;
            if (periodDays <= 0)
            {
                return 0m;
            }

            var elapsed = Math.Max(0, (today - periodStart).Days);
            var ratio = Math.Min(1m, (decimal)elapsed / periodDays);
            return Round(InterestLeft(next) * ratio);
        }

        public static List<LoanPaymentRecord> ApplyPayoff(Loan loan, DateTime asOf, string cardNumber, string reference)
        {
            AccrueAll(loan, asOf);

            var today = asOf.Date;
            var records = new List<LoanPaymentRecord>();
            var futurePrincipal = 0m;
            LoanInstallment? next = null;
            var future = new List<LoanInstallment>();

            foreach (var installment in Unsettled(loan))
            {
                if (installment.DueDate.Date <= today)
                {
                    var item = new InstallmentDue
                    {
                        Installment = installment,
                        Penalty = PenaltyLeft(installment),
                        Interest = InterestLeft(installment),
                        Principal = PrincipalLeft(installment),
                        IsOverdue = installment.DueDate.Date < today,
                        DaysOverdue = Math.Max(0, (today - installment.DueDate.Date).Days)
                    };
                    records.Add(PayInstallment(item, asOf, cardNumber, reference));
                }
                else
                {
                    futurePrincipal += PrincipalLeft(installment);
                    future.Add(installment);
                    if (next is null)
                    {
                        next = installment;
                    }
                }
            }

            var accruedInterest = next is null ? 0m : AccruedInterestOfNext(loan, next, today);

            if (future.Count > 0)
            {
                records.Add(new LoanPaymentRecord
                {
                    LoanId = loan.Id,
                    InstallmentId = null,
                    Type = LoanPaymentType.FullEarly,
                    Amount = Round(futurePrincipal + accruedInterest),
                    PenaltyPart = 0m,
                    InterestPart = Round(accruedInterest),
                    PrincipalPart = Round(futurePrincipal),
                    PaidAt = asOf,
                    CardNumber = cardNumber,
                    TransactionReference = reference
                });

                foreach (var installment in future)
                {
                    installment.IsCancelled = true;
                }
                loan.ClosedEarly = true;
            }

            Refresh(loan, asOf);
            return records;
        }

        // ---------- Kreditin vəziyyətinin yenilənməsi ----------

        // Qalan əsas borcu yeniləyir; hər şey ödənibsə krediti bağlayır
        public static void Refresh(Loan loan, DateTime asOf)
        {
            var live = loan.Installments.Where(i => !i.IsCancelled).ToList();
            loan.OutstandingPrincipal = Round(live.Sum(i => PrincipalLeft(i)));

            var finished = live.All(i => IsSettled(i));
            if (finished && (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Court))
            {
                loan.Status = LoanStatus.Closed;
                loan.ClosedAt = asOf;
            }
        }

        // courtAfterDays gün və ya daha çox gecikmiş ödəniş varsa kredit məhkəməyə keçir.
        // Gecikmə günü cərimə ilə eyni hesabla (Bakı günü) sayılır; asOf UTC andır (CourtAt kimi yazılır)
        public static bool MoveToCourtIfNeeded(Loan loan, DateTime asOf, int courtAfterDays)
        {
            if (loan.Status != LoanStatus.Active)
            {
                return false;
            }

            var today = BakuToday(asOf);
            foreach (var installment in Unsettled(loan))
            {
                var unpaid = InterestLeft(installment) + PrincipalLeft(installment);
                if (unpaid > 0m && (today - installment.DueDate.Date).Days >= courtAfterDays)
                {
                    loan.Status = LoanStatus.Court;
                    loan.CourtAt = asOf;
                    return true;
                }
            }

            return false;
        }

        // ---------- Kreditin aktivləşdirilməsi ----------

        // Təsdiq olunmuş məbləğ üçün aylıq ödənişləri yaradır; pul kartda artıq verilib (DisbursedAt = now)
        public static void Activate(Loan loan, DateTime now)
        {
            var principal = loan.ApprovedAmount ?? loan.RequestedAmount;
            var lines = BuildSchedule(principal, loan.AnnualInterestRate, loan.TermMonths, FirstDueDate(BakuToday(now)));

            loan.Installments.Clear();
            foreach (var line in lines)
            {
                loan.Installments.Add(new LoanInstallment
                {
                    Number = line.Number,
                    DueDate = line.DueDate,
                    PrincipalAmount = line.PrincipalAmount,
                    InterestAmount = line.InterestAmount
                });
            }

            loan.MonthlyPayment = MonthlyPayment(principal, loan.AnnualInterestRate, loan.TermMonths);
            loan.TotalRepayment = Round(lines.Sum(l => l.PrincipalAmount + l.InterestAmount));
            loan.OutstandingPrincipal = principal;
            loan.DisbursedAt = now;
            loan.Status = LoanStatus.Active;
        }
    }
}
