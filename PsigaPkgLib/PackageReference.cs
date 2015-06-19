/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;

namespace PsigaPkgLib
{
	public class PackageReference
	{
		public enum Files {
			Both,
			Manifest,
			Package
		}

		public readonly string Name;
		public readonly Files ReferencedFiles;

		public string DisplayName {
			get {
				if (ReferencedFiles == Files.Both) {
					return Name;
				} else if (ReferencedFiles == Files.Manifest) {
					return Name + ".pkg_manifest";
				} else if (ReferencedFiles == Files.Package) {
					return Name + ".pkg";
				}
				return Name;
			}
		}

		public PackageReference(string name, Files referencedFiles)
		{
			Name = name;
			ReferencedFiles = referencedFiles;
		}
	}
}

