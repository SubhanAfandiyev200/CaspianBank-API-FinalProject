using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Bank;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Globalization;

namespace Service.Services
{
    public class BankRulesService : IBankRulesService
    {
        private readonly IBaseRepository<OwnBank> _bank;
        private readonly IValidator<BankRulesDto> _validator;

        public BankRulesService(IBaseRepository<OwnBank> bank, IValidator<BankRulesDto> validator)
        {
            _bank = bank;
            _validator = validator;
        }

        public async Task<ServiceResult<BankRulesDto>> GetAsync()
        {
            var bank = await _bank.GetAsync();
            if (bank is null)
            {
                return ServiceResult<BankRulesDto>.Fail("The bank rules are not set up yet.");
            }
            return ServiceResult<BankRulesDto>.Ok(ToDto(bank));
        }

        public async Task<ServiceResult<(BankRulesDto Rules, string Changes)>> UpdateAsync(BankRulesDto model)
        {
            var validation = await _validator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<(BankRulesDto, string)>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var bank = await _bank.GetAsync();
            if (bank is null)
            {
                return ServiceResult<(BankRulesDto, string)>.Fail("The bank rules are not set up yet.");
            }

            var changes = new List<string>();
            Track(changes, "Reserve %", bank.ReservePercent, model.ReservePercent);
            Track(changes, "Profit tax %", bank.ProfitTaxPercent, model.ProfitTaxPercent);
            Track(changes, "Operating cost", bank.OperatingCostMonthly, model.OperatingCostMonthly);
            Track(changes, "Interbank max", bank.MaxInterbankAmount, model.MaxInterbankAmount);
            Track(changes, "Min loan", bank.MinLoanAmount, model.MinLoanAmount);
            Track(changes, "Max loan", bank.MaxLoanAmount, model.MaxLoanAmount);
            Track(changes, "Loan rate %", bank.AnnualInterestRate, model.AnnualInterestRate);
            Track(changes, "Fixed penalty", bank.FixedPenalty, model.FixedPenalty);
            Track(changes, "Daily penalty %", bank.DailyPenaltyPercent, model.DailyPenaltyPercent);
            Track(changes, "Open loans", bank.MaxActiveLoans, model.MaxActiveLoans);
            Track(changes, "Offer days", bank.OfferValidDays, model.OfferValidDays);

            bank.ReservePercent = model.ReservePercent;
            bank.ProfitTaxPercent = model.ProfitTaxPercent;
            bank.OperatingCostMonthly = model.OperatingCostMonthly;
            bank.MaxInterbankAmount = model.MaxInterbankAmount;
            bank.MinLoanAmount = model.MinLoanAmount;
            bank.MaxLoanAmount = model.MaxLoanAmount;
            bank.AnnualInterestRate = model.AnnualInterestRate;
            bank.FixedPenalty = model.FixedPenalty;
            bank.DailyPenaltyPercent = model.DailyPenaltyPercent;
            bank.MaxActiveLoans = model.MaxActiveLoans;
            bank.OfferValidDays = model.OfferValidDays;
            await _bank.UpdateAsync(bank);

            return ServiceResult<(BankRulesDto, string)>.Ok((ToDto(bank), changes.Count == 0 ? "No change" : string.Join("; ", changes)));
        }

        private static void Track(List<string> changes, string name, decimal before, decimal after)
        {
            if (before != after)
            {
                changes.Add(name + " " + before.ToString("0.##", CultureInfo.InvariantCulture) + " → " + after.ToString("0.##", CultureInfo.InvariantCulture));
            }
        }

        private static BankRulesDto ToDto(OwnBank bank)
        {
            return new BankRulesDto
            {
                ReservePercent = bank.ReservePercent,
                ProfitTaxPercent = bank.ProfitTaxPercent,
                OperatingCostMonthly = bank.OperatingCostMonthly,
                MaxInterbankAmount = bank.MaxInterbankAmount,
                MinLoanAmount = bank.MinLoanAmount,
                MaxLoanAmount = bank.MaxLoanAmount,
                AnnualInterestRate = bank.AnnualInterestRate,
                FixedPenalty = bank.FixedPenalty,
                DailyPenaltyPercent = bank.DailyPenaltyPercent,
                MaxActiveLoans = bank.MaxActiveLoans,
                OfferValidDays = bank.OfferValidDays
            };
        }
    }
}
