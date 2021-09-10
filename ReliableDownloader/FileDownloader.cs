using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ReliableDownloader
{
    public class FileDownloader : IFileDownloader
    {
        WebSystemCalls service = new WebSystemCalls();
        long totalBytesWritten = 0;
        long LasttotalBytesWritten = 0;
        HttpResponseMessage headersResponse;
        Timer timer;

        public async Task<bool> DownloadFile(string contentFileUrl, string localFilePath, Action<FileProgress> onProgressChanged)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            headersResponse = await service.GetHeadersAsync(contentFileUrl, cancellationToken);

            HttpResponseMessage response = await service.DownloadContent(contentFileUrl, cancellationToken);

            response.EnsureSuccessStatusCode();

            using (Stream contentStream =  await response.Content.ReadAsStreamAsync())
                await ProcessContentStream(response.Content.Headers.ContentLength, contentStream, localFilePath, onProgressChanged, cancellationToken);

            return true;
        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream, string destinationFilePath, Action<FileProgress> onProgressChanged, CancellationToken cancellationToken)
        {
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[8192];
            bool isMoreToRead = true;
            timer = new Timer(OnTimerElapsed, null, 5000, 5000);

            using (var fileStream = new FileStream(destinationFilePath, FileMode.Append, FileAccess.Write, FileShare.None, 8192, true))
            {
                do
                {
                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead, onProgressChanged);
                        continue;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);

                    totalBytesWritten = fileStream.Length;

                    totalBytesRead += bytesRead;
                    readCount += 1;

                    if (readCount % 10 == 0)
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead, onProgressChanged);
                }
                while (isMoreToRead);
            }
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead, Action<FileProgress> onProgressChanged)
        {
            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);


            onProgressChanged(new FileProgress(totalDownloadSize, totalBytesRead, progressPercentage, null));
        }

        public void OnTimerElapsed(object state)
        {

            if (LasttotalBytesWritten == totalBytesWritten)
            {
                CancelDownloads();
                timer.Dispose();
            }

            LasttotalBytesWritten = totalBytesWritten;
        }

        public void CancelDownloads()
        {
            Console.WriteLine("Download Canceled");
        }
    }
}