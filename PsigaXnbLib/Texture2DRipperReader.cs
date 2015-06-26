/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using PsigaXnbLib;
using Microsoft.Xna.Framework.Content;

namespace PsigaXnbLib
{
	class Texture2DRipperReader : ContentTypeReader {
		public override object Read(BinaryReader reader)
		{
			SurfaceFormat pixelFormat = (SurfaceFormat)reader.ReadInt32();
			int width = reader.ReadInt32();
			int height = reader.ReadInt32();
			int levelCount = reader.ReadInt32();
			if (levelCount != 1) {
				throw new PsigaShimUnsupported();
			}
			int num = reader.ReadInt32();
			byte[] array = new byte[num];
			int num2 = reader.Read(array, 0, num);
			return new Texture(pixelFormat, width, height, array);
		}
	}
}
