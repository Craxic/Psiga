/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using PsigaPkgLib;
using PsigaPkgLib.Entries;

namespace PsigaPkgLib
{
	public class EntryReference
	{
		public readonly string Name;
		public readonly int EntryIndex;
		public readonly EntryType Type;
		public readonly PackageReference ContainingPackage;

		public EntryReference(int entryIndex, PackageReference reference)
		{
			if (reference.ReferencedFiles == PackageReference.Files.Both) {
				throw new ArgumentException("reference can't be Both");
			}
			ContainingPackage = reference;
			EntryIndex = entryIndex;

			var entry = Dereference();

			Name = entry.DisplayName;
			Type = entry.Type;
		}

		public Entry Dereference() {
			Package package;
			if (!PackageManager.LoadedPackages.TryGetValue(ContainingPackage.Name, out package)) {
				throw new ArgumentException("package not loaded");
			}

			if (ContainingPackage.ReferencedFiles == PackageReference.Files.Manifest) {
				return package.ManifestContents[EntryIndex];
			} else {
				return package.PackageContents[EntryIndex];
			}
		}
	}
}

