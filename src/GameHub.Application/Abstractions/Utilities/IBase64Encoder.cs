namespace GameHub.Application.Abstractions.Utilities;

public interface IBase64Encoder
{
    string Encode(string plainText);
    string Decode(string base64EncodedData);
}
