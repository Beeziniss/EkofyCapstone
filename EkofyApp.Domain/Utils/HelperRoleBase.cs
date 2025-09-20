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

    public static string[] FullRoles => fullRoles;

    // 3 roles
    public static string[] ListenerArtistModeratorRoles => listenerArtistModeratorRoles;
    public static string[] ListenerArtistAdminRoles => listenerArtistAdminRoles;
    public static string[] ListenerModeratorAdminRoles => listenerModeratorAdminRoles;
    public static string[] ArtistModeratorAdminRoles => artistModeratorAdminRoles;

    // 2 roles
    public static string[] ListenerArtistRoles => listenerArtistRoles;
    public static string[] ListenerModeratorRoles => listenerModeratorRoles;
    public static string[] ListenerAdminRoles => listenerAdminRoles;
    public static string[] ArtistModeratorRoles => artistModeratorRoles;
    public static string[] ArtistAdminRoles => artistAdminRoles;
    public static string[] ModeratorAdminRoles => moderatorAdminRoles;

    // 1 role
    public static string[] ListenerRoles => listenerRoles;
    public static string[] ArtistRoles => artistRoles;
    public static string[] ModeratorRoles => moderatorRoles;
    public static string[] AdminRoles => adminRoles;
}
