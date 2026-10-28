using CMS.Application.Common.Abstractions;
using CMS.Domain.Content.ContentTypes;
using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Content;

// The aggregate validates the schema before repository tracking begins.
public sealed class CreateContentTypeHandler(IRepository<ContentType> repository, ICurrentTenant tenant, IIdGenerator ids)
    : IRequestHandler<CreateContentTypeCommand, Result<Guid>>
{
    // SQL enforces uniqueness if concurrent creators choose the same type key.
    public Task<Result<Guid>> Handle(CreateContentTypeCommand request, CancellationToken cancellationToken)
    {
        var result = ContentType.Create(ids.NewId(), tenant.TenantId ?? Guid.Empty, request.Key, request.Name, request.FieldsJson);
        if (result.IsFailure) return Task.FromResult(Result<Guid>.Failure(result.Error));
        repository.Add(result.Value);
        return Task.FromResult(Result<Guid>.Success(result.Value.Id));
    }
}
