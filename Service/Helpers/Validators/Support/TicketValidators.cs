using FluentValidation;
using Service.Helpers.DTOs.Support;

namespace Service.Helpers.Validators.Support
{
    public class CreateTicketDtoValidator : AbstractValidator<CreateTicketDto>
    {
        public CreateTicketDtoValidator()
        {
            RuleFor(x => x.Subject)
                .NotEmpty().WithMessage("Write a subject.")
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length >= 3).WithMessage("The subject must be at least 3 characters.")
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 120).WithMessage("The subject can be at most 120 characters.");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Write your message.")
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length >= 5).WithMessage("The message must be at least 5 characters.")
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 2000).WithMessage("The message can be at most 2000 characters.");
        }
    }

    public class ReplyTicketDtoValidator : AbstractValidator<ReplyTicketDto>
    {
        public ReplyTicketDtoValidator()
        {
            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Write your message.")
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length >= 2).WithMessage("The message is too short.")
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 2000).WithMessage("The message can be at most 2000 characters.");
        }
    }

    public class SendHelpDtoValidator : AbstractValidator<SendHelpDto>
    {
        public SendHelpDtoValidator()
        {
            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Write your message.")
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length >= 5).WithMessage("The message must be at least 5 characters.")
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 2000).WithMessage("The message can be at most 2000 characters.");

            // Email yalnız qeydiyyatsız ziyarətçidə məcburidir (servis yoxlayır); yazılıbsa düzgün olmalıdır
            RuleFor(x => x.Email)
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 200).WithMessage("The email can be at most 200 characters.")
                .Must(value => string.IsNullOrWhiteSpace(value) || new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(value.Trim())).WithMessage("Enter a valid email address.");
        }
    }
}
