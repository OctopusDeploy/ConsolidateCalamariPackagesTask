using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Octopus.Build.ConsolidateCalamariPackagesTask
{
    class SashimiPackageReference : IPackageReference
    {
        private readonly Hasher hasher;
        private readonly MsBuildPackageReference packageReference;

        public SashimiPackageReference(Hasher hasher, MsBuildPackageReference packageReference)
        {
            this.hasher = hasher;
            this.packageReference = packageReference;
        }

        public string Name => packageReference.Name;
        public string Version => packageReference.Version;

        public IReadOnlyList<SourceFile> GetSourceFiles(ILog log)
        {
            var toolZipsDir = Path.Combine(packageReference.ResolvedPath, "tools");

            if (!Directory.Exists(toolZipsDir))
            {
                log.Low($"Skipping {Name} as it does not have a tools folder: {toolZipsDir}");
                return Array.Empty<SourceFile>();
            }

            var toolZips = Directory.GetFiles(toolZipsDir);

            if (toolZips.Length == 0)
            {
                log.Low($"Skipping {Name} as it does not have any zip files in the tools folder: {toolZipsDir}");
                return Array.Empty<SourceFile>();
            }

            return toolZips.SelectMany(toolZipPath => ReadSashimiPackagedZip(toolZipPath))
                .ToArray();
        }

        private IEnumerable<SourceFile> ReadSashimiPackagedZip(string toolZipPath)
        {
            using (var zip = ZipFile.OpenRead(toolZipPath))
                return zip.Entries
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Select(entry =>
                    {
                        // Sashimi zips have each full Calamari executable in folders according to platform
                        var parts = entry.FullName.Split('/');
                        return new SourceFile
                        {
                            PackageId = Path.GetFileNameWithoutExtension(toolZipPath),
                            Version = Version,
                            Platform = parts[0],
                            ArchivePath = toolZipPath,
                            FullNameInDestinationArchive = string.Join("/", parts.Skip(1)),
                            FullNameInSourceArchive = entry.FullName,
                            Hash = hasher.Hash(entry)
                        };
                    })
                    .ToArray();
        }
    }
}
