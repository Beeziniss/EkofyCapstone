namespace EkofyApp.Api.GraphQL.Query.MonthlyStreamCounts;

public sealed class MonthlyStreamCountQueryExtension : ObjectTypeExtension<MonthlyStreamCountQuery>
{
    protected override void Configure(IObjectTypeDescriptor<MonthlyStreamCountQuery> descriptor)
    {
        descriptor.Field(x => x.GetMonthlyStreamCounts())
            .Authorize(roles: ["Listener", "Artist", "Moderator", "Admin"]);
        //.AllowAnonymous();
    }
}
