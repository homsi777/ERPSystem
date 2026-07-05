using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ERPSystem.Services.China;

public sealed class ContainerDocumentationFileDto
{
    public Guid Id { get; init; }
    public string OriginalFileName { get; init; } = "";
    public string StoredFileName { get; init; } = "";
    public string ContentType { get; init; } = "";
    public long SizeBytes { get; init; }
    public DateTime UploadedAt { get; init; }
    public string SizeDisplay => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:N1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024.0):N1} MB"
    };
}

internal sealed class ContainerDocumentationManifest
{
    public Guid ContainerId { get; set; }
    public string ContainerNumber { get; set; } = "";
    public List<ContainerDocumentationManifestEntry> Files { get; set; } = [];
}

internal sealed class ContainerDocumentationManifestEntry
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}

public sealed class ContainerDocumentationService
{
    private const string ManifestFileName = "_manifest.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static ContainerDocumentationService Instance =>
        AppServices.GetRequiredService<ContainerDocumentationService>();

    public static string RootDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ERPPro",
            "توثيق حاويات");

    public string GetContainerDirectory(Guid containerId, string containerNumber) =>
        Path.Combine(RootDirectory, $"{SanitizeFolderName(containerNumber)}_{containerId:N}");

    public IReadOnlyList<ContainerDocumentationFileDto> ListFiles(Guid containerId, string containerNumber)
    {
        var folder = GetContainerDirectory(containerId, containerNumber);
        if (!Directory.Exists(folder))
            return [];

        var manifest = ReadManifest(folder);
        return manifest.Files
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => new ContainerDocumentationFileDto
            {
                Id = f.Id,
                OriginalFileName = f.OriginalFileName,
                StoredFileName = f.StoredFileName,
                ContentType = f.ContentType,
                SizeBytes = f.SizeBytes,
                UploadedAt = f.UploadedAt
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ContainerDocumentationFileDto>> UploadFilesAsync(
        Guid containerId,
        string containerNumber,
        IReadOnlyList<(string FileName, byte[] Content)> files,
        CancellationToken cancellationToken = default)
    {
        if (files.Count == 0)
            return [];

        var folder = GetContainerDirectory(containerId, containerNumber);
        Directory.CreateDirectory(folder);

        var manifest = ReadManifest(folder);
        manifest.ContainerId = containerId;
        manifest.ContainerNumber = containerNumber;

        foreach (var (fileName, content) in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var safeOriginal = SanitizeFileName(fileName);
            var storedName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}_{safeOriginal}";
            var fullPath = Path.Combine(folder, storedName);
            await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

            manifest.Files.Add(new ContainerDocumentationManifestEntry
            {
                Id = Guid.NewGuid(),
                OriginalFileName = safeOriginal,
                StoredFileName = storedName,
                ContentType = GuessContentType(safeOriginal),
                SizeBytes = content.LongLength,
                UploadedAt = DateTime.UtcNow
            });
        }

        WriteManifest(folder, manifest);

        return ListFiles(containerId, containerNumber);
    }

    public string? ResolveFilePath(Guid containerId, string containerNumber, Guid fileId)
    {
        var folder = GetContainerDirectory(containerId, containerNumber);
        var manifest = ReadManifest(folder);
        var entry = manifest.Files.FirstOrDefault(f => f.Id == fileId);
        if (entry is null)
            return null;

        var path = Path.Combine(folder, entry.StoredFileName);
        return File.Exists(path) ? path : null;
    }

    public void OpenFile(Guid containerId, string containerNumber, Guid fileId)
    {
        var path = ResolveFilePath(containerId, containerNumber, fileId);
        if (path is null)
            throw new FileNotFoundException("الملف غير موجود على القرص.");

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    public void OpenContainerFolder(Guid containerId, string containerNumber)
    {
        var folder = GetContainerDirectory(containerId, containerNumber);
        Directory.CreateDirectory(folder);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }

    private static ContainerDocumentationManifest ReadManifest(string folder)
    {
        var manifestPath = Path.Combine(folder, ManifestFileName);
        if (!File.Exists(manifestPath))
            return new ContainerDocumentationManifest();

        try
        {
            var json = File.ReadAllText(manifestPath);
            return JsonSerializer.Deserialize<ContainerDocumentationManifest>(json, JsonOptions)
                ?? new ContainerDocumentationManifest();
        }
        catch
        {
            return new ContainerDocumentationManifest();
        }
    }

    private static void WriteManifest(string folder, ContainerDocumentationManifest manifest)
    {
        var manifestPath = Path.Combine(folder, ManifestFileName);
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions));
    }

    private static string SanitizeFolderName(string value)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? "container" : value.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            trimmed = trimmed.Replace(c, '_');
        return trimmed;
    }

    private static string SanitizeFileName(string value)
    {
        var name = Path.GetFileName(value.Trim());
        if (string.IsNullOrWhiteSpace(name))
            name = "document";
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private static string GuessContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".tif" or ".tiff" => "image/tiff",
            _ => "application/octet-stream"
        };
    }
}
