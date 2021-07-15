namespace Nextended.Core.Contracts
{
    public interface IStringHashing
    {
        public string Hash(string input, string salt = null);
    }
}