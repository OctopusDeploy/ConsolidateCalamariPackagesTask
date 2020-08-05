using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Octopus.Build.ConsolidateCalamariPackagesTask
{
    class CalamariPackageReference : IPackageReference
    {
        private readonly Hasher hasher;
        private readonly MsBuildPackageReference packageReference;

        public CalamariPackageReference(Hasher hasher, MsBuildPackageReference packageReference)
        {
            this.hasher = hasher;
            this.packageReference = packageReference;
        }

        public string Name => packageReference.Name;
        public string Version => packageReference.Version;

        public IReadOnlyList<SourceFile> GetSourceFiles(ILog log)
        {
            var archivePath = Path.Combine(packageReference.ResolvedPath, $"{Name}.{Version}.nupkg".ToLower());
            if (!File.Exists(archivePath))
                throw new Exception($"Could not find the source NuGet package {archivePath} does not exist");

            using (var zip = ZipFile.OpenRead(archivePath))
                return zip.Entries
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.FullName != "[Content_Types].xml")
                    .Where(e => !e.FullName.StartsWith("_rels"))
                    .Where(e => !e.FullName.StartsWith("package/services"))
                    .Where(e => !e.FullName.EndsWith("nuspec"))
                    .Select(entry =>
                    {
                        // Sashimi zips have each full Calamari executable in folders according to platform
                        var parts = entry.FullName.Split('/');
                        return new SourceFile
                        {
                            PackageId = Name,
                            Version = Version,
                            Platform = parts[0],
                            ArchivePath = archivePath,
                            IsNupkg = true,
                            FullNameInDestinationArchive = entry.FullName,
                            FullNameInSourceArchive = entry.FullName,
                            Hash = hasher.Hash(entry)
                        };
                    })
                    .ToArray();
        }
    }
}