using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Aeron.LowLatency.IntegrationTests.Testcontainers;

public sealed class AeronContainerFixture : IAsyncLifetime
{
    private static readonly Uri AeronAllJarUri = new("https://repo1.maven.org/maven2/io/aeron/aeron-all/1.51.0/aeron-all-1.51.0.jar");
    private static readonly Uri TemurinJreUri = new("https://api.adoptium.net/v3/binary/latest/21/ga/windows/x64/jre/hotspot/normal/eclipse");

    private readonly IContainer? _container;
    private readonly string _rootDirectory;
    private readonly string _jarPath;
    private string _javaExecutable = "java";
    private Process? _localMediaDriver;

    public AeronContainerFixture()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "aeron-low-latency-tests", Guid.NewGuid().ToString("N"));
        AeronDirectory = Path.Combine(_rootDirectory, "driver");
        _jarPath = Path.Combine(_rootDirectory, "aeron-all.jar");
        Directory.CreateDirectory(_rootDirectory);

        if (!ShouldUseLocalMediaDriver())
        {
            _container = new ContainerBuilder()
                .WithImage("eclipse-temurin:21-jre")
                .WithName($"aeron-media-driver-{Guid.NewGuid():N}")
                .WithBindMount(_rootDirectory, "/aeron")
                .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
                .WithCommand(
                    "/bin/sh",
                    "-c",
                    "echo starting-aeron-media-driver && java --add-opens java.base/jdk.internal.misc=ALL-UNNAMED --add-opens java.base/sun.nio.ch=ALL-UNNAMED -Daeron.dir=/aeron/driver -Daeron.term.buffer.sparse.file=false -cp /aeron/aeron-all.jar io.aeron.driver.MediaDriver")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilFileExists("/aeron/driver/cnc.dat", FileSystem.Container, wait => wait.WithTimeout(TimeSpan.FromSeconds(30))))
                .Build();
        }
    }

    public string AeronDirectory { get; }

    public async ValueTask InitializeAsync()
    {
        await DownloadAeronJarAsync().ConfigureAwait(false);
        if (_container is not null)
        {
            await _container.StartAsync().ConfigureAwait(false);
            return;
        }

        _javaExecutable = await ResolveJavaExecutableAsync().ConfigureAwait(false);
        StartLocalMediaDriver();
        await WaitForCncFileAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }

        if (_localMediaDriver is not null)
        {
            await StopLocalMediaDriverAsync().ConfigureAwait(false);
        }

        try
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private async ValueTask DownloadAeronJarAsync()
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        await using var input = await httpClient.GetStreamAsync(AeronAllJarUri).ConfigureAwait(false);
        await using var output = File.Create(_jarPath);
        await input.CopyToAsync(output).ConfigureAwait(false);
    }

    private void StartLocalMediaDriver()
    {
        var startInfo = new ProcessStartInfo(_javaExecutable)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        startInfo.ArgumentList.Add("--add-opens");
        startInfo.ArgumentList.Add("java.base/jdk.internal.misc=ALL-UNNAMED");
        startInfo.ArgumentList.Add("--add-opens");
        startInfo.ArgumentList.Add("java.base/sun.nio.ch=ALL-UNNAMED");
        startInfo.ArgumentList.Add($"-Daeron.dir={AeronDirectory}");
        startInfo.ArgumentList.Add("-Daeron.term.buffer.sparse.file=false");
        startInfo.ArgumentList.Add("-cp");
        startInfo.ArgumentList.Add(_jarPath);
        startInfo.ArgumentList.Add("io.aeron.driver.MediaDriver");

        _localMediaDriver = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start local Java Aeron Media Driver.");
    }

    private async ValueTask WaitForCncFileAsync()
    {
        var cncPath = Path.Combine(AeronDirectory, "cnc.dat");
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (!File.Exists(cncPath))
        {
            if (_localMediaDriver?.HasExited == true)
            {
                var error = await _localMediaDriver.StandardError.ReadToEndAsync().ConfigureAwait(false);
                throw new InvalidOperationException($"Local Aeron Media Driver exited before creating cnc.dat. {error}");
            }

            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException($"Local Aeron Media Driver did not create {cncPath} within 30 seconds.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
        }
    }

    private async ValueTask StopLocalMediaDriverAsync()
    {
        if (_localMediaDriver is null)
        {
            return;
        }

        if (!_localMediaDriver.HasExited)
        {
            _localMediaDriver.Kill(entireProcessTree: true);
            await _localMediaDriver.WaitForExitAsync().ConfigureAwait(false);
        }

        _localMediaDriver.Dispose();
    }

    private static async ValueTask<string> ResolveJavaExecutableAsync()
    {
        var configuredJava = Environment.GetEnvironmentVariable("AERON_JAVA");
        if (!string.IsNullOrWhiteSpace(configuredJava) && await IsJava17OrNewerAsync(configuredJava).ConfigureAwait(false))
        {
            return configuredJava;
        }

        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            var javaFromHome = Path.Combine(javaHome, "bin", JavaExecutableName);
            if (File.Exists(javaFromHome) && await IsJava17OrNewerAsync(javaFromHome).ConfigureAwait(false))
            {
                return javaFromHome;
            }
        }

        if (await IsJava17OrNewerAsync("java").ConfigureAwait(false))
        {
            return "java";
        }

        return await EnsurePortableJreAsync().ConfigureAwait(false);
    }

    private static bool ShouldUseLocalMediaDriver()
    {
        var value = Environment.GetEnvironmentVariable("AERON_USE_LOCAL_DRIVER");
        return OperatingSystem.IsWindows() || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string JavaExecutableName => OperatingSystem.IsWindows() ? "java.exe" : "java";

    private static async ValueTask<bool> IsJava17OrNewerAsync(string javaExecutable)
    {
        try
        {
            var startInfo = new ProcessStartInfo(javaExecutable)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            startInfo.ArgumentList.Add("-version");

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            var output = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            output += await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);

            var match = Regex.Match(output, "version \"(?<major>\\d+)");
            return match.Success && int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture) >= 17;
        }
        catch (Exception) when (!Debugger.IsAttached)
        {
            return false;
        }
    }

    private static async ValueTask<string> EnsurePortableJreAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException("Java 17+ is required to run the local Aeron Media Driver. Install Java 21 or run the Linux Testcontainers path without AERON_USE_LOCAL_DRIVER=true.");
        }

        var cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "aeron-low-latency-dotnet", "jre-21");
        Directory.CreateDirectory(cacheDirectory);

        var javaExecutable = Directory
            .EnumerateFiles(cacheDirectory, "java.exe", SearchOption.AllDirectories)
            .FirstOrDefault(path => path.EndsWith(Path.Combine("bin", "java.exe"), StringComparison.OrdinalIgnoreCase));

        if (javaExecutable is not null)
        {
            return javaExecutable;
        }

        var archivePath = Path.Combine(cacheDirectory, "temurin-jre-21.zip");

        using (var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
        {
            await using var input = await httpClient.GetStreamAsync(TemurinJreUri).ConfigureAwait(false);
            await using var output = File.Create(archivePath);
            await input.CopyToAsync(output).ConfigureAwait(false);
        }

        ZipFile.ExtractToDirectory(archivePath, cacheDirectory, overwriteFiles: true);
        File.Delete(archivePath);

        javaExecutable = Directory
            .EnumerateFiles(cacheDirectory, "java.exe", SearchOption.AllDirectories)
            .FirstOrDefault(path => path.EndsWith(Path.Combine("bin", "java.exe"), StringComparison.OrdinalIgnoreCase));

        return javaExecutable ?? throw new InvalidOperationException("Could not locate java.exe in the downloaded portable JRE.");
    }
}
