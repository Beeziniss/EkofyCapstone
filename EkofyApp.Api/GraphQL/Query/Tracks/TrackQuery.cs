using AutoMapper;
using EkofyApp.Api.GraphQL.Resolver;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Domain.Entities;
using HealthyNutritionApp.Application.Interfaces;
using HotChocolate.Data;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Reflection;

namespace EkofyApp.Api.GraphQL.Query.Tracks;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public class TrackQuery(ITrackService trackService, ITrackGraphQLService trackGraphQLService)
{
    private readonly ITrackService _trackService = trackService;
    private readonly ITrackGraphQLService _trackGraphQLService = trackGraphQLService;

    public async Task<IEnumerable<TrackResponse>> GetTracksAsync()
    {
        return await _trackService.GetTracksAsync();
    }

    #region Original
    //public async Task<TrackResponse> GetTrackByIdAsync(string id, IResolverContext context, [Service] IUnitOfWork unitOfWork, [Service] IMapper mapper)
    //{
    //    IReadOnlyList<string> selectedFields = GetSelectedFieldNames(context);
    //    ProjectionDefinition<Tracks> projection = BuildProjection<Tracks>(selectedFields);

    //    Tracks tracks = await unitOfWork.GetCollection<Tracks>()
    //        .Find(x => x.Id == id)
    //        .Project<Tracks>(projection)
    //        .FirstOrDefaultAsync();

    //    return mapper.Map<TrackResponse>(tracks);
    //}
    #endregion

    public IExecutable<Track> GetCustomTrackResponse([Service] IUnitOfWork unitOfWork, [Service] IMapper mapper)
    {
        return unitOfWork.GetCollection<Track>().AsExecutable();
    }

    public IQueryable<TrackResponse> GetCustomTrackResponseDto()
    {
        return _trackGraphQLService.GetTracksExecutable();
    }

    public async Task<TrackResponse> GetTrackByIdAsync(string id, IResolverContext context)
    {
        IReadOnlyList<string> selectedFields = GetSelectedFieldNames(context);
        ProjectionDefinition<Track> projection = BuildProjection<Track>(selectedFields);

        return await _trackService.GetTrackResolverContext(projection, id);
    }

    public IReadOnlyList<string> GetSelectedFieldNames(IResolverContext context)
    {
        return context.Selection.SyntaxNode.SelectionSet?.Selections
            .OfType<FieldNode>()
            .Select(f => f.Name.Value)
            .Distinct()
            .ToList()
            ?? [];
    }

    public ProjectionDefinition<T> BuildProjection<T>(IEnumerable<string> fields)
    {
        ProjectionDefinitionBuilder<T> builder = Builders<T>.Projection;
        ProjectionDefinition<T> projection = builder.Include("_id"); // luôn cần _id

        foreach (string field in fields)
        {
            // Lấy thông tin property từ class T (ignore case: "name" → "Name")
            PropertyInfo? propInfo = typeof(T).GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propInfo != null)
            {
                BsonElementAttribute? bsonElement = propInfo.GetCustomAttribute<BsonElementAttribute>();
                string fieldName = bsonElement?.ElementName ?? propInfo.Name;

                projection = projection.Include(fieldName);
            }
        }

        return projection;
    }

}
