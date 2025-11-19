namespace EkofyApp.Domain.Enums;

public enum PopularityActionType
{
    SkipStreaming,
    Streaming,
    CompleteStreaming,
    RepeatStreaming,

    Favorite,
    Unfavorite,
    Share,
    AddToPlaylist,
    RemoveFromPlaylist,
    Comment,

    Search,
    SearchResultClick,
    ClickFromRecommendation,

    Follow,
    Unfollow
}
