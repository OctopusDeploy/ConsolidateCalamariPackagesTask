using System;
using System.Collections.Generic;

namespace Octopus.Build.ConsolidateCalamariPackagesTask
{
    public class ConsolidatedPackageIndex
    {
        public ConsolidatedPackageIndex(Dictionary<string, Package> packages)
        {
            Packages = new Dictionary<string, Package>(packages, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyDictionary<string, Package> Packages { get; }

        public class Package
        {
            public Package(string packageId, string version, Dictionary<string, string[]> platformHashes)
            {
                PackageId = packageId;
                Version = version;
                PlatformHashes = new Dictionary<string, string[]>(platformHashes, StringComparer.OrdinalIgnoreCase);
            }

            public string PackageId { get; }
            public string Version { get; }
            public Dictionary<string, string[]> PlatformHashes { get; }
        }
    }
}