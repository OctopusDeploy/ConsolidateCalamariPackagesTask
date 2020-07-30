using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using Assent;
using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NSubstitute;
using NUnit.Framework;
using Octopus.Build.ConsolidateCalamariPackagesTask;
using TestStack.BDDfy;

namespace Tests
{
    public class IntegrationTests
    {
        private readonly Configuration assentConfiguration = new Configuration()
            .UsingSanitiser(s => Sanitise4PartVersions(SanitiseHashes(s)));

        private string temp;
        private string expectedZip;
        private MsBuildPackageReference[] packageReferences;
        private bool returnValue;

        public void SetUp()
        {
            temp = Path.GetTempFileName();
            File.Delete(temp);
            Directory.CreateDirectory(temp);
            expectedZip = Path.Combine(temp, $"Calamari.de460c29ca798f9fa26f4924a8f10c1a.zip");
        }

        public void TearDown()
        {
            Directory.Delete(temp, true);
        }

        public void GivenABunchOfPackageReferences()
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(GetCsProjFileName());
            packageReferences = xmlDoc.SelectNodes("Project/ItemGroup/PackageReference")
                .Cast<XmlNode>()
                .Select(n => (packageId: n.Attributes["Include"].Value, version: n.Attributes["Version"].Value))
                .Select(p => new MsBuildPackageReference()
                {
                    Name = p.packageId,
                    Version = p.version,
                    ResolvedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages", p.packageId, p.version)
                })
                .ToArray();
        }



        public void WhenTheTaskIsExecuted()
        {
            var sw = Stopwatch.StartNew();
            var task = new Consolidate(Substitute.For<ILog>());
            task.AssemblyVersion = "1.2.3";
            (returnValue, _) = task.Execute(temp, packageReferences);
            Console.WriteLine($"Time: {sw.ElapsedMilliseconds:n0}ms");
        }

        public void ThenTheReturnValueIsTrue()
            => returnValue.Should().BeTrue();

        public void AndThenThePackageIsCreated()
        {
            Directory.GetFiles(temp).Should().BeEquivalentTo(new[] {expectedZip});
            Console.WriteLine($"Package Size: {new FileInfo(expectedZip).Length / 1024 / 1024}MB");
        }

        public void AndThenThePackageContentsShouldBe()
        {
            using (var zip = ZipFile.Open(expectedZip, ZipArchiveMode.Read))
                this.Assent(string.Join("\r\n", zip.Entries.Select(e => SanitiseHashes(e.FullName)).OrderBy(k => k)), assentConfiguration);
        }

        public void AndThenTheIndexShouldBe()
        {
            using (var zip = ZipFile.Open(expectedZip, ZipArchiveMode.Read))
            using (var entry = zip.Entries.First(e => e.FullName == "index.json").Open())
            using (var sr = new StreamReader(entry))
                this.Assent(sr.ReadToEnd(), assentConfiguration);
        }

        string GetCsProjFileName([CallerFilePath] string callerFilePath = null)
            => Path.Combine(Path.GetDirectoryName(callerFilePath), "Tests.csproj");

        private static string SanitiseHashes(string s)
            => Regex.Replace(s, "[a-z0-9]{32}", "<hash>");

        private static string Sanitise4PartVersions(string s)
            => Regex.Replace(s, @"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+", "<version>");

        [Test]
        public void Execute()
            => this.BDDfy();
    }
}