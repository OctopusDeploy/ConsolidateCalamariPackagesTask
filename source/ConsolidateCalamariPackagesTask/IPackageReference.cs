using System.Collections.Generic;

namespace Octopus.Build.ConsolidateCalamariPackagesTask
{
    interface IPackageReference
    {
        string Name { get; }
        string Version { get; }
        IReadOnlyList<SourceFile> GetSourceFiles(ILog log);
    }
}
