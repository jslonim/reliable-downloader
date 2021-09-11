using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ReliableDownloader
{
    public class FileDownloader : IFileDownloader
    {
        WebSystemCalls service = new WebSystemCalls();
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        long? totalBytesRead = 0L;
        bool isPartial = false;
        long? chunckSize = 0L;

        public async Task<bool> DownloadFile(string contentFileUrl, string localFilePath, Action<FileProgress> onProgressChanged)
        {
            if (File.Exists(localFilePath))
            {
                File.Delete(localFilePath);
            }

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            HttpResponseMessage headersResponse = await service.GetHeadersAsync(contentFileUrl, cancellationToken);

            long? remoteFileSize = headersResponse.Content.Headers.ContentLength;
            isPartial = headersResponse.Headers.AcceptRanges.ToString() == "bytes";
            //In case that the CDN allows partial download, we divide the total file in 100 "chunks" and download one after the other
            //If the connection fails, we will inform the user and retry in 5 seconds
            if (isPartial)
            {
                chunckSize = remoteFileSize / 100;

                while (totalBytesRead < remoteFileSize)
                {
                    try
                    {
                        //If the sum of the bytes read + chunck size is grater than the file, means this is the last part, so we will use the file size for the "to"
                        long? to = totalBytesRead + chunckSize > remoteFileSize ? remoteFileSize : totalBytesRead + chunckSize;

                        HttpResponseMessage response = await service.DownloadPartialContent(contentFileUrl, totalBytesRead, to, cancellationToken);

                        using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                            await ProcessContentStream(response.Content.Headers.ContentLength, contentStream, localFilePath, onProgressChanged, cancellationToken);
                    }
                    catch (HttpRequestException)
                    {
                        Console.WriteLine("Connection lost, retrying in 5 seconds..");
                        System.Threading.Thread.Sleep(5000);
                        continue;
                    }
                }
            }
            //In case the CDN does not allow partial, we download directly, using HttpCompletionOption.ResponseHeadersRead in the request to save while downloading
            else
            {
                HttpResponseMessage response = await service.DownloadContent(contentFileUrl, cancellationToken);

                response.EnsureSuccessStatusCode();

                using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    await ProcessContentStream(response.Content.Headers.ContentLength, contentStream, localFilePath, onProgressChanged, cancellationToken);
            }


            if (GetMd5(localFilePath).SequenceEqual(headersResponse.Content.Headers.ContentMD5))
            {
                Console.WriteLine("Valid File!");
            }
            else
            {
                Console.WriteLine("Ivalid File!");
            }

            return true;
        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream, string destinationFilePath, Action<FileProgress> onProgressChanged, CancellationToken cancellationToken)
        {
            long readCount = 0L;
            byte[] buffer = new byte[8192];
            bool isMoreToRead = true;

            using (var fileStream = new FileStream(destinationFilePath, FileMode.Append, FileAccess.Write, FileShare.None, 8192, true))
            {
                do
                {
                    int bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead, onProgressChanged);
                        continue;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);

                    totalBytesRead += bytesRead;
                    readCount += 1;

                    if (readCount % 10 == 0)
                    {
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead, onProgressChanged);
                    }

                }
                while (isMoreToRead);
            }
        }
        private void TriggerProgressChanged(long? totalDownloadSize, long? totalBytesRead, Action<FileProgress> onProgressChanged)
        {
            double? progressPercentage = null;

            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round(((double)totalBytesRead / totalDownloadSize.Value * 100), 2);

            if (isPartial)
                progressPercentage = Math.Round((double)totalBytesRead / (double)chunckSize, 2);

            //Show only the percentage number once instead of: 1% , 1.05% , 1.27% or all the number if its the whole file
            if (!isPartial || Math.Truncate(Convert.ToDecimal(progressPercentage)) - Convert.ToDecimal(progressPercentage) == 00)
            {
                onProgressChanged(new FileProgress(totalDownloadSize, totalBytesRead, progressPercentage, null));
            }
        }

        private byte[] GetMd5(string localFile)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(localFile))
                {
                    return md5.ComputeHash(stream);
                }
            }
        }

        public void CancelDownloads()
        {
            cancellationTokenSource.Cancel();
        }

    }
}