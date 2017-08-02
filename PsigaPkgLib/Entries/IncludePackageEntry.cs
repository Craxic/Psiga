/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.IO;
using System.Collections.Generic;

namespace PsigaPkgLib.Entries
{
	public class IncludePackageEntry : Entry
	{
		public string PackageName { get; private set; }

		public IncludePackageEntry(string packageName)
		{
			PackageName = packageName;
		}

		public static IncludePackageEntry Read(Stream input)
		{
			return new IncludePackageEntry(StreamHelpers.ReadString(input));
		}

		public override void WriteTo(Stream s)
		{
			s.WriteString(DisplayName);
		}

		public override EntryType Type { get { return EntryType.IncludePackage; } }

		public override string DisplayName { get { return PackageName; } }
	}
}

