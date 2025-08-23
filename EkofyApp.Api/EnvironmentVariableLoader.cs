using dotenv.net;

namespace EkofyApp.Api
{
    public static class EnvironmentVariableLoader
    {
        public static void LoadEnvironmentVariable()
        {
            // xử lý việc lấy file biến môi trường dựa theo environment
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            string envFileName = environment.Equals("Development", StringComparison.OrdinalIgnoreCase) ? ".env.Development" : ".env";

            // Xây dựng đường dẫn đầy đủ tới file .env nằm trong thư mục "4. Application"
            string envFilePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), envFileName);

            // Tải file .env từ đường dẫn cụ thể bằng DotEnvOptions
            DotEnvOptions options = new(
                // Truyền đường dẫn tới file .env
                envFilePaths: [envFilePath],
                // Không cần thăm dò .env vì đang chỉ định đường dẫn
                probeForEnv: false
            );

            // Load file .env
            DotEnv.Load(options);
        }
    }
}
