using System.Collections.Concurrent;
using System.Text;
using CommunityToolkit.Diagnostics;
using Docker.DotNet;
using Docker.DotNet.Models;
using MongoDB.Driver;
using Spectre.Console;

namespace Altinn.Authorization.Cli.ErrorDb;

internal static class ErrorDbHelper
{
    private const string LabelName = "altinn-auth-cli";
    private const string DbVolumeName = "altinn-auth-cli.error-db.db";
    private const string ConfigDbVolumeName = "altinn-auth-cli.error-db.configdb";
    private const string ContainerName = "altinn-auth-cli.error-db";
    private const string MongoDbImage = "docker.io/mongodb/mongodb-community-server";
    private const string MongoDbVersion = "7.0-ubi9";
    private const string MongoDbImageSpecific = $"{MongoDbImage}:{MongoDbVersion}";
    private const string HostPort = "19647";
    private const string MongoDbUser = "altinn-auth-cli";
    private const string MongoDbPassword = "altinn-auth-cli";
    private const string ConnectionString = "mongodb://altinn-auth-cli:altinn-auth-cli@localhost:19647/";

    public static async Task<MongoClient> GetClient(CancellationToken cancellationToken = default)
    {
        using var dockerConfig = new DockerClientConfiguration();
        using var dockerClient = dockerConfig.CreateClient();

        var dbVolume = await EnsureErrorDbVolume(dockerClient, DbVolumeName, cancellationToken);
        var configDbVolume = await EnsureErrorDbVolume(dockerClient, ConfigDbVolumeName, cancellationToken);
        var image = await EnsureImage(dockerClient, cancellationToken);
        var container = await EnsureErrorDbContainer(dockerClient, dbVolume, configDbVolume, cancellationToken);
        await EnsureRunning(dockerClient, container, cancellationToken);

        return new MongoClient(ConnectionString);
    }

    private static async Task EnsureRunning(DockerClient client, ContainerListResponse container, CancellationToken cancellationToken)
    {
        await client.Containers.StartContainerAsync(
            container.ID,
            new()
            {
            },
            cancellationToken);
    }

    private static async Task<string> EnsureImage(DockerClient client, CancellationToken cancellationToken)
    {
        var imageList = await client.Images.ListImagesAsync(
            new()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "reference", new Dictionary<string, bool> { { MongoDbImageSpecific, true } } },
                },
            },
            cancellationToken);

        if (imageList.Count > 1)
        {
            ThrowHelper.ThrowInvalidOperationException(
                $"Found multiple images with name '{MongoDbImage}:{MongoDbVersion}'. This is unexpected and may cause issues.");
        }

        if (imageList.Count == 1)
        {
            return imageList[0].ID; // Image already exists
        }

        await AnsiConsole.Progress()
            .AutoClear(true)
            .Columns([
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
            ])
            .StartAsync(async (ctx) =>
            {
                var tasks = new ConcurrentDictionary<string, ProgressTask>();
                var progress = new Progress<JSONMessage>(msg =>
                {
                    var task = tasks.GetOrAdd(msg.ID, id =>
                    {
                        var task = ctx.AddTask(id, autoStart: true);
                        ////_ = FetchTaskInfo(task, msg.ID, cancellationToken);

                        return task;
                    });

                    if (msg.Progress.Total > 0)
                    {
                        task.MaxValue = msg.Progress.Total;
                        task.Value = msg.Progress.Current;
                    }
                    else
                    {
                        task.Value = task.MaxValue;
                        task.StopTask();
                    }
                });

                await client.Images.CreateImageAsync(
                    new()
                    {
                        FromImage = MongoDbImageSpecific,
                    },
                    authConfig: null,
                    progress,
                    cancellationToken);
            });

        imageList = await client.Images.ListImagesAsync(
            new()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "reference", new Dictionary<string, bool> { { MongoDbImageSpecific, true } } },
                },
            },
            cancellationToken);

        if (imageList.Count != 1)
        {
            ThrowHelper.ThrowInvalidOperationException(
                $"Expected exactly one image with name '{MongoDbImage}:{MongoDbVersion}' after creation, but found {imageList.Count}.");
        }

        return imageList[0].ID;
    }

    private static async Task<ContainerListResponse> EnsureErrorDbContainer(
        DockerClient client,
        VolumeResponse dbVolume,
        VolumeResponse configDbVolume,
        CancellationToken cancellationToken)
    {
        var containerList = await client.Containers.ListContainersAsync(
            new()
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "name", new Dictionary<string, bool> { { ContainerName, true } } },
                },
            },
            cancellationToken);

        if (containerList.Count > 1)
        {
            ThrowHelper.ThrowInvalidOperationException(
                $"Found multiple containers with name '{ContainerName}'. This is unexpected and may cause issues.");
        }

        if (containerList.Count == 1)
        {
            return containerList[0];
        }

        var createResponse = await client.Containers.CreateContainerAsync(
            new() 
            {
                Name = ContainerName,
                Image = MongoDbImageSpecific,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { "27017/tcp", default },
                },
                Volumes = new Dictionary<string, EmptyStruct>
                {
                    { "/data/db", default },
                    { "/data/configdb", default },
                },
                Labels = new Dictionary<string, string>
                {
                    { LabelName, "error-db" },
                },
                Env = new List<string>
                {
                    $"MONGODB_INITDB_ROOT_USERNAME={MongoDbUser}",
                    $"MONGODB_INITDB_ROOT_PASSWORD={MongoDbPassword}",
                },
                HostConfig = new()
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        { "27017/tcp", [new() { HostPort = HostPort }] }
                    },
                    Mounts = 
                    [
                        new() 
                        {
                            Target = "/data/db",
                            Source = dbVolume.Name,
                            Type = "volume",
                        },
                        new()
                        {
                            Target = "/data/configdb",
                            Source = configDbVolume.Name,
                            Type = "volume",
                        }
                    ],
                },
            },
            cancellationToken);

        EnsureNoWarnings(createResponse.Warnings);

        containerList = await client.Containers.ListContainersAsync(
            new()
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "name", new Dictionary<string, bool> { { ContainerName, true } } },
                },
            },
            cancellationToken);

        if (containerList.Count != 1)
        {
            ThrowHelper.ThrowInvalidOperationException(
                $"Expected exactly one container with name '{ContainerName}' after creation, but found {containerList.Count}.");
        }

        return containerList[0];
    }

    private static async Task<VolumeResponse> EnsureErrorDbVolume(
        DockerClient client,
        string volumeName,
        CancellationToken cancellationToken)
    {
        var volumeList = await client.Volumes.ListAsync(
            new()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "name", new Dictionary<string, bool> { { volumeName, true } } },
                },
            },
            cancellationToken);

        EnsureNoWarnings(volumeList.Warnings);
        
        if (volumeList.Volumes.Count > 1)
        {
            ThrowHelper.ThrowInvalidOperationException(
                $"Found multiple volumes with name '{volumeName}'. This is unexpected and may cause issues.");
        }

        if (volumeList.Volumes.Count == 1)
        {
            return volumeList.Volumes[0];
        }

        var createResponse = await client.Volumes.CreateAsync(
            new()
            {
                Name = volumeName,
                Labels = new Dictionary<string, string>
                {
                    { LabelName, "error-db" },
                },
            },
            cancellationToken);

        return createResponse;
    }

    private static void EnsureNoWarnings(ICollection<string>? warnings)
    {
        if (warnings is not { Count: > 0 })
        {
            return;
        }

        var sb = new StringBuilder("One or more warnings received").AppendLine();
        foreach (var warning in warnings)
        {
            sb.Append("- ").AppendLine(warning);
        }

        ThrowHelper.ThrowInvalidOperationException(sb.ToString());
    }
}
