using System.Diagnostics;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Signing
{
    public class ConsoleSignData
    {
        private readonly string _pathToSignerExe;

        public ConsoleSignData(string pathToSignerExe)
        {
            _pathToSignerExe = pathToSignerExe;
        }

        public async Task<Result<string>> SignAsync(string data, string certificateSerialNumber)
        {
            var tempFilePath = default(string);
            try
            {
                if (data.Length > 1000)
                {
                    tempFilePath = Path.GetTempFileName();
                    File.WriteAllText(tempFilePath, data);

                    //_logger.LogInformation($"Wrote data in {tempFile}");
                }
                else
                {
                    //_logger.LogInformation($"Passing data in command line");
                }

                var signer = new Process();
                signer.StartInfo.FileName = _pathToSignerExe;
                signer.StartInfo.Arguments = $"{certificateSerialNumber} {tempFilePath ?? data}"; // Pass either the path to the temp file, or the data itself
                signer.StartInfo.UseShellExecute = false;
                signer.StartInfo.RedirectStandardOutput = true;
                signer.Start();

                var output = signer.StandardOutput.ReadToEnd();

                await signer.WaitForExitAsync();

                return signer.ExitCode == 0
                    ? Result.Success(output)
                    : Result.Failure<string>($"[{Path.GetFileName(_pathToSignerExe)}] {output}");
            }
            catch (Exception e)
            {
                return Result.Failure<string>($"Error while trying to sign data.{Environment.NewLine}{e}");
            }
            finally
            {
                if (tempFilePath != default && File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
    }
}
