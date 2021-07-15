namespace Nextended.Core.Contracts
{
    public interface IStringEncryption
    {
        string Encrypt(string str, string key);
        string Decrypt(string str, string key);
    }
}