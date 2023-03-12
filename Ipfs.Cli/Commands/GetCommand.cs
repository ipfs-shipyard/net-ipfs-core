using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Threading.Tasks.Dataflow;
using IpfsShipyard.Ipfs.Core;
using McMaster.Extensions.CommandLineUtils;
using Nito.AsyncEx;

namespace Ipfs.Cli.Commands;

[Command(Description = "Download IPFS data")]
internal class GetCommand : CommandBase
{
    private readonly AsyncLock _zipLock = new();

    private ActionBlock<IpfsFile> _fetch;
    private int _processed;

    // when requested equals processed then the task is done.
    private int _requested = 1;

    // ZipArchive is NOT thread safe
    private ZipArchive _zip;

    [Argument(0, "ipfs-path", "The path to the IPFS data")]
    [Required]
    public string IpfsPath { get; set; }

    [Option("-o|--output", Description = "The output path for the data")]
    public string OutputBasePath { get; set; }

    [Option("-c|--compress", Description = "Create a ZIP compressed file")]
    public bool Compress { get; set; }

    private Program Parent { get; set; }

    private async Task FetchFileOrDirectory(IpfsFile file)
    {
        if (file.Node.IsDirectory)
        {
            foreach (var link in file.Node.Links)
            {
                var next = new IpfsFile
                {
                    Path = Path.Combine(file.Path, link.Name),
                    Node = await Parent.CoreApi.FileSystem.ListFileAsync(link.Id)
                };
                ++_requested;
                _fetch.Post(next);
            }
        }
        else
        {
            if (_zip != null)
            {
                await SaveToZip(file);
            }

            else
            {
                await SaveToDisk(file);
            }
        }

        if (++_processed == _requested)
        {
            _fetch.Complete();
        }
    }

    private async Task SaveToZip(IpfsFile file)
    {
        await using var instream = await Parent.CoreApi.FileSystem.ReadFileAsync(file.Node.Id);
        using var _ = await _zipLock.LockAsync();
        await using var entryStream = _zip.CreateEntry(file.Path).Open();
        await instream.CopyToAsync(entryStream);
    }

    private async Task SaveToDisk(IpfsFile file)
    {
        var outputPath = Path.GetFullPath(Path.Combine(OutputBasePath, file.Path));
        var directoryName = Path.GetDirectoryName(outputPath);
        ArgumentException.ThrowIfNullOrEmpty(directoryName);
        Directory.CreateDirectory(directoryName);
        await using var instream = await Parent.CoreApi.FileSystem.ReadFileAsync(file.Node.Id);
        await using var outstream = File.Create(outputPath);
        await instream.CopyToAsync(outstream);
    }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        OutputBasePath ??= Path.Combine(".", IpfsPath);

        if (Compress)
        {
            var zipPath = Path.GetFullPath(OutputBasePath);
            if (!Path.HasExtension(zipPath))
            {
                zipPath = Path.ChangeExtension(zipPath, ".zip");
            }

            await app.Out.WriteLineAsync($"Saving to {zipPath}");
            _zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        }

        try
        {
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 10
            };
            _fetch = new(FetchFileOrDirectory, options);
            var first = new IpfsFile
            {
                Path = _zip == null ? "" : IpfsPath,
                Node = await Parent.CoreApi.FileSystem.ListFileAsync(IpfsPath)
            };
            _fetch.Post(first);
            await _fetch.Completion;
        }
        finally
        {
            _zip?.Dispose();
        }

        return 0;
    }

    private class IpfsFile
    {
        public IFileSystemNode Node;
        public string Path;
    }
}