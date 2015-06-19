/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.IO;

namespace Microsoft.Xna.Framework.Content
{
	abstract class ContentTypeReader {
		public abstract object Read(BinaryReader reader);
	}
}

