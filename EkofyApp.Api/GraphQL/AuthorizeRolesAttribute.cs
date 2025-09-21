using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using HotChocolate.Authorization;

namespace EkofyApp.Api.GraphQL;

public sealed class AuthorizeRolesAttribute : AuthorizeAttribute
{
    public AuthorizeRolesAttribute(string groupName)
    {
        Roles = groupName switch
        {
            // 4 roles
            HelperRoleBase.FullRoles => HelperRoleBase.FullRolesArray,

            // 3 roles
            HelperRoleBase.ListenerArtistModeratorRoles => HelperRoleBase.ListenerArtistModeratorRolesArray,
            HelperRoleBase.ListenerArtistAdminRoles => HelperRoleBase.ListenerArtistAdminRolesArray,
            HelperRoleBase.ListenerModeratorAdminRoles => HelperRoleBase.ListenerModeratorAdminRolesArray,
            HelperRoleBase.ArtistModeratorAdminRoles => HelperRoleBase.ArtistModeratorAdminRolesArray,

            // 2 roles
            HelperRoleBase.ListenerArtistRoles => HelperRoleBase.ListenerArtistRolesArray,
            HelperRoleBase.ListenerModeratorRoles => HelperRoleBase.ListenerModeratorRolesArray,
            HelperRoleBase.ListenerAdminRoles => HelperRoleBase.ListenerAdminRolesArray,
            HelperRoleBase.ArtistModeratorRoles => HelperRoleBase.ArtistModeratorRolesArray,
            HelperRoleBase.ArtistAdminRoles => HelperRoleBase.ArtistAdminRolesArray,
            HelperRoleBase.ModeratorAdminRoles => HelperRoleBase.ModeratorAdminRolesArray,

            // 1 role
            HelperRoleBase.ListenerRoles => HelperRoleBase.ListenerRolesArray,
            HelperRoleBase.ArtistRoles => HelperRoleBase.ArtistRolesArray,
            HelperRoleBase.ModeratorRoles => HelperRoleBase.ModeratorRolesArray,
            HelperRoleBase.AdminRoles => HelperRoleBase.AdminRolesArray,

            _ => throw new ArgumentNullCustomException($"Invalid role group name: {groupName}"),
        };
    }
}
