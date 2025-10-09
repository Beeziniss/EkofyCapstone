using EkofyApp.Api.GraphQL.Scalars;
using EkofyApp.Application.Models.Wavs;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using MongoDB.Bson;

namespace EkofyApp.Api.GraphQL.Mutation.Test;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class TestMutation
{
    public async Task<string> UploadFileAsync(string fileName, IFile file, CancellationToken cancellationToken)
    {
        // Folder custom
        string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        using FileStream stream = File.Create(System.IO.Path.Combine(folderPath, $"{fileName}.png"));

        await file.OpenReadStream().CopyToAsync(stream, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        return $"File {fileName}.png uploaded successfully to {folderPath}";
    }

    public async Task<WavFileResponse> ConvertToWavFileAsync(IFile file, [Service] IFfmpegService ffmpegService, CancellationToken cancellationToken)
    {
        using Stream stream = file.OpenReadStream();

        return await ffmpegService.ConvertToWavAsync(stream, file.Name, AudioConvertPathOptions.ForConvertToWav());

    }

    public async Task<string> ConvertToHlsAsync(IFile file, [Service] IFfmpegService ffmpegService, CancellationToken cancellationToken)
    {
        using Stream stream = file.OpenReadStream();

        WavFileResponse wavFileResponse = await ffmpegService.ConvertToWavAsync(stream, file.Name, AudioConvertPathOptions.ForConvertToWav());

        string trackIdTemp = ObjectId.GenerateNewId().ToString();

        return await ffmpegService.ConvertToHlsAsync(wavFileResponse, AudioConvertPathOptions.ForConvertToHls(trackIdTemp));
    }

    public async Task<bool> CreateEntilement([Service] IUnitOfWork unitOfWork, Domain.Enums.EntitlementValueType featureValueType, [GraphQLType(typeof(EntitlementValueScalar))] object value)
    {
        await unitOfWork.GetCollection<Entitlement>().InsertOneAsync(new Entitlement
        {
            //UserId = ObjectId.GenerateNewId().ToString(),
            Name = "Test Entitlement",
            Code = "test_entitlement",
            Description = "This is a test entitlement",
            ValueType = featureValueType,
            Value = value,
            ExpiredAt = DateTime.UtcNow.AddDays(30)
        });
        return true;
    }
}
