using System;

namespace Nextended.Core.Contracts
{
    public interface IStringEncoding
    {
        string Encode(string str);
        string Decode(string str);
    }

    public interface IStringEncodingExt : IStringEncoding
    {
        IStringEncodingExt BeforeEncode(Func<string, string> onBeforeEncode);
        IStringEncodingExt AfterEncode(Func<string, string> onAfterEncode);
        IStringEncodingExt BeforeDecode(Func<string, string> onBeforeDecode);
        IStringEncodingExt AfterDecode(Func<string, string> onAfterDecode);
    }

}