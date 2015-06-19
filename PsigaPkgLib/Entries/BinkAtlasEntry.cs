/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.ComponentModel;

namespace PsigaPkgLib.Entries
{
	public class BinkAtlasEntry : Entry
	{
		private const int BINK_ATLAS_VERSION = 1;

		private const string ERR_BINK_ATLAS_SIZE = "Bink atlas has negative size of {0}";
		private const string ERR_BINK_ATLAS_VERSION = "Bink atlas has version size of {0}, expected {1}";

		public string Name { get; protected set; }
		public int Width { get; protected set; }
		public int Height { get; protected set; }

		private BinkAtlasEntry()
		{
		}

		public static BinkAtlasEntry Read(Stream input)
		{
			// Validate input
			var size = input.ReadInt32BE();
			if (size < 0) {
				throw new EntryReadException(string.Format(ERR_BINK_ATLAS_SIZE, size));
			}
			var binkAtlasVersion = input.ReadInt32BE();
			if (binkAtlasVersion != BINK_ATLAS_VERSION) {
				throw new EntryReadException(string.Format(ERR_BINK_ATLAS_VERSION, size, BINK_ATLAS_VERSION));
			}

			// Read the values we need
			var name = StreamHelpers.ReadString(input);
			var width = input.ReadInt32BE();
			var height = input.ReadInt32BE();

			// Make the entry
			return new BinkAtlasEntry() {
				Name = name,
				Width = width,
				Height = height
			};
		}

		public override EntryType Type { get { return EntryType.BinkAtlas; } }
		public override string DisplayName { get { return Name; } }
	}
}

