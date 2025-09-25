using EkofyApp.Application.Models.Works;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Works;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Works;
public sealed class WorkService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IWorkService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public IQueryable<Work> GetWorksQueryable()
    {
        return _unitOfWork.GetCollection<Work>().AsQueryable();
    }

    public WorkTempRequest CreateWorkTemp(CreateWorkRequest createWorkRequest)
    {
        return new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Description = createWorkRequest.Description,
            WorkSplits = createWorkRequest.WorkSplits,
        };
    }

    public async Task CreateWorkAsync(CreateWorkRequest createWorkRequest, string trackId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(trackId))
        {
            throw new BadRequestCustomException("TrackId cannot be null or empty.");
        }

        // Chỉ được tạo recording mới trước 3 ngày trước khi qua tháng mới
        // FE lo
        DateTimeOffset now = HelperMethod.GetUtcPlus7TimeOffset();
        DateTimeOffset lastDayOfMonth = new(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59, now.Offset);
        DateTimeOffset threeDaysBeforeEndOfMonth = lastDayOfMonth.AddDays(-3);
        if (now > threeDaysBeforeEndOfMonth)
        {
            throw new InvalidOperationException("Cannot create a new recording within the last 3 days of the month.");
        }

        // Check for duplicate users
        // Fe lo
        //var duplicateUsers = splits.GroupBy(s => s.UserId)
        //    .Where(g => g.Count() > 1)
        //    .Select(g => g.Key);

        //if (duplicateUsers.Any())
        //    throw new ArgumentException($"Duplicate users found in recording splits: {string.Join(", ", duplicateUsers)}");

        await _unitOfWork.ExecuteInTransactionAsync(async session =>
        {
            Work currentWork = await _unitOfWork.GetCollection<Work>()
                .Find(session, x => x.TrackId == trackId && x.Status == WorkStatus.Active)
                .Project<Work>(Builders<Work>.Projection
                    .Include(x => x.Id)
                    .Include(x => x.Version))
                .SortByDescending(x => x.Version)
                .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundCustomException("Not found any active work for this track");

            // Cập nhật trạng thái của work hiện tại thành Inactive
            UpdateDefinition<Work> updateCurrent = Builders<Work>.Update.Set(w => w.Status, WorkStatus.Inactive);
            UpdateResult updateResult = await _unitOfWork.GetCollection<Work>().UpdateOneAsync(session, w => w.Id == currentWork.Id, updateCurrent, cancellationToken: cancellationToken);
            if(updateResult.MatchedCount == 0)
            {
                throw new Exception("Not found work with given id");
            }
            if (updateResult.ModifiedCount == 0)
            {
                throw new Exception("Failed to update the current work status.");
            }

            // Tạo work mới với version mới
            Work newWork = new()
            {
                TrackId = trackId,
                Description = createWorkRequest.Description,
                WorkSplits = createWorkRequest.WorkSplits.Select(split => new WorkSplit
                {
                    UserId = split.UserId,
                    ArtistRole = split.ArtistRole,
                    Percentage = split.Percentage,
                }).ToList(),
                Version = ++currentWork.Version,
                Status = WorkStatus.Active,
            };

            await _unitOfWork.GetCollection<Work>().InsertOneAsync(session, newWork, cancellationToken: cancellationToken);
        });
    }
}
