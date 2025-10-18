using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Recordings;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Recordings;
public sealed class RecordingService(IUnitOfWork unitOfWork) : IRecordingService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<Recording> GetRecordings()
    {
        return _unitOfWork.GetCollection<Recording>().AsQueryable();
    }

    public RecordingTempRequest CreateRecordingTemp(CreateRecordingRequest createRecordingRequest)
    {
        return new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Description = createRecordingRequest.Description,
            RecordingSplitRequests = createRecordingRequest.RecordingSplits,
        };
    }

    public async Task CreateRecordingAsync(CreateRecordingRequest createRecordingRequest, string trackId, CancellationToken cancellationToken = default)
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
            // Find the latest version for this track and update its status to Inactive
            Recording currentRecording = await _unitOfWork.GetCollection<Recording>()
                .Find(session, r => r.TrackId == trackId && r.Status == RecordingStatus.Active)
                .Project<Recording>(Builders<Recording>.Projection
                    .Include(r => r.Id)
                    .Include(r => r.Version))
                .SortByDescending(r => r.Version)
                .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundCustomException("Not found any recording");

            // Update current recording status to Inactive
            UpdateDefinition<Recording> updateDefinition = Builders<Recording>.Update.Set(r => r.Status, RecordingStatus.Inactive);
            UpdateResult updateResultVersionRecording = await _unitOfWork.GetCollection<Recording>().UpdateOneAsync(session,
                Builders<Recording>.Filter.Eq(r => r.Id, currentRecording.Id),
                updateDefinition,
                cancellationToken: cancellationToken);
            if (updateResultVersionRecording.MatchedCount == 0)
            {
                throw new NotFoundCustomException("Not found recording with given id");
            }
            if (updateResultVersionRecording.ModifiedCount == 0)
            {
                throw new ConflictCustomException("Failed to update the status of the current recording.");
            }

            // Create new recording
            Recording newRecording = new()
            {
                TrackId = trackId,
                Description = createRecordingRequest.Description,
                RecordingSplits = createRecordingRequest.RecordingSplits.Select(split => new RecordingSplit
                {
                    UserId = split.UserId,
                    ArtistRole = split.ArtistRole,
                    Percentage = split.Percentage
                }).ToList(),
                Version = ++currentRecording.Version,
                Status = RecordingStatus.Active,
            };

            await _unitOfWork.GetCollection<Recording>().InsertOneAsync(session, newRecording, cancellationToken: cancellationToken);
        });
    }
}
