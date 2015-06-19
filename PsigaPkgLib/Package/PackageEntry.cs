using System;
using System.IO;

namespace PsigaPkgLib.Package
{
	public class PackageEntry
	{
		private const string ERR_UNKNOWN_MANIFEST_TYPE = "Unknown manifest type {0}";

		public string Name { get; protected set; }

	}
}

