using dotenv.net;

namespace EkofyApp.Api
{
    public static class EnvironmentVariableLoader
    {
        public static void LoadEnvironmentVariable()
        {
            string envFilePath = "";
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
            {
                envFilePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), ".env");
            }
            else if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                envFilePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), ".env.development");
            }

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
