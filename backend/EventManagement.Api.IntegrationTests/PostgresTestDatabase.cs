using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Npgsql;

namespace EventManagement.Api.IntegrationTests;

internal sealed class PostgresTestDatabase : IAsyncDisposable
{
    private const string ConnectionVariable = "EMS_TEST_POSTGRES";
    private readonly string? _dataDirectory;
    private readonly Process? _serverProcess;
    private readonly StringBuilder _serverLog;

    private PostgresTestDatabase(
        string connectionString,
        string? dataDirectory = null,
        Process? serverProcess = null,
        StringBuilder? serverLog = null)
    {
        ConnectionString = connectionString;
        _dataDirectory = dataDirectory;
        _serverProcess = serverProcess;
        _serverLog = serverLog ?? new StringBuilder();
    }

    public string ConnectionString { get; }

    public static async Task<PostgresTestDatabase> StartAsync()
    {
        var configuredConnection = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (!string.IsNullOrWhiteSpace(configuredConnection))
        {
            var configuredBuilder = new NpgsqlConnectionStringBuilder(configuredConnection)
            {
                Pooling = false,
                IncludeErrorDetail = true
            };
            await VerifyConnectionAsync(configuredBuilder.ConnectionString);
            return new PostgresTestDatabase(configuredBuilder.ConnectionString);
        }

        var initDbPath = FindExecutable("initdb");
        var postgresPath = FindExecutable("postgres");
        if (initDbPath is null || postgresPath is null)
        {
            throw new InvalidOperationException(
                $"PostgreSQL test binaries were not found. Install PostgreSQL or set {ConnectionVariable} " +
                "to a dedicated test database connection string.");
        }

        var dataDirectory = Path.Combine(
            Path.GetTempPath(),
            $"event-management-postgres-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dataDirectory);
        PostgresTestDatabase? database = null;

        try
        {
            await RunToCompletionAsync(
                initDbPath,
                ["-D", dataDirectory, "-A", "trust", "--no-locale", "--encoding=UTF8"],
                TimeSpan.FromSeconds(30));

            var port = GetAvailablePort();
            var serverLog = new StringBuilder();
            var process = CreateProcess(
                postgresPath,
                [
                    "-D", dataDirectory,
                    "-h", IPAddress.Loopback.ToString(),
                    "-p", port.ToString(),
                    "-F",
                    "-c", "fsync=off",
                    "-c", "synchronous_commit=off",
                    "-c", "full_page_writes=off"
                ]);
            AttachLogCapture(process, serverLog);
            if (!process.Start()) throw new InvalidOperationException("PostgreSQL failed to start.");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var connectionBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = IPAddress.Loopback.ToString(),
                Port = port,
                Database = "postgres",
                Username = Environment.UserName,
                Pooling = false,
                IncludeErrorDetail = true,
                Timeout = 2,
                CommandTimeout = 30
            };
            database = new PostgresTestDatabase(
                connectionBuilder.ConnectionString,
                dataDirectory,
                process,
                serverLog);
            await database.WaitUntilReadyAsync();
            return database;
        }
        catch
        {
            if (database is not null)
                await database.DisposeAsync();
            else
                TryDeleteDirectory(dataDirectory);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();

        if (_serverProcess is { HasExited: false })
        {
            _serverProcess.Kill(entireProcessTree: true);
            await _serverProcess.WaitForExitAsync();
        }
        _serverProcess?.Dispose();

        if (_dataDirectory is not null) TryDeleteDirectory(_dataDirectory);
    }

    private async Task WaitUntilReadyAsync()
    {
        Exception? lastException = null;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (_serverProcess?.HasExited == true)
            {
                throw new InvalidOperationException(
                    $"PostgreSQL exited before accepting connections.{Environment.NewLine}{ReadServerLog()}");
            }

            try
            {
                await VerifyConnectionAsync(ConnectionString);
                return;
            }
            catch (Exception exception) when (exception is NpgsqlException or TimeoutException or SocketException)
            {
                lastException = exception;
                await Task.Delay(100);
            }
        }

        throw new InvalidOperationException(
            $"PostgreSQL did not become ready.{Environment.NewLine}{ReadServerLog()}",
            lastException);
    }

    private static async Task VerifyConnectionAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync();
    }

    private string ReadServerLog()
    {
        lock (_serverLog) return _serverLog.ToString();
    }

    private static async Task RunToCompletionAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan timeout)
    {
        using var process = CreateProcess(executable, arguments);
        var output = new StringBuilder();
        AttachLogCapture(process, output);
        if (!process.Start()) throw new InvalidOperationException($"{executable} failed to start.");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            throw new TimeoutException($"{executable} did not finish within {timeout}.");
        }

        if (process.ExitCode != 0)
        {
            lock (output)
            {
                throw new InvalidOperationException(
                    $"{executable} exited with code {process.ExitCode}.{Environment.NewLine}{output}");
            }
        }
    }

    private static Process CreateProcess(string executable, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        return new Process { StartInfo = startInfo };
    }

    private static void AttachLogCapture(Process process, StringBuilder output)
    {
        process.OutputDataReceived += (_, eventArgs) => AppendLine(output, eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => AppendLine(output, eventArgs.Data);
    }

    private static void AppendLine(StringBuilder output, string? value)
    {
        if (value is null) return;
        lock (output) output.AppendLine(value);
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static string? FindExecutable(string executable)
    {
        var fileName = OperatingSystem.IsWindows() ? $"{executable}.exe" : executable;
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static void TryDeleteDirectory(string path)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException) when (attempt < 4)
            {
                Thread.Sleep(100);
            }
        }
    }
}
