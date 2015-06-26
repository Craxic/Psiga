/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.ComponentModel;
using System.IO;

namespace PsigaPkgLib.Entries
{
	public abstract class Entry
	{
		[EditorBrowsableAttribute(EditorBrowsableState.Never)]
		public abstract EntryType Type { get; }
		[EditorBrowsableAttribute(EditorBrowsableState.Never)]
		public abstract string DisplayName { get; }
		[EditorBrowsableAttribute(EditorBrowsableState.Never)]
		public PackageReference Container { get; set; }

		public abstract void WriteTo(Stream s);
	}
}

