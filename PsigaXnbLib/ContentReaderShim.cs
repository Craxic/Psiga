// MIT License - Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Utilities;
using PsigaXnbLib;

namespace Microsoft.Xna.Framework.Content
{
	public sealed class ContentReaderShim : BinaryReader
	{
		public int Version { get; private set; }

		internal ContentReaderShim(Stream stream, int version) : base(stream)
		{
			Version = version;
		}

		public object ReadAsset<T>()
		{
			if (Version < 6)
			{
				// Transistor: Skip old unused resource loader deets. Used to call ContentReaderManager here.
				int num = Read7BitEncodedInt();
				for (int i = 0; i < num; i++)
				{
					ReadString();
					ReadInt32();
				}
				Read7BitEncodedInt();
				Read7BitEncodedInt();

				if (typeof(T) != typeof(Texture))
				{
					throw new Exception("Only textures are supported for the version 5 format.");
				}
			}

			// Use the content type reader for the requested type directly.
			return ContentTypeReaderManager.GetTypeReader(typeof(T)).Read(this);
		}
	}
}
