using CloudinaryDotNet.Actions;
using EkofyApp.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.Cloudinary
{
    public interface ICloudinaryService
    {
        public ImageUploadResult UploadImage(IFormFile imageFile, ImageTag imageTag, string rootFolder = "Image");
    }
}
