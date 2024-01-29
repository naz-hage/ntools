using Ntools;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace NbuildTasks
{
    public static class DownloadFile
    {
        public static async Task<ResultHelper> DownloadFileAsync(this HttpClient client, Uri uri, string fileName)
        {
            var result = ResultHelper.New();
            try
            {
                if (!ValidUri(uri.ToString())) throw new ArgumentException("Invalid uri", nameof(uri));

                if (string.IsNullOrEmpty(fileName)) throw new ArgumentException("Invalid file name.", nameof(fileName));

                if (!Path.IsPathRooted(fileName)) throw new ArgumentException("Invalid file name. Must be a valid full path.", nameof(fileName));

                // Check if fileName contains invalid characters
                var invalidChars = Path.GetInvalidFileNameChars();

                if (Path.GetFileName(fileName).IndexOfAny(invalidChars) >= 0) throw new ArgumentException("Invalid file name. Contains invalid characters.", nameof(fileName));

                if (File.Exists(fileName)) File.Delete(fileName);

                var response = await client.GetAsync(uri);
                if (!response.IsSuccessStatusCode) throw new FileNotFoundException($"'{uri}' not found. status: {response.StatusCode}", nameof(uri));

                using (var s = await client.GetStreamAsync(uri))
                using (var fs = new FileStream(fileName, FileMode.Create))
                {
                    await s.CopyToAsync(fs);
                }
                result = ResultHelper.Success();
            }
            catch (ArgumentNullException ex)
            {
                result = ResultHelper.Fail(-1, ex.Message);
            }
            catch (ArgumentException ex)
            {
                result = ResultHelper.Fail(-1, ex.Message);
            }
            catch (Exception ex)
            {
                result = ResultHelper.Fail(-1, ex.Message);
            }
            finally
            {
                if (result.IsSuccess())
                {
                    // Check if file got downnloaed and do any cleanup here
                    if (!File.Exists(fileName)) result = ResultHelper.Fail(-1, $"File {fileName} does not exist.");
                }
            }

            return result;
        }

        public static async Task<bool> UriExistsAsync(string uri)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(uri);
                    return response.IsSuccessStatusCode;
                }
                catch (HttpRequestException)
                {
                    return false;
                }
            }
        }

        public static bool ValidUri(string uri)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            bool result = Uri.TryCreate(uri, UriKind.Absolute, out Uri uriResult)
                        && uriResult.Scheme == Uri.UriSchemeHttps;
            return result;
        }
    }
}
