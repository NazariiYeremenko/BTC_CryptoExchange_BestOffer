namespace CryptoExchangeApp.Tests;

public interface IFileReader
{
    Task<string> ReadAllTextAsync(string path);
}