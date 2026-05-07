using FluentValidation;
using FinancialAPI.Application.DTOs.Payment;
using FinancialAPI.Domain.Entities;

namespace FinancialAPI.Application.Validators;

/// <summary>
/// FluentValidation rules for InitiatePaymentRequest.
/// Runs before the service layer — returns 400 with field-level errors.
/// </summary>
public class InitiatePaymentValidator : AbstractValidator<InitiatePaymentRequest>
{
    public InitiatePaymentValidator()
    {
        RuleFor(x => x.SenderAccountId)
            .NotEmpty().WithMessage("Sender account ID is required.")
            .MaximumLength(100);

        RuleFor(x => x.SenderName)
            .NotEmpty().WithMessage("Sender name is required.")
            .MaximumLength(200);

        RuleFor(x => x.BeneficiaryId)
            .NotEmpty().WithMessage("Beneficiary ID is required.");

        RuleFor(x => x.BeneficiaryAccountNumber)
            .NotEmpty().WithMessage("Beneficiary account number is required.")
            .MaximumLength(50);

        RuleFor(x => x.BeneficiaryBankCode)
            .NotEmpty().WithMessage("SWIFT/BIC code is required.")
            .Must(Beneficiary.IsValidSwiftBic)
            .WithMessage("Invalid SWIFT/BIC code. Must be 8 or 11 alphanumeric characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.")
            .LessThanOrEqualTo(10_000_000).WithMessage("Amount exceeds maximum allowed limit.");

        RuleFor(x => x.Currency)
            .IsInEnum().WithMessage("Invalid currency code.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid payment type.");

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
