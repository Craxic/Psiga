/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using PsigaXnbLib;
using Microsoft.Xna.Framework.Content;

namespace PsigaXnbLib
{
	class Texture3DRipperReader : ContentTypeReader {
		public override object Read(BinaryReader reader)
		{
			int formatInt = reader.ReadInt32();
			SurfaceFormat pixelFormat = (SurfaceFormat)formatInt;
			int width = reader.ReadInt32();
			int height = reader.ReadInt32();
			int depth = reader.ReadInt32();
			int num = reader.ReadInt32();
			byte[] array = new byte[num];
			int num2 = reader.Read(array, 0, num);
			return new Texture3D(pixelFormat, width, height, depth, array);
		}
	}
}
