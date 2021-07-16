using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Encode;
using Nextended.Core.Encryption;
using Nextended.Core.Hashing;
using Nextended.Core.Helper;

namespace Nextended.Core.Tests
{
    [TestClass]
    public class EncodingTests
    {
        [TestMethod]
        public void Base64()
        {
            var b = new Base64Encoding();
            var encode = b.Encode("Hallo flo");
            var r = b.Decode(encode);
            Assert.AreEqual(r, "Hallo flo");

            var encoded2 = "Hallo flo".EncodeDecode().Base64.Encode();
            Assert.AreEqual(encoded2, encode);
            var halloFlo = encoded2.EncodeDecode().Base64.Decode();
            Assert.AreEqual(halloFlo, "Hallo flo");
        }

        [TestMethod]
        public void Hex()
        {
            var b = new HexEncoding();
            var encode = b.Encode("Hallo flo");
            var r = b.Decode(encode);
            Assert.AreEqual(r, "Hallo flo");
        }

        [TestMethod]
        public void Hash()
        {
            string str = "Hallo flo";
            var hashed = str.Hash("salt").Sha265();
            var hashed2 = str.Hash().Sha265();
            var hashed3 = str.Hash().MD5();
            var hashed4 = str.Hash("salt").MD5();
            var hashed5 = str.Hash("salt").Sha512();
            var hashed6 = str.Hash().Sha512();
        }
    }
}