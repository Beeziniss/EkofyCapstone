using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Listeners;

public sealed class ListenerQueryExtension : ObjectTypeExtension<ListenerQuery>
{
    protected override void Configure(IObjectTypeDescriptor<ListenerQuery> descriptor)
    {
        descriptor.Field(x => x.GetListeners())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting();
        //.AllowAnonymous();
    }
}
