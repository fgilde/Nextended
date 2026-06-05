using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace Nextended.Core.Tests
{
    [TestClass]
    public class FileInfoTests
    {
        [TestMethod]
        public void FileInfoExtensionsWorking()
        {
            string path = @"C:\Windows\system32\notepad.exe";
            var fileInfo = new FileInfo(path);
            var dirInfo = fileInfo.Directory.Parent;

            var p = fileInfo.GetRelativePathTo(dirInfo);
            Assert.AreEqual(@".\system32\notepad.exe", p);
            Assert.AreEqual(@"Application", fileInfo.FileTypeDescription());
            Assert.AreEqual(true, fileInfo.IsExecutable());
            Assert.IsTrue(fileInfo.Exists);
        }
    }
}