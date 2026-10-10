using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.CardTiers;
using Service.Helpers.DTOs.Cards;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class CardTierService : ICardTierService
    {
        private readonly ICardTierConfigRepository _repo;
        private readonly ICardDesignRepository _designRepo;
        private readonly IValidator<CardTierUpdateDto> _updateValidator;

        public CardTierService(ICardTierConfigRepository repo,
                               ICardDesignRepository designRepo,
                               IValidator<CardTierUpdateDto> updateValidator)
        {
            _repo = repo;
            _designRepo = designRepo;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<CardTierDto>> GetAllAsync()
        {
            var configs = await _repo.GetAllWithDesignAsync();
            return configs.Select(c => new CardTierDto
            {
                Tier = c.Tier.ToString(),
                IssueFee = c.IssueFee,
                CashbackPercent = c.CashbackPercent,
                TransferLimit = c.TransferLimit,
                CommissionPercent = c.CommissionPercent,
                DesignImage = c.CardDesign.Image
            }).ToList();
        }

        // Admin siyahısı: növ sırası ilə (Cashback, Standard, Silver, Gold)
        public async Task<IEnumerable<CardTierAdminDto>> GetAllAdminAsync()
        {
            var configs = await _repo.GetAllWithDesignAsync();
            return configs.Select(ToAdminDto).ToList();
        }

        public async Task<CardTierAdminDto> GetDetailAsync(int id)
        {
            var configs = await _repo.GetAllWithDesignAsync();
            var config = configs.FirstOrDefault(c => c.Id == id);
            if (config is null) throw new NotFoundException();
            return ToAdminDto(config);
        }

        public async Task UpdateAsync(int id, CardTierUpdateDto model)
        {
            await _updateValidator.EnsureValidAsync(model);

            var config = await _repo.GetByIdAsync(id);
            if (config is null) throw new NotFoundException();

            var design = await _designRepo.GetByIdAsync(model.CardDesignId!.Value);
            if (design is null)
            {
                throw new BadRequestException("Choose an existing card design.");
            }

            // Modeldəki dəyərlər tapılan obyektə köçürülməsə, UpdateAsync köhnə dəyərləri yenidən yazar
            config.IssueFee = model.IssueFee!.Value;
            config.CashbackPercent = model.CashbackPercent!.Value;
            config.TransferLimit = model.TransferLimit!.Value;
            config.CommissionPercent = model.CommissionPercent!.Value;
            config.CardDesignId = design.Id;
            await _repo.UpdateAsync(config);
        }

        private static CardTierAdminDto ToAdminDto(CardTierConfig config)
        {
            return new CardTierAdminDto
            {
                Id = config.Id,
                Tier = config.Tier.ToString(),
                IssueFee = config.IssueFee,
                CashbackPercent = config.CashbackPercent,
                TransferLimit = config.TransferLimit,
                CommissionPercent = config.CommissionPercent,
                CardDesignId = config.CardDesignId,
                DesignTitle = config.CardDesign.Title,
                DesignImage = config.CardDesign.Image
            };
        }
    }
}
