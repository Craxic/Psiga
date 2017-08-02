/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using PsigaPkgLib.Entries.Atlas;
using System.ComponentModel;

namespace PsigaPkgLib.Entries
{
	public class AtlasEntry : Entry
	{
		private const int NEW_ATLAS_VERSION_MAGIC = 2142336875;
		private const int IS_MULTI_TEXTURE_FLAG = 0x1;
		private const int IS_MIP_FLAG = 0x2;
		private const int REFERENCE_CODE = 0xDD;
		private const int INLINE_TEXTURE_CODE = 0x0; // Not sure what this value is actually, Transistor doesn't specify.

		private const string ERR_ATLAS_SIZE = "Atlas has negative size of {0}";

		public List<SubAtlas> Entries { get; protected set; }

		public bool IsReference { get; protected set; }
		public int VersionCode { get; protected set; }

		public TextureEntry IncludedTextureEntry { get; protected set; }
		public string ReferencedTextureName { get; protected set; }

		private AtlasEntry()
		{
		}

		public static AtlasEntry Read(Stream input, bool isManifest)
		{
			// Validate input
			var size = input.ReadInt32BE();
			if (size < 0) {
				throw new EntryReadException(string.Format(ERR_ATLAS_SIZE, size));
			}

			// Read the header of the atlas.
			int atlasVersionCode = 0;
			int numSubAtlases = input.ReadInt32BE();
			if (numSubAtlases == NEW_ATLAS_VERSION_MAGIC)
			{
				// New texture atlas format: Ignore the first integer
				atlasVersionCode = input.ReadInt32BE();
				numSubAtlases = input.ReadInt32BE();
			}

			var entry = new AtlasEntry();
			entry.Entries = new List<SubAtlas>();
			entry.VersionCode = atlasVersionCode;

			// Read all sub atlases.
			for (int i = 0; i < numSubAtlases; i++) {
				string name = StreamHelpers.ReadString(input);
				Rectangle rect = new Rectangle(input.ReadInt32BE(), input.ReadInt32BE(), 
					                           input.ReadInt32BE(), input.ReadInt32BE());
				Point topLeft = new Point(input.ReadInt32BE(), input.ReadInt32BE());
				Point originalSize = new Point(input.ReadInt32BE(), input.ReadInt32BE());
				Vector2 scaleRatio = new Vector2(input.ReadSingleBE(), input.ReadSingleBE());
				bool isMultiTexture = false;
				bool isMip = false;
				if (atlasVersionCode > 0) {
					int atlasType = input.ReadByte();
					if (atlasVersionCode > 1) {
						isMultiTexture = (atlasType & IS_MULTI_TEXTURE_FLAG) != 0;
						isMip = (atlasType & IS_MIP_FLAG) != 0;
					} else {
						isMultiTexture = atlasType != 0;
					}
				}
				List<IntVector2> hull = null;
				if (atlasVersionCode > 2)
				{
					int hullCount = input.ReadInt32BE();
					hull = new List<IntVector2>();
					for (int j = 0; j < hullCount; j++)
					{
						int num10 = input.ReadInt32BE();
						int num11 = input.ReadInt32BE();
						hull.Add(new IntVector2(num10, num11));
					}
				}

				entry.Entries.Add(new SubAtlas() {
					Parent = entry,
					Name = name,
					Rect = rect,
					TopLeft = topLeft,
					OriginalSize = originalSize,
					ScaleRatio = scaleRatio,
					IsMultiTexture = isMultiTexture,
					IsMip = isMip,
					Hull = hull,
				});
			}

			// Is this a reference to the texture, or the actual thing? If we're reading a manifest, then its always a 
			// reference.
			//byte unkByte = (byte)input.ReadByte();
			byte refByte = (byte)input.ReadByte();
			bool isReference = refByte == REFERENCE_CODE || isManifest;
			entry.IsReference = isReference;
			if (isReference) {
				// Read the name.
				entry.ReferencedTextureName = StreamHelpers.ReadString(input);
			} else {
				entry.IncludedTextureEntry = TextureEntry.Read(input);
			}

			return entry;
		}

		public override void WriteTo(Stream s)
		{
			using (MemoryStream ms = new MemoryStream()) {
				// Use the new version format
				ms.WriteInt32BE(NEW_ATLAS_VERSION_MAGIC);

				// Write the newest version code (2)
				ms.WriteInt32BE(VersionCode);

				// Write the number of SubAtlases
				ms.WriteInt32BE(Entries.Count);

				foreach (var entry in Entries) {
					// Write the name
					ms.WriteString(entry.Name);

					// Write the rectangle
					ms.WriteInt32BE(entry.Rect.X);
					ms.WriteInt32BE(entry.Rect.Y);
					ms.WriteInt32BE(entry.Rect.Width);
					ms.WriteInt32BE(entry.Rect.Height);

					// Write the top left
					ms.WriteInt32BE(entry.TopLeft.X);
					ms.WriteInt32BE(entry.TopLeft.Y);

					// Write the original size
					ms.WriteInt32BE(entry.OriginalSize.X);
					ms.WriteInt32BE(entry.OriginalSize.Y);

					// Write the scale ratio
					ms.WriteSingleBE(entry.ScaleRatio.X);
					ms.WriteSingleBE(entry.ScaleRatio.Y);

					// Write the hull. I'm not gonna touch this data.
					if (VersionCode > 2)
					{
						var hullCount = entry.Hull.Count;
						ms.WriteInt32BE(hullCount);
						foreach (IntVector2 jj in entry.Hull)
						{
							ms.WriteInt32BE(jj.X);
							ms.WriteInt32BE(jj.Y);
						}
					}

					// Write the byte for the flags
					ms.WriteByte((byte)((entry.IsMip ? IS_MIP_FLAG : 0) | (entry.IsMultiTexture ? IS_MULTI_TEXTURE_FLAG : 0)));
				}

				ms.WriteByte((byte)(IsReference ? REFERENCE_CODE : INLINE_TEXTURE_CODE));

				// Read the name.
				if (IsReference) {
					ms.WriteString(ReferencedTextureName);
				} else {
					IncludedTextureEntry.WriteTo(ms);
				}

				var bytes = ms.ToArray();
				// The size is wrong. Calculation by SuperGiant's tooling
				// might be wrong? Transistor ignores it regardless.
				s.WriteInt32BE(bytes.Length - 35); 
				s.Write(bytes, 0, bytes.Length);
			}
		}

		public override EntryType Type { get { return EntryType.Atlas; } }

		public override string DisplayName { 
			get { 
				return IsReference ? 
					"<ref " + ReferencedTextureName + ">" : 
					"<included " + IncludedTextureEntry.Name + ">"; 
			} 
		}
	}
}

