using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Encryption;
using Nextended.Core.Helper;

namespace Nextended.Core.Tests
{
    [TestClass]
    public class EncrypionTests
    {
        [TestMethod]
        public void CanEncryptAndDecryptRijndaelWithUnique()
        {
            var operation = new RijndaelEncryption();
            string sample = "Hello my name is Florian Gilde and this is the Nextended pack";
            string passPhrase = "b14ca5898a4e4133bbce2ea2315a1916";
            var encrypted = operation.Encrypt(sample, passPhrase);

            var decrypted = operation.Decrypt(encrypted, passPhrase);
            Assert.AreEqual(sample, decrypted);

            var encrypted2 = operation.Encrypt(sample, passPhrase);
            Assert.AreNotEqual(encrypted, encrypted2);

            var decrypted2 = operation.Decrypt(encrypted, passPhrase);
            Assert.AreEqual(sample, decrypted2);
        }

        [TestMethod]
        public void CanEncryptAndDecryptAes()
        {
            var aes = new AesEncryption();
            
            string sample = "Hello my name is Florian Gilde and this is the Nextended pack";
            string passPhrase = "b14ca5898a4e4133bbce2ea2315a1916";
            var encrypted = aes.Encrypt(sample, passPhrase);

            var decrypted = aes.Decrypt(encrypted, passPhrase);
            Assert.AreEqual(sample, decrypted);
        }
    }
}