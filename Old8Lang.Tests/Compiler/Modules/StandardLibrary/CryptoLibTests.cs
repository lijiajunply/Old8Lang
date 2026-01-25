using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Modules.StandardLibrary;

/// <summary>
/// CryptoLib 库测试 - 测试加密和哈希功能
/// </summary>
[Collection("Sequential")]
public class CryptoLibTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_Crypto_ShouldWorkCorrectly()
    {
        var code = @"
import Crypto

PrintLine(""Crypto library imported"")
";
        CreateTempModuleFile("./StandardLibrary/crypto_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/crypto_test.old8");

        Assert.Null(exception);
        var cryptoLib = interpreter.Manager.GetValue(new LangId("Crypto"));
        Assert.NotNull(cryptoLib);
        Assert.IsAssignableFrom<IModuleValueType>(cryptoLib);
    }

    [Fact]
    public void Base64Encode_ShouldEncodeCorrectly()
    {
        var code = @"
import Crypto

text <- ""Hello Old8Lang""
encoded <- Crypto.Base64Encode(text)
PrintLine($""Original: {text}"")
PrintLine($""Encoded: {encoded}"")
";
        CreateTempModuleFile("./StandardLibrary/crypto_base64encode_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/crypto_base64encode_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Base64Decode_ShouldDecodeCorrectly()
    {
        var code = @"
import Crypto

text <- ""Hello Old8Lang""
encoded <- Crypto.Base64Encode(text)
decoded <- Crypto.Base64Decode(encoded)
PrintLine($""Original: {text}"")
PrintLine($""Decoded: {decoded}"")
";
        CreateTempModuleFile("./StandardLibrary/crypto_base64decode_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/crypto_base64decode_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Sha256Hash_ShouldGenerateHash()
    {
        var code = @"
import Crypto

text <- ""Hello Old8Lang""
hash <- Crypto.Sha256Hash(text)
PrintLine($""Text: {text}"")
PrintLine($""SHA256: {hash}"")
";
        CreateTempModuleFile("./StandardLibrary/crypto_sha256_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/crypto_sha256_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Sha512Hash_ShouldGenerateHash()
    {
        var code = @"
import Crypto

text <- ""Hello Old8Lang""
hash <- Crypto.Sha512Hash(text)
PrintLine($""Text: {text}"")
PrintLine($""SHA512: {hash}"")
";
        CreateTempModuleFile("./StandardLibrary/crypto_sha512_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/crypto_sha512_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void HmacSha256Hash_ShouldGenerateHmac()
    {
        var code = @"
import Crypto

text <- ""Hello Old8Lang""
key <- ""MySecretKey""
hmac <- Crypto.HmacSha256Hash(text, key)
PrintLine($""Text: {text}"")
PrintLine($""HMAC-SHA256: {hmac}"")
";
        CreateTempModuleFile("./StandardLibrary/crypto_hmacsha256_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/crypto_hmacsha256_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void XorEncrypt_And_XorDecrypt_ShouldWorkCorrectly()
    {
        var code = @"
import Crypto

text <- ""Hello Old8Lang""
key <- ""secret""
encrypted <- Crypto.XorEncrypt(text, key)
decrypted <- Crypto.XorDecrypt(encrypted, key)
PrintLine($""Original: {text}"")
PrintLine($""Encrypted: {encrypted}"")
PrintLine($""Decrypted: {decrypted}"")
";
        CreateTempModuleFile("./StandardLibrary/crypto_xor_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/crypto_xor_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Sha256Hash_ShouldBeDeterministic()
    {
        var code = @"
import Crypto

text <- ""Test Text""
hash1 <- Crypto.Sha256Hash(text)
hash2 <- Crypto.Sha256Hash(text)
PrintLine($""Hash 1: {hash1}"")
PrintLine($""Hash 2: {hash2}"")
PrintLine($""Are equal: {hash1 == hash2}"")
";
        CreateTempModuleFile("./StandardLibrary/crypto_sha256_deterministic_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/crypto_sha256_deterministic_test.old8");

        Assert.Null(exception);
    }
}
