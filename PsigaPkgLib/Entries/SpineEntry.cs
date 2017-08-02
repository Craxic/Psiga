/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.IO;
using System.ComponentModel;

namespace PsigaPkgLib.Entries
{
	public class SpineEntry : Entry
	{
		public string Path { get; protected set; }
		public string SpineAtlas { get; protected set; }
		public string SpineData { get; protected set; }

		private SpineEntry()
		{
		}

		public static SpineEntry Read(Stream input)
		{
			var versionCode = input.ReadByte();
			if (versionCode != 0)
			{
				throw new EntryReadException("Unsupported spine version: " + versionCode);
			}

			string path = input.ReadString();
			var spineAtlas = input.ReadBigString();
			var spineData = input.ReadBigString();

			return new SpineEntry() {
				Path = path,
				SpineAtlas = spineAtlas,
				SpineData = spineData
			};
		}

		public override void WriteTo(Stream s)
		{
			s.WriteByte(0);
			s.WriteString(Path);
			s.WriteBigString(SpineAtlas);
			s.WriteBigString(SpineData);
		}

		public override EntryType Type { get { return EntryType.Spine; } }
		public override string DisplayName { get { return "Spine (Path=" + Path + ")"; } }
	}
}

