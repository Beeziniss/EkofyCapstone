namespace EkofyApp.Domain.Enums;

public enum PopularityActionType
{
    SkipStreaming,
    Streaming,
    CompleteStreaming,
    RepeatStreaming,

    // Track
    Favorite,
    Unfavorite,
    Share,
    AddToPlaylist,
    RemoveFromPlaylist,
    Comment,

    // Searching
    Search,
    SearchResultClick,
    ClickFromRecommendation,

    // Artist
    Follow,
    Unfollow
}
