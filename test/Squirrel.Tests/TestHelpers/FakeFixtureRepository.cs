﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Squirrel.Tests.TestHelpers
{
    internal class FakeFixtureRepository : Sources.IFileDownloader
    {
        private readonly string _pkgId;
        private readonly IEnumerable<ReleaseEntry> _releases;

        public FakeFixtureRepository(string pkgId, bool mockLatestFullVer)
        {
            _pkgId = pkgId;
            var releases = ReleaseEntry.BuildReleasesFile(IntegrationTestHelper.GetFixturesPath(), false)
                .Where(r => r.OriginalFilename.StartsWith(_pkgId))
                .ToList();

            if (mockLatestFullVer) {
                var minFullVer = releases.Where(r => !r.IsDelta).OrderBy(r => r.Version).First();
                var maxfullVer = releases.Where(r => !r.IsDelta).OrderByDescending(r => r.Version).First();
                var maxDeltaVer = releases.Where(r => r.IsDelta).OrderByDescending(r => r.Version).First();

                // our fixtures don't have a full package for the latest version, we expect the tests to generate this file
                if (maxfullVer.Version < maxDeltaVer.Version) {
                    var name = new ReleaseEntryName(maxfullVer.PackageId, maxDeltaVer.Version, false, maxfullVer.Rid);
                    releases.Add(new ReleaseEntry("0000000000000000000000000000000000000000", name.ToFileName(), maxfullVer.Filesize));
                }
            }

            _releases = releases;
        }

        public Task<byte[]> DownloadBytes(string url, string authorization = null, string accept = null)
        {
            if (url.Contains("/RELEASES?")) {
                MemoryStream ms = new MemoryStream();
                ReleaseEntry.WriteReleaseFile(_releases, ms);
                return Task.FromResult(ms.ToArray());
            }

            var rel = _releases.FirstOrDefault(r => url.EndsWith(r.OriginalFilename));
            var filePath = IntegrationTestHelper.GetPath("fixtures", rel.OriginalFilename);
            if (!File.Exists(filePath)) {
                throw new NotSupportedException("FakeFixtureRepository doesn't have: " + rel.OriginalFilename);
            }

            return Task.FromResult(File.ReadAllBytes(filePath));
        }

        public Task DownloadFile(string url, string targetFile, Action<int> progress, string authorization = null, string accept = null)
        {
            var rel = _releases.FirstOrDefault(r => url.EndsWith(r.OriginalFilename));
            var filePath = IntegrationTestHelper.GetPath("fixtures", rel.OriginalFilename);
            if (!File.Exists(filePath)) {
                throw new NotSupportedException("FakeFixtureRepository doesn't have: " + rel.OriginalFilename);
            }

            File.Copy(filePath, targetFile);
            progress(25);
            progress(50);
            progress(75);
            progress(100);
            return Task.CompletedTask;
        }

        public Task<string> DownloadString(string url, string authorization = null, string accept = null)
        {
            if (!url.Contains("/RELEASES?")) {
                throw new NotImplementedException();
            }
            MemoryStream ms = new MemoryStream();
            ReleaseEntry.WriteReleaseFile(_releases, ms);
            return Task.FromResult(Encoding.UTF8.GetString(ms.ToArray()));
        }
    }
}
