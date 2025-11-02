using EkofyApp.Api.GraphQL.Scalars;
using EkofyApp.Application.Models.Stripes;
using EkofyApp.Application.Models.Wavs;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Policies;
using EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
using EkofyApp.Application.ServiceInterfaces.Tracks;
using EkofyApp.Application.ThirdPartyServiceInterfaces.FFMPEG;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using MongoDB.Bson;

namespace EkofyApp.Api.GraphQL.Mutation.Test;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class TestMutation
{
    public TransferResponse TestTransferMoneyToArtist(string artistAccountId, decimal amount, [Service] IStripeService stripeService)
    {
        //decimal sgdAmount = HelperCurrencyConverter.ConvertVndToSgd(amount);
        //long stripeAmount = HelperCurrencyConverter.ConvertDecimalToStripeAmount(sgdAmount, "sgd");
        long stripeAmount = HelperCurrencyConverter.ConvertVndDecimalToStripeAmountSgdLong(amount);

        return stripeService.TransferToArtist(artistAccountId, stripeAmount, "aaaaa");
    }

    public async Task<bool> SeedMonthlyStreamCountByTrackIdAsync(string trackId, long streamCount, int month, int year, [Service] ITrackService trackService)
    {
        await trackService.SeedMonthlyStreamCountByTrackIdAsync(trackId, streamCount, month, year);
        return true;
    }

    public async Task<bool> TestGenrateMonthlyRoyaltyReportsAynsc(int month, int year, [Service] IRoyaltyReportService royaltyReportService)
    {
        await royaltyReportService.GenerateMonthlyRoyaltyReportsAsync(month, year);
        return true;
    }

    #region Không dùng
    //public async Task<string> UploadFileAsync(string fileName, IFile file, CancellationToken cancellationToken)
    //{
    //    // Folder custom
    //    string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");

    //    if (!Directory.Exists(folderPath))
    //    {
    //        Directory.CreateDirectory(folderPath);
    //    }

    //    using FileStream stream = File.Create(System.IO.Path.Combine(folderPath, $"{fileName}.png"));

    //    await file.OpenReadStream().CopyToAsync(stream, cancellationToken);
    //    await stream.FlushAsync(cancellationToken);

    //    return $"File {fileName}.png uploaded successfully to {folderPath}";
    //}

    //public async Task<WavFileResponse> ConvertToWavFileAsync(IFile file, [Service] IFfmpegService ffmpegService, CancellationToken cancellationToken)
    //{
    //    using Stream stream = file.OpenReadStream();

    //    return await ffmpegService.ConvertToWavAsync(stream, file.Name, AudioConvertPathOptions.ForConvertToWav());

    //}

    //public async Task<string> ConvertToHlsAsync(IFile file, [Service] IFfmpegService ffmpegService, CancellationToken cancellationToken)
    //{
    //    using Stream stream = file.OpenReadStream();

    //    WavFileResponse wavFileResponse = await ffmpegService.ConvertToWavAsync(stream, file.Name, AudioConvertPathOptions.ForConvertToWav());

    //    string trackIdTemp = ObjectId.GenerateNewId().ToString();

    //    return await ffmpegService.ConvertToHlsAsync(wavFileResponse, AudioConvertPathOptions.ForConvertToHls(trackIdTemp));
    //}

    //public async Task<bool> CreateEntilement([Service] IUnitOfWork unitOfWork, Domain.Enums.EntitlementValueType featureValueType, [GraphQLType(typeof(EntitlementValueScalar))] object value)
    //{
    //    await unitOfWork.GetCollection<Entitlement>().InsertOneAsync(new Entitlement
    //    {
    //        //UserId = ObjectId.GenerateNewId().ToString(),
    //        Name = "Test Entitlement",
    //        Code = "test_entitlement",
    //        Description = "This is a test entitlement",
    //        ValueType = featureValueType,
    //        Value = value,
    //        ExpiredAt = DateTime.UtcNow.AddDays(30)
    //    });
    //    return true;
    //}
    #endregion
}
