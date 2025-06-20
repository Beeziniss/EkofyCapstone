using AutoMapper;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.ServiceInterfaces.Track;
using EkofyApp.Domain.Entities;
using EkofyApp.Infrastructure.Services;
using HealthyNutritionApp.Application.Interfaces;
using HotChocolate.Data;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Reflection;

namespace EkofyApp.Api.GraphQL.Query.Track
{
    [ExtendObjectType(typeof(QueryInitialization))]
    public class TrackQuery(ITrackService trackService)
    {
        private readonly ITrackService _trackService = trackService;

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

        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IExecutable<Tracks> GetCustomTrackResponse([Service] IUnitOfWork unitOfWork, [Service] IMapper mapper)
        {
            return unitOfWork.GetCollection<Tracks>().AsExecutable();

        }

        //[UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<TrackResponse> GetCustomTrackResponseDto([Service] IUnitOfWork unitOfWork, [Service] IMapper mapper)
        {
            var queryable = unitOfWork.GetCollection<Tracks>().AsQueryable();
            return mapper.ProjectTo<TrackResponse>(queryable);

            // Method ProjectTo sử dụng Expression<Func<Tracks, TrackResponse>>
            // Nên HotChocolate không thể hiểu hoặc kiểm soát được projection này
            // Do đó [UseProjection] sẽ bị bỏ qua 
        }

        public async Task<TrackResponse> GetTrackByIdAsync(string id, IResolverContext context)
        {
            IReadOnlyList<string> selectedFields = GetSelectedFieldNames(context);
            ProjectionDefinition<Tracks> projection = BuildProjection<Tracks>(selectedFields);

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
}
