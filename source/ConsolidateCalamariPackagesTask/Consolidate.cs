using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Octopus.Build.ConsolidateCalamariPackagesTask
{
    class Consolidate
    {
        private readonly ILog log;

        public Consolidate(ILog log)
        {
            this.log = log;
        }

        public string AssemblyVersion { get; set; } = ((AssemblyInformationalVersionAttribute) typeof(Consolidate).Assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute))).InformationalVersion;

        public (bool result, string packageFileName) Execute(string outputDirectory, IReadOnlyList<MsBuildPackageReference> packageReferences)
        {
            using (var hasher = new Hasher())
            {
                if (!Directory.Exists(outputDirectory))
                {
                    log.Error($"The output directory {outputDirectory} does not exist");
                    return (false, null);
                }

                var packages = GetPackages(hasher, packageReferences);

                var packageHash = hasher.GetPackageCombinationHash(AssemblyVersion, packages);
                log.Normal($"Hash of the package combination is {packageHash}");

                var destination = Path.Combine(outputDirectory, $"Calamari.{packageHash}.zip");
                if (File.Exists(destination))
                {
                    log.Normal("Calamari zip with the right package combination hash already exists");
                    return (true, destination);
                }

                DeleteExistingCalamariZips(destination);

                log.Normal("Scanning Calamari Packages");

                var indexEntries = packages.SelectMany(p => p.GetSourceFiles(log)).ToArray();

                log.Normal("Creating consolidated Calamari package");
                var sw = Stopwatch.StartNew();

                ConsolidatedPackageCreator.Create(indexEntries, destination);

                log.Normal($"Package creation took {sw.ElapsedMilliseconds:n0}ms");

                foreach (var item in indexEntries.Select(i => new {i.PackageId, i.Platform}).Distinct())
                    log.Normal($"Packaged {item.PackageId} for {item.Platform}");

                return (true, destination);
            }
        }

        private static IReadOnlyList<IPackageReference> GetPackages(Hasher hasher, IReadOnlyList<MsBuildPackageReference> packageReferences)
        {
            var calamariPackages = packageReferences
                .Where(p => p.Name.StartsWith("Calamari"))
                .Select(p => new CalamariPackageReference(hasher, p));

            var sashimiPackages = packageReferences
                .Where(p => p.Name.StartsWith("Sashimi."))
                .Select(p => new SashimiPackageReference(hasher, p));

            return calamariPackages.Concat<IPackageReference>(sashimiPackages).ToArray();
        }

        private void DeleteExistingCalamariZips(string destination)
        {
            log.Low("Deleting existing Calamari Zips");
            foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(destination), "Calamari.*.zip"))
                File.Delete(file);
        }
    }
}