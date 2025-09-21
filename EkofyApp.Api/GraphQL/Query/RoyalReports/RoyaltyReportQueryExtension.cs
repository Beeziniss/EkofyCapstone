using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.RoyalReports;

public sealed class RoyaltyReportQueryExtension : ObjectTypeExtension<RoyaltyReportQuery>
{
    protected override void Configure(IObjectTypeDescriptor<RoyaltyReportQuery> descriptor)
    {
        descriptor.Field(x => x.GetRoyaltyReports())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting<RoyaltyPolicy>();
        //.AllowAnonymous();
    }
}
