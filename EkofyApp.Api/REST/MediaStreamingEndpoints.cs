using EkofyApp.Application.ThirdPartyServiceInterfaces.AWS;
using Microsoft.AspNetCore.Mvc;

namespace EkofyApp.Api.REST;

public static class MediaStreamingEndpoints
{
    public static void MapMediaStreamingEndpoints(this IEndpointRouteBuilder app)
    {
        // Chỉ cần xác thực
        app.MapPost("/api/media-streaming/signed-token",
            (IAmazonCloudFrontService cloudFrontService, [FromBody] string trackId) =>
            {
                string token = cloudFrontService.GenerateHlsToken(trackId);
                return Results.Ok(new { Message = "Get signed token successfully", token });
            })
            .RequireAuthorization("AdminOnly");

        // Chỉ dành cho Admin
        app.MapGet("/api/media-streaming/keys",
            (IAmazonCloudFrontService cloudFrontService, string trackId, string token) =>
            {
                byte[] keyBytes = cloudFrontService.DecryptionKey(trackId, token);
                return Results.File(keyBytes, "application/octet-stream");
            })
            .RequireAuthorization("AdminOnly");

        // Yêu cầu người dùng có claim `can_access_track: true`
        app.MapGet("/api/media-streaming/cloudfront/{trackId}/master.m3u8",
            async (IAmazonCloudFrontService cloudFrontService, string trackId, string token) =>
            {
                string signedContent = await cloudFrontService.GetMasterPlaylistAsync(trackId, token);
                return Results.Content(signedContent, "application/vnd.apple.mpegurl");
            })
            .RequireAuthorization("TrackOwner");

        // Chỉ yêu cầu xác thực
        app.MapGet("/api/media-streaming/{trackId}/{bitrate}/playlist.m3u8",
            async (IAmazonCloudFrontService cloudFrontService, string trackId, string bitrate, string token) =>
            {
                string finalContent = await cloudFrontService.GetBitratePlaylistAsync(trackId, bitrate, token);
                return Results.Content(finalContent, "application/vnd.apple.mpegurl");
            })
            .RequireAuthorization();
    }
}
