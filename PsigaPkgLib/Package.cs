/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PsigaPkgLib.Entries;

namespace PsigaPkgLib
{
	/// <summary>
	/// Represents a package imported from Transistor
	/// </summary>
	public class Package
	{
		private const int READ_BUFFER_MAXIMUM = 0x800000; // Size is important
		private const int COMPRESSION_FLAG = 0x40000000;
		private const int PACKAGE_VERSION_CODE = 5;

		private const string ERR_COMPRESSED_MANIFEST = "Compressed manifests are not allowed (in file {0})";
		private const string ERR_PACKAGE_VERSION = "Package version {0} is not supported (in file {1})";
		private const string ERR_UNKNOWN_MANIFEST_entryType = "Unknown manifest entry type {0}";
		private const string ERR_UNKNOWN_PACKAGE_entryType = "Unknown package entry type {0}";

		/// <summary>
		/// Path to .pkg_manifest file
		/// </summary>
		private string manifestFile;
		private List<Entry> manifestContents;

		/// <summary>
		/// Path to .pkg file
		/// </summary>
		private string packageFile;
		private List<Entry> packageContents;

		/// <summary>
		/// Is the package loaded yet?
		/// </summary>
		private bool isPackageLoaded;

		/// <summary>
		/// These are the entries within the pkg_manifest files.
		/// </summary>
		public IList<Entry> ManifestContents { 
			get 
			{
				EnsureLoaded();
				return manifestContents;
			}
		}

		/// <summary>
		/// These are the entries within the pkg_manifest files.
		/// </summary>
		public IList<Entry> PackageContents { 
			get 
			{
				EnsureLoaded();
				return packageContents;
			}
		}

		public readonly PackageReference PackageReference;
		public readonly PackageReference ManifestReference;

		/// <summary>
		/// Initializes a new instance of the <see cref="PsigaPkgLib.ImportedPackage"/> class.
		/// </summary>
		/// <param name="manifest_file">Manifest file.</param>
		/// <param name="data_file">Data file.</param>
		public Package(string packageName, string manifestFile, string packageFile) {
			PackageReference = new PackageReference(packageName, PackageReference.Files.Package);
			ManifestReference = new PackageReference(packageName, PackageReference.Files.Manifest);
			this.manifestFile = manifestFile;
			this.packageFile = packageFile;
			isPackageLoaded = false;
		}

		/// <summary>
		/// Opens the files and reads the contents of the package.
		/// </summary>
		public void Load() {
			manifestContents = TakeOwnership(ManifestReference, LoadManifest(manifestFile));
			packageContents = TakeOwnership(PackageReference, LoadPackage(packageFile));

			isPackageLoaded = true;
		}

		/// <summary>
		/// Sets the Container property to reference for all Entrys in `entries`.
		/// </summary>
		/// <returns>entries</returns>
		/// <param name="reference">Value for Container property of each entry</param>
		private static List<Entry> TakeOwnership(PackageReference reference, List<Entry> entries) {
			foreach (var entry in entries) {
				entry.Container = reference;
			}
			return entries;
		}

		public static Entry ReadManifestEntry(MemoryStream chunk, out ReadStatus readStatus) {
			int entryType = chunk.ReadByte();
			readStatus = ReadStatus.OK;
			switch (entryType)
			{
			case 0xDE:
				return AtlasEntry.Read(chunk, true);
			case 0xEE:
				return BinkAtlasEntry.Read(chunk);
			case 0xCC:
				return IncludePackageEntry.Read(chunk);
			case 0xBE:
				readStatus = ReadStatus.EndOfChunk;
				return null;
			case (int) byte.MaxValue:
			case -1:
				readStatus = ReadStatus.EndOfFile;
				return null;
			default:
				throw new PackageReadException(string.Format(ERR_UNKNOWN_MANIFEST_entryType, entryType));
			}
		}

		/// <summary>
		/// Opens and loads all contents of the package manifest file.
		/// </summary>
		private static List<Entry> LoadManifest(string manifestFile) {
			// Open the file we're going to read.
			using (var fs = File.OpenRead(manifestFile)) {
				var contents = new List<Entry>(1024);
				var readBuffer = new byte[READ_BUFFER_MAXIMUM];

				// Read the header of the manifest
				int manifestHeader = fs.ReadInt32BE();
				int manifestLength = readBuffer.Length - 4;
				if ((manifestHeader & COMPRESSION_FLAG) != 0) {
					throw new PackageReadException(string.Format(ERR_COMPRESSED_MANIFEST, manifestFile));
				}
				if (manifestHeader != PACKAGE_VERSION_CODE) {
					throw new PackageReadException(string.Format(ERR_PACKAGE_VERSION, manifestHeader, manifestFile));
				}

				// Read all chunks in the file
				ReadStatus readStatus;
				do {
					int chunkSize = fs.Read(readBuffer, 0, manifestLength);
					manifestLength = readBuffer.Length;
					var chunk = new MemoryStream(readBuffer, 0, chunkSize, false);

					// Read all entries in the chunk
					do {
						var e = ReadManifestEntry(chunk, out readStatus);
						if (e != null) {
							contents.Add(e);
						}
					} while (readStatus != ReadStatus.EndOfChunk && readStatus != ReadStatus.EndOfFile);
				} while (readStatus == ReadStatus.EndOfChunk);

				return contents;
			}
		}

		private static void WriteManifest(List<Entry> writeTheseEntries, Stream output) {
		}

		public static Entry ReadPackageEntry(MemoryStream chunk, out ReadStatus readStatus) {
			int entryType = chunk.ReadByte();
			readStatus = ReadStatus.OK;
			switch (entryType)
			{
			case 0xEE:
				return BinkAtlasEntry.Read(chunk);
			case 0xCC:
				return IncludePackageEntry.Read(chunk);
			case 0xDE:
				return AtlasEntry.Read(chunk, false);
			case 0xBB:
				return BinkEntry.Read(chunk);
			case 0xAD:
				return TextureEntry.Read(chunk);
			case 0xBE:
				readStatus = ReadStatus.EndOfChunk;
				return null;
			case (int) byte.MaxValue:
			case -1:
				readStatus = ReadStatus.EndOfFile;
				return null;
			default:
				throw new PackageReadException(string.Format(ERR_UNKNOWN_PACKAGE_entryType, entryType));
			}
		}

		private static List<Entry> LoadPackage(string dataFile) {
			using (var fs = File.OpenRead(dataFile)) {
				var contents = new List<Entry>(1024);
				var readBuffer = new byte[READ_BUFFER_MAXIMUM];

				byte[] compressionBuffer = null;
				bool isCompressed = false;

				// Read the header of the package
				int packageHeader = fs.ReadInt32BE();
				int packageLength = readBuffer.Length - 4;
				if ((packageHeader & COMPRESSION_FLAG) != 0) {
					packageHeader &= ~COMPRESSION_FLAG;
					isCompressed = true;
					compressionBuffer = new byte[8388608];
				}
				if (packageHeader != PACKAGE_VERSION_CODE) {
					throw new PackageReadException(string.Format(ERR_PACKAGE_VERSION, packageHeader, dataFile));
				}

				// Read all chunks in the file
				ReadStatus readStatus;
				do {
					int chunkSize;
					if (isCompressed && fs.ReadByte() != 0) {
						int num2 = fs.ReadInt32BE();
						fs.Read(compressionBuffer, 0, num2);
						var bytes = CLZF2.Decompress(compressionBuffer);
						if (readBuffer.Length < bytes.Length) {
							throw new PackageReadException("Not enough room in read buffer for decompressed chunk.");
						}
						Array.Copy(bytes, readBuffer, bytes.Length);
						chunkSize = bytes.Length;
					} else {
						chunkSize = fs.Read(readBuffer, 0, packageLength);
					}
					packageLength = readBuffer.Length;
					var chunk = new MemoryStream(readBuffer, 0, chunkSize, false);
					do {
						var e = ReadPackageEntry(chunk, out readStatus);
						if (e != null) {
							contents.Add(e);
						}
					} while (readStatus != ReadStatus.EndOfChunk && readStatus != ReadStatus.EndOfFile);
				} while (readStatus == ReadStatus.EndOfChunk);

				return contents;
			}
		}

		private void EnsureLoaded() {
			if (!isPackageLoaded) {
				throw new ApplicationException("Package is not loaded yet!");
			}
		}
	}
}
