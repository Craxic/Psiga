﻿/// <summary>
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

		private const string ERR_ATLAS_SIZE = "Atlas has negative size of {0}";

		public List<SubAtlas> Entries { get; protected set; }

		public bool IsReference { get; protected set; }

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

				entry.Entries.Add(new SubAtlas() {
					Parent = entry,
					Name = name,
					Rect = rect,
					TopLeft = topLeft,
					OriginalSize = originalSize,
					ScaleRatio = scaleRatio,
					IsMultiTexture = isMultiTexture,
					IsMip = isMip,
				});
			}

			// Is this a reference to the texture, or the actual thing? If we're reading a manifest, then its always a 
			// reference.
			bool isReference = input.ReadByte() == 0xDD || isManifest;
			entry.IsReference = isReference;
			if (isReference) {
				// Read the name.
				entry.ReferencedTextureName = StreamHelpers.ReadString(input);
			} else {
				entry.IncludedTextureEntry = TextureEntry.Read(input);
			}

			return entry;
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
