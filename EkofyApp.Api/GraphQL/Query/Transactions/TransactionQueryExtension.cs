using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Transactions;

public sealed class TransactionQueryExtension : ObjectTypeExtension<TransactionQuery>
{
    protected override void Configure(IObjectTypeDescriptor<TransactionQuery> descriptor)
    {
        descriptor.Field(x => x.GetTransactions())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting();
        //.AllowAnonymous();
    }
}
