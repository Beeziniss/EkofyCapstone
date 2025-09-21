using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Query.Invoices;

public sealed class InvoiceQueryExtension : ObjectTypeExtension<InvoiceQuery>
{
    protected override void Configure(IObjectTypeDescriptor<InvoiceQuery> descriptor)
    {
        descriptor.Field(x => x.GetInvoices())
            .Authorize(roles: HelperRoleBase.FullRoles)
            .UseProjection()
            .UseFiltering()
            .UseSorting<Invoice>();
        //.AllowAnonymous();
    }
}
