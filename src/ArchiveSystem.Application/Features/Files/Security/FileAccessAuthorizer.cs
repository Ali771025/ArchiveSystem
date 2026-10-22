using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Repositories;
using SecurityAccessControlService = ArchiveSystem.Application.Common.Interfaces.Security.IAccessControlService;

namespace ArchiveSystem.Application.Features.Files.Security
{
    public sealed class FileAccessAuthorizer : IFileAccessAuthorizer
    {
        private readonly ICurrentUserContext _currentUser;
        private readonly SecurityAccessControlService _accessControlService;
        private readonly IAccessControlLookupRepository _lookupRepository;
        private readonly IClassificationRepository _classificationRepository;

        public FileAccessAuthorizer(
            ICurrentUserContext currentUser,
            SecurityAccessControlService accessControlService,
            IAccessControlLookupRepository lookupRepository,
            IClassificationRepository classificationRepository)
        {
            _currentUser = currentUser;
            _accessControlService = accessControlService;
            _lookupRepository = lookupRepository;
            _classificationRepository = classificationRepository;
        }

        public long CurrentUserId =>
            _currentUser.IsAuthenticated && _currentUser.UserId.HasValue
                ? _currentUser.UserId.Value
                : throw new ForbiddenAccessException("Authenticated user context is required.");

        public async Task EnsureCanViewFileAsync(long fileId, CancellationToken cancellationToken)
        {
            if (await IsAdministratorAsync(cancellationToken))
            {
                return;
            }

            if (!await _accessControlService.CanViewFileAsync(CurrentUserId, fileId, cancellationToken))
            {
                throw new ForbiddenAccessException("User is not allowed to view this file.");
            }
        }

        public async Task EnsureCanWriteFileAsync(long fileId, CancellationToken cancellationToken)
        {
            if (await IsAdministratorAsync(cancellationToken))
            {
                return;
            }

            var descriptor = await _lookupRepository.GetFileAccessDescriptorAsync(fileId, cancellationToken);
            if (descriptor is null)
            {
                throw new NotFoundException($"File '{fileId}' was not found.");
            }

            if (!descriptor.ClassificationId.HasValue ||
                !await _accessControlService.CanWriteClassificationAsync(
                    CurrentUserId,
                    descriptor.ClassificationId.Value,
                    cancellationToken))
            {
                throw new ForbiddenAccessException("User is not allowed to modify this file.");
            }
        }

        public async Task EnsureCanWriteSessionAsync(UploadSession session, CancellationToken cancellationToken)
        {
            if (await IsAdministratorAsync(cancellationToken) || !session.CaseId.HasValue)
            {
                return;
            }

            var classificationId = await _lookupRepository.GetClassificationIdByCaseIdAsync(
                session.CaseId.Value,
                cancellationToken);

            if (!classificationId.HasValue ||
                !await _accessControlService.CanWriteClassificationAsync(
                    CurrentUserId,
                    classificationId.Value,
                    cancellationToken))
            {
                throw new ForbiddenAccessException("User is not allowed to modify this upload session.");
            }
        }

        public async Task<long?> ResolveInitiateUploadClassificationAsync(
            long? caseId,
            long? classificationId,
            CancellationToken cancellationToken)
        {
            long? targetClassificationId = classificationId;

            if (caseId.HasValue)
            {
                var caseClassificationId = await _lookupRepository.GetClassificationIdByCaseIdAsync(
                    caseId.Value,
                    cancellationToken);

                if (!caseClassificationId.HasValue ||
                    !await CanUploadAsync(caseClassificationId.Value, cancellationToken))
                {
                    throw new ForbiddenAccessException("User is not allowed to upload to this case.");
                }

                if (targetClassificationId.HasValue && targetClassificationId.Value != caseClassificationId.Value)
                {
                    throw new ValidationException("Case classification does not match the provided classificationId.");
                }

                targetClassificationId = caseClassificationId.Value;
            }

            if (targetClassificationId.HasValue)
            {
                var classification = await _classificationRepository.GetByIdAsync(
                    targetClassificationId.Value,
                    cancellationToken);

                if (classification is null || !classification.IsActive)
                {
                    throw new NotFoundException($"Classification '{targetClassificationId.Value}' was not found.");
                }

                if (await _classificationRepository.HasChildrenAsync(targetClassificationId.Value, cancellationToken))
                {
                    throw new ValidationException("Documents can only be added to final sub-categories.");
                }

                if (!await CanUploadAsync(targetClassificationId.Value, cancellationToken))
                {
                    throw new ForbiddenAccessException("User is not allowed to upload to this classification.");
                }
            }

            if (!caseId.HasValue && targetClassificationId.HasValue)
            {
                throw new ValidationException("A caseId is required for document attachment.");
            }

            return targetClassificationId;
        }

        public async Task EnsureCanAssignTagsAsync(
            long fileId,
            IReadOnlyCollection<long> _,
            CancellationToken cancellationToken)
        {
            await EnsureCanWriteFileAsync(fileId, cancellationToken);
        }

        private async Task<bool> CanUploadAsync(long classificationId, CancellationToken cancellationToken)
        {
            return await IsAdministratorAsync(cancellationToken) ||
                   await _accessControlService.CanUploadFileVersionAsync(
                       CurrentUserId,
                       classificationId,
                       cancellationToken);
        }

        private async Task<bool> IsAdministratorAsync(CancellationToken cancellationToken)
        {
            return _currentUser.IsInRole("Admin") ||
                   _currentUser.IsInRole("Superuser") ||
                   await _accessControlService.HasGlobalPermissionAsync(CurrentUserId, "admin.access", cancellationToken);
        }
    }
}
