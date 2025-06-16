using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Cloudinary;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;

namespace EkofyApp.Infrastructure.ThirdPartyServices.Cloudinaries
{
    public class CloudinaryService(Cloudinary cloudinary, IHttpContextAccessor httpContextAccessor) : IDisposable, ICloudinaryService
    {
        private bool disposedValue;

        private readonly Cloudinary _cloudinary = cloudinary;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        #region UploadImage
        public ImageUploadResult UploadImage(IFormFile imageFile, ImageTag imageTag, string rootFolder = "Image")
        {
            // UserId lấy từ phiên người dùng có thể là FE hoặc BE
            //string userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("Your session is limit, you must login again to edit profile!");
            string userId = ObjectId.GenerateNewId().ToString();

            if (imageFile is null || imageFile.Length == 0)
            {
                throw new ArgumentNullException(nameof(imageFile), "No file uploaded");
            }

            #region Kiểm tra bằng đuôi file (.ext)
            //// Lấy đuôi file (AudioFile Extension)
            //string? fileExtension = Path.GetExtension(imageFile.FileName).ToLower().TrimStart('.');

            //// Kiểm tra nếu phần mở rộng có tồn tại trong enum ImageExtension
            //if (!System.Enum.TryParse(fileExtension, true, out ImageExtension _))
            //{
            //    throw new BadRequestCustomException("Unsupported file type");
            //}
            #endregion

            // Kiểm tra bằng content-type (image/webp)
            string fileType = imageFile.ContentType.Split('/').First();
            if (fileType != "image")
            {
                throw new BadImageFormatException("Unsupported file type");
            }

            using Stream? stream = imageFile.OpenReadStream();

            string currentFolder = $"{rootFolder}/{imageTag}";

            // Hashing Metadata
            string hashedData = DataEncryptionExtensions.Encrypt($"image_{imageTag}_{userId}_{DateTime.UtcNow}");

            // Khởi tạo các thông số cần thiết
            ImageUploadParams imageUploadParams = new()
            {
                AssetFolder = currentFolder,
                File = new FileDescription(imageFile.FileName, stream),
                PublicId = $"{Uri.EscapeDataString(hashedData)}",
                DisplayName = imageFile.FileName,
                UniqueFilename = false, // Đã custom nên không cần Unique từ Server nữa
                Tags = imageTag.ToString(),
                Format = "webp", // Chuyển đổi sang định dạng webp
                // Transformation = new Transformation() // Nếu cần chỉnh kích thước ảnh
            };

            // Kết quả Response
            ImageUploadResult? uploadResult = _cloudinary.Upload(imageUploadParams);

            if ((int)uploadResult.StatusCode != StatusCodes.Status200OK)
            {
                throw new Exception(uploadResult.Error.Message);
            }

            //Console.WriteLine(uploadResult.JsonObj);

            return uploadResult;
        }
        #endregion

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~CloudinaryService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
