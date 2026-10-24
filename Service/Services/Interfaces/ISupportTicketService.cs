using Service.Helpers.DTOs.Support;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // Dəstək mesajları. userId və staffUserId HƏMİŞƏ tokendən götürülür. Müştəri yalnız öz mesajlarını görür və yazır
    public interface ISupportTicketService
    {
        // ---------- Müştəri ----------
        Task<IEnumerable<TicketListItemDto>> GetMineAsync(string userId);
        Task<ServiceResult<TicketDetailDto>> GetMineDetailAsync(string userId, int id);
        Task<ServiceResult<TicketDetailDto>> CreateAsync(string userId, CreateTicketDto model);

        // "Help" pəncərəsi: userId boşdursa qeydiyyatsız ziyarətçidir (model.Email məcburidir)
        Task<ServiceResult<HelpSentDto>> SendHelpAsync(string? userId, SendHelpDto model);
        Task<ServiceResult<TicketDetailDto>> CustomerReplyAsync(string userId, int id, ReplyTicketDto model);
        Task<ServiceResult<TicketDetailDto>> CustomerCloseAsync(string userId, int id);

        // ---------- İşçi ----------
        Task<ServiceResult<TicketPageDto>> GetPageAsync(TicketFilterDto filter);
        Task<ServiceResult<TicketDetailDto>> GetDetailAsync(int id);
        Task<ServiceResult<TicketDetailDto>> StaffReplyAsync(string staffUserId, int id, ReplyTicketDto model);
        Task<ServiceResult<TicketDetailDto>> StaffCloseAsync(string staffUserId, int id);
    }
}
