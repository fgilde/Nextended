using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.ResponseFilters.Tests.TestSupport;
using Shouldly;

namespace Nextended.ResponseFilters.Tests;

[TestClass]
public class HashTests
{
    private sealed class Sha256Default : ResponseFilter<OrderDto>
    {
        public Sha256Default() => Hash(x => x.Email).Always();
    }

    private sealed class Md5Filter : ResponseFilter<OrderDto>
    {
        public Md5Filter() => Hash(x => x.Email).AsMd5().Always();
    }

    private sealed class CustomHasher : ResponseFilter<OrderDto>
    {
        public CustomHasher() => Hash(x => x.Email).Using(s => $"H:{s.Length}").Always();
    }

    [TestMethod]
    public async Task DefaultSha256_ProducesCorrectHex()
    {
        var order = new OrderDto { Email = "hello" };
        await new Sha256Default().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        // Reference: SHA256("hello") = 2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824
        var expected = System.Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("hello"))).ToLowerInvariant();
        order.Email.ShouldBe(expected);
    }

    [TestMethod]
    public async Task Md5_ProducesCorrectHex()
    {
        var order = new OrderDto { Email = "hello" };
        await new Md5Filter().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        var expected = System.Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("hello"))).ToLowerInvariant();
        order.Email.ShouldBe(expected);
    }

    [TestMethod]
    public async Task CustomHasher_Used()
    {
        var order = new OrderDto { Email = "hello" };
        await new CustomHasher().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Email.ShouldBe("H:5");
    }

    [TestMethod]
    public async Task Null_PassesThrough()
    {
        var order = new OrderDto { Email = null };
        await new Sha256Default().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Email.ShouldBeNull();
    }

    [TestMethod]
    public async Task Empty_StaysEmpty()
    {
        var order = new OrderDto { Email = string.Empty };
        await new Sha256Default().ApplyAsync(order, Helpers.MakeContext()).ConfigureAwait(false);

        order.Email.ShouldBe(string.Empty);
    }

    [TestMethod]
    public async Task SameInput_ProducesSameOutput()
    {
        var a = new OrderDto { Email = "abc" };
        var b = new OrderDto { Email = "abc" };
        await new Sha256Default().ApplyAsync(a, Helpers.MakeContext()).ConfigureAwait(false);
        await new Sha256Default().ApplyAsync(b, Helpers.MakeContext()).ConfigureAwait(false);

        a.Email.ShouldBe(b.Email);
    }
}
