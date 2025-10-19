using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.EntityTypeExtension;

public sealed class ApprovalHistoryObjectExtension : ObjectTypeExtension<ApprovalHistory>
{
    protected override void Configure(IObjectTypeDescriptor<ApprovalHistory> descriptor)
    {
        descriptor.Field(x => x.Snapshot).Type<NonNullType<JsonType>>();
    }
}
