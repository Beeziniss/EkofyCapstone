using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Reports;

public sealed class ReportMutationExtension : ObjectTypeExtension<ReportMutation>
{
    protected override void Configure(IObjectTypeDescriptor<ReportMutation> descriptor)
    {
        descriptor.Field(t => t.CreateReportAsync(default!))
            .Authorize(HelperRoleBase.FullRolesArray);

        descriptor.Field(t => t.AssignReportToModeratorAsync(default!, default!))
            .Authorize(HelperRoleBase.ModeratorAdminRolesArray);

        descriptor.Field(t => t.ProcessReportAsync(default!))
            .Authorize(HelperRoleBase.ModeratorAdminRolesArray);

        descriptor.Field(t => t.RestoreUserAsync(default!))
            .Authorize(HelperRoleBase.ModeratorAdminRolesArray);

        descriptor.Field(t => t.RestoreContentAsync(default!))
            .Authorize(HelperRoleBase.ModeratorAdminRolesArray);
    }
}
