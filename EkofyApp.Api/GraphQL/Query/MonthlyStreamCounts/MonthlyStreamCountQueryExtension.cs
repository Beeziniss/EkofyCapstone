using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.MonthlyStreamCounts;

public sealed class MonthlyStreamCountQueryExtension : ObjectTypeExtension<MonthlyStreamCountQuery>
{
    protected override void Configure(IObjectTypeDescriptor<MonthlyStreamCountQuery> descriptor)
    {
        descriptor.Field(x => x.GetMonthlyStreamCounts())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting<MonthlyStreamCount>();
        //.AllowAnonymous();
    }
}
