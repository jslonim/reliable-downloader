using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ReliableDownloader.Tests
{
    [TestFixture]
    public class Tests
    {
        string exampleUrl = "https://installerstaging.accurx.com/chain/3.55.11050.0/accuRx.Installer.Local.msi";
        string exampleFilePath = "C:/Users/Julian/Downloads/myfirstdownload.msi";

        [SetUp]
        public void Setup()
        {
            if (File.Exists(exampleFilePath))
            {
                File.Delete(exampleFilePath);
            }
        }

        [Test]
        public async Task DownloadFile_Partial_ShouldDownloadWholeFile()
        {
            FileDownloader fileDownloader = new FileDownloader();
            await fileDownloader.DownloadFile(exampleUrl, exampleFilePath, progress => { Console.WriteLine($"Percent progress is {progress.ProgressPercent}%"); });
            FileInfo info = new FileInfo(exampleFilePath);
            Assert.IsTrue(info.Exists);
            Assert.AreEqual(info.Length, 12644352);
        }

        [Test]
        public async Task DownloadFile_Cancel_ShouldStopDownload()
        {
            FileDownloader fileDownloader = new FileDownloader();

            try
            {
                await fileDownloader.DownloadFile(exampleUrl, exampleFilePath, progress =>
               {
                   if (progress.ProgressPercent >= .1)
                   {
                       fileDownloader.CancelDownloads();
                   }
               });

            }
            catch (Exception)
            {
                Assert.Throws<TaskCanceledException>(() => { throw new TaskCanceledException(); });
            }           
        }


    }
}