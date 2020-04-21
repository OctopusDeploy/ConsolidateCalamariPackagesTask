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

        public IReadOnlyList<SourceFile> GetSourceFiles()
        {
            var isNetFx = Name == "Calamari";
            var isCloud = Name == "Calamari.Cloud";
            var platform = isNetFx || isCloud
                ? "netfx"
                : Name.Split('.')[1];

            var archivePath = Path.Combine(packageReference.ResolvedPath, $"{Name}.{Version}.nupkg".ToLower());
            if (!File.Exists(archivePath))
                throw new Exception($"Could not find the source NuGet package {archivePath} does not exist");

            using (var zip = ZipFile.OpenRead(archivePath))
                return zip.Entries
                    .Where(e => e.FullName != "[Content_Types].xml")
                    .Where(e => !e.FullName.StartsWith("_rels"))
                    .Where(e => !e.FullName.StartsWith("package/services"))
                    .Select(entry => new SourceFile
                    {
                        PackageId = isCloud ? "Calamari.Cloud" : "Calamari",
                        Version = Version,
                        Platform = platform,
                        ArchivePath = archivePath,
                        Name = entry.FullName,
                        FullName = entry.FullName,
                        Hash = hasher.Hash(entry)
                    })
                    .ToArray();
        }
    }
}