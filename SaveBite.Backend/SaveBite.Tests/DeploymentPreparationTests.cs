using SaveBite.Deployment;
using Xunit;

namespace SaveBite.Tests;

public sealed class DeploymentPreparationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "savebite-prepare-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task PreparationIsIdempotentAndDoesNotRotateExistingSecrets()
    {
        await CreateSourceAsync();
        Assert.True(await DeploymentPreparation.PrepareAsync(_directory));
        var path = Path.Combine(_directory, ".env");
        var original = await File.ReadAllTextAsync(path);
        var values = DeploymentPreparation.ReadEnv(path);
        Assert.NotEmpty(values["BOOTSTRAP_ADMIN_PASSWORD"]);
        Assert.False(await DeploymentPreparation.PrepareAsync(_directory));
        Assert.Equal(original, await File.ReadAllTextAsync(path));
        if (!OperatingSystem.IsWindows())
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
    }

    [Fact]
    public async Task ConflictingCredentialsAreNotOverwrittenAndErrorDoesNotExposeSecret()
    {
        await CreateSourceAsync();
        await DeploymentPreparation.PrepareAsync(_directory);
        var source = Path.Combine(_directory, "SaveBite.Backend", ".env");
        await File.AppendAllTextAsync(source, "POSTGRES_PASSWORD='new-private-password'\n");
        var exception = await Assert.ThrowsAsync<PreparationException>(() => DeploymentPreparation.PrepareAsync(_directory));
        Assert.Contains("POSTGRES_PASSWORD", exception.Message);
        Assert.DoesNotContain("new-private-password", exception.Message);
        Assert.Equal("original-password", DeploymentPreparation.ReadEnv(Path.Combine(_directory, ".env"))["POSTGRES_PASSWORD"]);
    }

    [Theory]
    [InlineData("dollar$hash#quote'secret")]
    [InlineData("back\\slash\"secret")]
    public async Task QuotingPreservesCredentialCharacters(string secret)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, ".env");
        await File.WriteAllTextAsync(path, "TEST_SECRET=" + DeploymentPreparation.Quote(secret) + "\n");
        Assert.Equal(secret, DeploymentPreparation.ReadEnv(path)["TEST_SECRET"]);
    }

    private async Task CreateSourceAsync()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "SaveBite.Backend", "SaveBite.Api"));
        await File.WriteAllTextAsync(Path.Combine(_directory, "SaveBite.Backend", "SaveBite.Api", "appsettings.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(_directory, "SaveBite.Backend", ".env"), """
            POSTGRES_DB='savebite'
            POSTGRES_USER='app'
            POSTGRES_PASSWORD='original-password'
            REDIS_PASSWORD='redis-password'
            RABBITMQ_USER='app'
            RABBITMQ_PASSWORD='rabbit-password'
            MINIO_ROOT_USER='app'
            MINIO_ROOT_PASSWORD='minio-password'
            BootstrapAdmin__Password=''

            """);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
