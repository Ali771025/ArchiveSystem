using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Taxpayers.DTOs;
using ArchiveSystem.Application.Features.Taxpayers.Interfaces;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using MediatR;

namespace ArchiveSystem.Application.Features.Taxpayers.Commands.CreateTaxpayer
{
    public sealed class CreateTaxpayerCommandHandler : IRequestHandler<CreateTaxpayerCommand, TaxpayerDetailsDto>
    {
        private readonly ITaxpayerRepository _taxpayerRepository;

        public CreateTaxpayerCommandHandler(ITaxpayerRepository taxpayerRepository)
        {
            _taxpayerRepository = taxpayerRepository;
        }

        public async Task<TaxpayerDetailsDto> Handle(CreateTaxpayerCommand request, CancellationToken cancellationToken)
        {
            var normalizedNationalId = request.TaxpayerType == TaxpayerType.NaturalPerson ? NormalizeRequired(request.NationalId) : null;
            var normalizedNationalCompanyId = request.TaxpayerType == TaxpayerType.LegalEntity ? NormalizeRequired(request.NationalCompanyId) : null;

            if (!string.IsNullOrWhiteSpace(normalizedNationalId) &&
                await _taxpayerRepository.ExistsByNationalIdAsync(normalizedNationalId, cancellationToken: cancellationToken))
            {
                throw new ConflictException($"کد/شناسه ملی «{normalizedNationalId}» قبلاً ثبت شده است.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedNationalCompanyId) &&
                await _taxpayerRepository.ExistsByNationalCompanyIdAsync(normalizedNationalCompanyId, cancellationToken: cancellationToken))
            {
                throw new ConflictException($"شناسه ملی «{normalizedNationalCompanyId}» قبلاً ثبت شده است.");
            }

            var normalizedMobile = NormalizeOptional(request.Mobile);
            if (!string.IsNullOrWhiteSpace(normalizedMobile) &&
                await _taxpayerRepository.ExistsByMobileAsync(normalizedMobile, cancellationToken: cancellationToken))
            {
                throw new ConflictException($"شماره همراه «{normalizedMobile}» قبلاً ثبت شده است.");
            }

            var taxpayer = new Taxpayer(
                Guid.NewGuid(),
                request.TaxpayerType,
                request.TaxpayerType == TaxpayerType.NaturalPerson ? request.FirstName : null,
                request.TaxpayerType == TaxpayerType.NaturalPerson ? request.LastName : null,
                normalizedNationalId,
                request.TaxpayerType == TaxpayerType.LegalEntity ? request.CompanyName : null,
                normalizedNationalCompanyId,
                request.TaxpayerType == TaxpayerType.NaturalPerson ? request.IdentityNumber : null,
                normalizedMobile,
                request.LandlinePhone,
                request.PostalCode,
                request.Address);

            await _taxpayerRepository.AddAsync(taxpayer, cancellationToken);
            await _taxpayerRepository.SaveChangesAsync(cancellationToken);

            return new TaxpayerDetailsDto(
                taxpayer.Id,
                taxpayer.TaxpayerType,
                taxpayer.FirstName,
                taxpayer.LastName,
                taxpayer.NationalId,
                taxpayer.CompanyName,
                taxpayer.NationalCompanyId,
                taxpayer.IdentityNumber,
                taxpayer.Mobile,
                taxpayer.LandlinePhone,
                taxpayer.PostalCode,
                taxpayer.Address,
                taxpayer.CreatedAt,
                taxpayer.ModifiedAt);
        }

        private static string? NormalizeOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static string NormalizeRequired(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }
    }
}
