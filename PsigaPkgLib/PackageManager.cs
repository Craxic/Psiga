/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.Collections.Generic;
using PsigaPkgLib;
using System.IO;
using System.Linq;
using System.Threading;
using PsigaXnbLib;
using PsigaPkgLib.Entries;

namespace PsigaPkgLib
{
	public static class PackageManager
	{
		public static event Action<string, Package> OnPackageLoad;
		public static event Action OnRootChanged;

		public static readonly object Lock = new object();
		public static string RootDirectory { get; private set; }
		public static List<string> Packages { get; private set; }
		public static Dictionary<string, Package> LoadedPackages { get; private set; }
		public static Dictionary<string, long> PackageSizes { get; private set; }

		public static Dictionary<string, EntryReference> TextureEntries { get; private set; }

		private const string PKG_EXT = ".pkg";
		private const string PKG_MANIFEST_EXT = ".pkg_manifest";

		public delegate void AsyncLoadPackagesCallback(string packageLoaded, double progress);

		static PackageManager() {
			Packages = new List<string>();
			LoadedPackages = new Dictionary<string, Package>();
			TextureEntries = new Dictionary<string, EntryReference>();
		}

		public static void SetTransistorRoot(string rootDirectory) {
			lock (Lock) {
				RootDirectory = rootDirectory;
				Packages.Clear();
				LoadedPackages.Clear();

				var files = Directory.EnumerateFiles(rootDirectory);
				var packageFiles = files.Where(x => Path.GetExtension(x) == PKG_EXT)
				                     .Select(Path.GetFileNameWithoutExtension);
				var manifestFiles = files.Where(x => Path.GetExtension(x) == PKG_MANIFEST_EXT)
				                      .Select(Path.GetFileNameWithoutExtension);

				Packages = packageFiles.Union(manifestFiles).ToList();
				Packages.Sort();

				PackageSizes = new Dictionary<string, long>();
				foreach (var name in Packages) {
					var package = Path.Combine(rootDirectory, name + PKG_EXT);
					var manifest = Path.Combine(rootDirectory, name + PKG_MANIFEST_EXT);

					PackageSizes[name] = (new FileInfo(package)).Length + (new FileInfo(manifest)).Length;
				}

				if (OnRootChanged != null)
					OnRootChanged();
			}
		}

		private static void FindTextures(PackageReference pr, IList<Entry> contents) {
			for (int i = 0; i < contents.Count; i++) {
				TextureEntry te = contents[i] as TextureEntry;
				if (te != null) {
					if (TextureEntries.ContainsKey(te.Name)) {
						Console.WriteLine("Duplicate Texture entry: " + te.Name + " in " + pr.DisplayName + " and " + TextureEntries[te.Name].ContainingPackage.DisplayName);
					} else {
						TextureEntries.Add(te.Name, new EntryReference(i, pr));
					}
				}
			}
		}

		public static Package LoadPackage(string packageName) {
			var baseFile = Path.Combine(RootDirectory, packageName);
			var package = new Package(packageName, baseFile + PKG_MANIFEST_EXT, baseFile + PKG_EXT);
			package.Load();

			lock (Lock) {
				LoadedPackages.Add(packageName, package);

				FindTextures(new PackageReference(packageName, PackageReference.Files.Manifest), package.ManifestContents);
				FindTextures(new PackageReference(packageName, PackageReference.Files.Package), package.PackageContents);
			}
			if (OnPackageLoad != null) {
				OnPackageLoad(packageName, package);
			}
			return package;
		}

		public static void ReleasePackage(string packageName) {
			lock (Lock) {
				LoadedPackages.Remove(packageName);
			}
			GC.Collect();
		}

		public static long GetPackageSize(string packageName) {
			lock (Lock) {
				return PackageSizes[packageName];
			}
		}

		public static bool IsLoaded(string packageName) {
			lock (Lock) {
				return (LoadedPackages.ContainsKey(packageName) && LoadedPackages[packageName] != null);
			}
		}

		public static void AsyncLoadPackages(HashSet<string> packageNames, AsyncLoadPackagesCallback cb) {
			(new Thread(() => {
				int i = 0;

				// Load each package
				foreach (var package in packageNames) {
					double fraction = i / (double)packageNames.Count;
					cb(package, fraction);
					if (!PackageManager.IsLoaded(package)) {
						PackageManager.LoadPackage(package);
					}
					i++;
				}

				// Clear loading status
				cb("", 1);
			})).Start();
		}

		public static string GetTexturePackageName(string textureName) {
			if (textureName == null) {
				return null;
			}
			lock (Lock) {
				if (TextureEntries.ContainsKey(textureName)) {
					return TextureEntries[textureName].ContainingPackage.DisplayName;
				}
			}
			return null;
		}
	}
}

