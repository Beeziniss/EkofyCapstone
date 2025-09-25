using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Domain.Utils;
public static class HelperRoleBase
{
    private static readonly string listenerRole = UserRole.Listener.ToString();
    private static readonly string artistRole = UserRole.Artist.ToString();
    private static readonly string moderatorRole = UserRole.Moderator.ToString();
    private static readonly string adminRole = UserRole.Admin.ToString();

    #region 4 roles
    private static readonly string[] fullRoles = [listenerRole, artistRole, moderatorRole, adminRole];
    #endregion

    #region 3 roles
    private static readonly string[] listenerArtistModeratorRoles = [listenerRole, artistRole, moderatorRole];
    private static readonly string[] listenerArtistAdminRoles = [listenerRole, artistRole, adminRole];
    private static readonly string[] listenerModeratorAdminRoles = [listenerRole, moderatorRole, adminRole];
    private static readonly string[] artistModeratorAdminRoles = [artistRole, moderatorRole, adminRole];
    #endregion

    #region 2 roles
    private static readonly string[] listenerArtistRoles = [listenerRole, artistRole];
    private static readonly string[] listenerModeratorRoles = [listenerRole, moderatorRole];
    private static readonly string[] listenerAdminRoles = [listenerRole, adminRole];
    private static readonly string[] artistModeratorRoles = [artistRole, moderatorRole];
    private static readonly string[] artistAdminRoles = [artistRole, adminRole];
    private static readonly string[] moderatorAdminRoles = [moderatorRole, adminRole];
    #endregion

    #region 1 role
    private static readonly string[] listenerRoles = [listenerRole];
    private static readonly string[] artistRoles = [artistRole];
    private static readonly string[] moderatorRoles = [moderatorRole];
    private static readonly string[] adminRoles = [adminRole];
    #endregion

    public static string[] FullRolesArray => fullRoles;

    // 3 roles
    public static string[] ListenerArtistModeratorRolesArray => listenerArtistModeratorRoles;
    public static string[] ListenerArtistAdminRolesArray => listenerArtistAdminRoles;
    public static string[] ListenerModeratorAdminRolesArray => listenerModeratorAdminRoles;
    public static string[] ArtistModeratorAdminRolesArray => artistModeratorAdminRoles;

    // 2 roles
    public static string[] ListenerArtistRolesArray => listenerArtistRoles;
    public static string[] ListenerModeratorRolesArray => listenerModeratorRoles;
    public static string[] ListenerAdminRolesArray => listenerAdminRoles;
    public static string[] ArtistModeratorRolesArray => artistModeratorRoles;
    public static string[] ArtistAdminRolesArray => artistAdminRoles;
    public static string[] ModeratorAdminRolesArray => moderatorAdminRoles;

    // 1 role
    public static string[] ListenerRolesArray => listenerRoles;
    public static string[] ArtistRolesArray => artistRoles;
    public static string[] ModeratorRolesArray => moderatorRoles;
    public static string[] AdminRolesArray => adminRoles;

    #region Role group names
    // 4 roles
    public const string FullRoles = "FullRoles";

    // 3 roles
    public const string ListenerArtistModeratorRoles = "ListenerArtistModeratorRoles";
    public const string ListenerArtistAdminRoles = "ListenerArtistAdminRoles";
    public const string ListenerModeratorAdminRoles = "ListenerModeratorAdminRoles";
    public const string ArtistModeratorAdminRoles = "ArtistModeratorAdminRoles";

    // 2 roles
    public const string ListenerArtistRoles = "ListenerArtistRoles";
    public const string ListenerModeratorRoles = "ListenerModeratorRoles";
    public const string ListenerAdminRoles = "ListenerAdminRoles";
    public const string ArtistModeratorRoles = "ArtistModeratorRoles";
    public const string ArtistAdminRoles = "ArtistAdminRoles";
    public const string ModeratorAdminRoles = "ModeratorAdminRoles";

    // 1 role
    public const string ListenerRoles = "ListenerRoles";
    public const string ArtistRoles = "ArtistRoles";
    public const string ModeratorRoles = "ModeratorRoles";
    public const string AdminRoles = "AdminRoles";
    #endregion
}
