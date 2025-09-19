namespace EkofyApp.Api.GraphQL.Query.RoyalReports;

public sealed class RoyaltyReportQueryExtension : ObjectTypeExtension<RoyaltyReportQuery>
{
    protected override void Configure(IObjectTypeDescriptor<RoyaltyReportQuery> descriptor)
    {
        descriptor.Field(x => x.GetRoyaltyReports())
            .Authorize(roles: ["Artist", "Moderator", "Admin"]);
        //.AllowAnonymous();
    }
}
