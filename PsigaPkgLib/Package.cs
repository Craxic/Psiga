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
		private const int CHUNK_SIZE = 0x800000;
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

		public const byte BINK_ATLAS_CODE = 0xEE;
		public const byte INCLUDE_PACKAGE_CODE = 0xCC;
		public const byte ATLAS_CODE = 0xDE;
		public const byte BINK_CODE = 0xBB;
		public const byte TEXTURE_CODE = 0xAD;
		public const byte END_CHUNK_CODE = 0xBE;

		private static byte GetCodeFromType(EntryType type) {
			switch (type) {
			case EntryType.BinkAtlas:
				return BINK_ATLAS_CODE;
			case EntryType.IncludePackage:
				return INCLUDE_PACKAGE_CODE;
			case EntryType.Atlas:
				return ATLAS_CODE;
			case EntryType.Bink:
				return BINK_CODE;
			case EntryType.Texture:
				return TEXTURE_CODE;
			}
			return END_CHUNK_CODE;
		}

		public static Entry ReadManifestEntry(MemoryStream chunk, out ReadStatus readStatus) {
			int entryType = chunk.ReadByte();
			readStatus = ReadStatus.OK;
			switch (entryType)
			{
			case ATLAS_CODE:
				return AtlasEntry.Read(chunk, true);
			case BINK_ATLAS_CODE:
				return BinkAtlasEntry.Read(chunk);
			case INCLUDE_PACKAGE_CODE:
				return IncludePackageEntry.Read(chunk);
			case END_CHUNK_CODE:
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
				var readBuffer = new byte[CHUNK_SIZE];

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

		private static List<byte[]> MakeEntriesIntoBytes(IList<Entry> writeTheseEntries) {
			List<byte[]> entries = new List<byte[]>();

			// Get every entry in the manifest and turn it into bytes.
			foreach (var e in writeTheseEntries) {
				using (var ms = new MemoryStream()) {
					// Write the type code
					ms.WriteByte(GetCodeFromType(e.Type));

					// Write the data.
					e.WriteTo(ms);

					entries.Add(ms.ToArray());
				}
			}

			return entries;
		}

		private static List<byte[]> MakeByteEntriesIntoChunks(List<byte[]> entries) {
			var retn = new List<byte[]>();
			int chunkId = 0;
			int entryId = 0;
			while (true) {
				// For some reason, the first chunk is 4 bytes smaller than all the others.
				int chunk_size = chunkId == 0 ? CHUNK_SIZE - 4 : CHUNK_SIZE;

				// Current writing position in chunk
				int chunkIndex = 0;
				var chunk = new byte[chunk_size];

				// For every entry left to add...
				while (entryId < entries.Count) {
					// There are still entries left that must be added.
					if (chunk.Length - chunkIndex < entries[entryId].Length + 1) {
						// No room for this entry, time to end the chunk.
						if (chunk.Length == chunkIndex) {
							throw new ApplicationException("should not occur");
						}
						chunk[chunkIndex] = END_CHUNK_CODE;
						chunkIndex++;
						break;
					} else {
						// Add the entry into the chunk
						Array.Copy(entries[entryId], 0, chunk, chunkIndex, entries[entryId].Length);
						chunkIndex += entries[entryId].Length;
						entryId++;
					}
				}

				// Are we done adding entries?
				if (entryId == entries.Count) {
					// There are no more entries! Time to end the file.
					if (chunk.Length == chunkIndex) {
						throw new ApplicationException("should not occur");
					}

					// Write the end of file byte.
					chunk[chunkIndex] = (byte)byte.MaxValue;
					chunkIndex++;

					// Clip the byte array
					var newChunk = new byte[chunkIndex];
					Array.Copy(chunk, newChunk, chunkIndex);
					retn.Add(newChunk);
					chunkId++;
					break;
				} else {
					// Add the chunks to the list of chunks
					retn.Add(chunk);
					chunkId++;
				}
			}
			return retn;
		}

		private static void AssertOnChunks(List<byte[]> chunks) {
			for (int i = 0; i < chunks.Count; i++) {
				if (i == chunks.Count - 1) {
					if (chunks[i].Length >= CHUNK_SIZE) {
						throw new ApplicationException("Chunk not correct size");
					}
				} else if (i == 0) {
					if (chunks[i].Length != CHUNK_SIZE - 4) {
						throw new ApplicationException("Chunk not correct size");
					}
				} else {
					if (chunks[i].Length != CHUNK_SIZE) {
						throw new ApplicationException("Chunk not correct size");
					}
				}
			}
		}

		private static byte[] MakeChunksIntoPackage(List<byte[]> chunks, bool compressed) {
			AssertOnChunks(chunks);
			using (MemoryStream ms = new MemoryStream(chunks.Count * CHUNK_SIZE)) {
				int headerCode = PACKAGE_VERSION_CODE;
				if (compressed) {
					headerCode |= COMPRESSION_FLAG;
				}
				ms.WriteInt32BE(headerCode);

				if (compressed) {
					var compressionBuffer = new byte[8388608];
					var compressor = new LZF();
					foreach (var chunk in chunks) {
						int compressedSize = compressor.Compress(chunk, chunk.Length, compressionBuffer, compressionBuffer.Length);
						if (compressedSize != 0) {
							ms.WriteByte(1); // Compressed.
							ms.WriteInt32BE(compressedSize);
							ms.Write(compressionBuffer, 0, compressedSize);
						} else {
							ms.WriteByte(0); // Not compressed.
							ms.Write(chunk, 0, chunk.Length);
						}
					}
				} else {
					foreach (var chunk in chunks) {
						ms.Write(chunk, 0, chunk.Length);
					}
				}

				return ms.ToArray();
			}
		}

		public static byte[] CreatePackageFile(IList<Entry> writeTheseEntries, bool compressed) {
			var entryBytes = MakeEntriesIntoBytes(writeTheseEntries);
			var chunks = MakeByteEntriesIntoChunks(entryBytes);
			return MakeChunksIntoPackage(chunks, compressed);
		}

		public byte[] CreatePackageFile() {
			return CreatePackageFile(PackageContents, true);
		}

		public byte[] CreateManifestFile() {
			return CreatePackageFile(ManifestContents, false);
		}

		public void WritePackageFiles(string filenameBase) {
			File.WriteAllBytes(filenameBase + ".pkg", CreatePackageFile());
			File.WriteAllBytes(filenameBase + ".pkg_manifest", CreateManifestFile());
		}

		public static Entry ReadPackageEntry(MemoryStream chunk, out ReadStatus readStatus) {
			int entryType = chunk.ReadByte();
			readStatus = ReadStatus.OK;
			switch (entryType)
			{
			case BINK_ATLAS_CODE:
				return BinkAtlasEntry.Read(chunk);
			case INCLUDE_PACKAGE_CODE:
				return IncludePackageEntry.Read(chunk);
			case ATLAS_CODE:
				return AtlasEntry.Read(chunk, false);
			case BINK_CODE:
				return BinkEntry.Read(chunk);
			case TEXTURE_CODE:
				return TextureEntry.Read(chunk);
			case END_CHUNK_CODE:
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
				var readBuffer = new byte[CHUNK_SIZE];

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

				LZF decompressor = new LZF();
				// Read all chunks in the file
				ReadStatus readStatus;
				do {
					int chunkSize;
					if (isCompressed && fs.ReadByte() != 0) {
						int num2 = fs.ReadInt32BE();
						fs.Read(compressionBuffer, 0, num2);
						chunkSize = decompressor.Decompress(compressionBuffer, num2, readBuffer, readBuffer.Length);
					} else {
						chunkSize = fs.Read(readBuffer, 0, packageLength);
					}
					byte[] test = new byte[chunkSize];
					Array.Copy(readBuffer, test, chunkSize);
					File.WriteAllBytes("test", test);
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
