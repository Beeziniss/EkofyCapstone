namespace EkofyApp.Api.GraphQL.SubscriptionQL.Tracks;

[ExtendObjectType(typeof(SubscriptionInitialization))]
//[SubscriptionType]
public sealed class TrackSubscription
{
    //public ValueTask<ISourceStream<long>> SubscribeToFavoriteCount(ITopicEventReceiver receiver)
    //{
    //    return receiver.SubscribeAsync<long>("FavoriteCountTopic");
    //}

    [Subscribe]
    [Topic($"{{{nameof(trackId)}}}")]
    public long OnFavoriteCountUpdated(string trackId, [EventMessage] long favoriteCount)
    {
        return favoriteCount;
    }
}
