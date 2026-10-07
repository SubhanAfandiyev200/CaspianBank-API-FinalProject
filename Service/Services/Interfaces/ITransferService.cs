using Service.Helpers.DTOs.Transfers;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    public interface ITransferService
    {
        // Bütün qaydaları yoxlayır və komissiyanı hesablayır, amma pulu köçürmür
        Task<ServiceResult<TransferPreviewDto>> PreviewAsync(string userId, TransferDto model);

        // Eyni yoxlamalardan sonra pulu atomik köçürür
        Task<ServiceResult<TransferReceiptDto>> TransferAsync(string userId, TransferDto model);
    }
}
