namespace EkofyApp.Api.GraphQL.Mutation.Test
{
    [ExtendObjectType(typeof(MutationInitialization))]
    public class TestMutation
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
    }
}
