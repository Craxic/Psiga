/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.IO;
using System.ComponentModel;

namespace PsigaPkgLib.Entries
{
	public class BinkEntry : Entry
	{
		public bool IsAlpha { get; protected set; }
		public string BinkFileName { get; protected set; }

		private BinkEntry()
		{
		}

		public static BinkEntry Read(Stream input)
		{
			var isAlpha = input.ReadByte() == 1;
			var binkFileName = input.ReadString();

			return new BinkEntry() {
				IsAlpha = isAlpha,
				BinkFileName = binkFileName
			};
		}

		public override void WriteTo(Stream s)
		{
			s.WriteByte((byte)(IsAlpha ? 1 : 0));
			s.WriteString(BinkFileName);
		}

		public override EntryType Type { get { return EntryType.Bink; } }
		public override string DisplayName { get { return BinkFileName; } }
	}
}

