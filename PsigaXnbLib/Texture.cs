/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using Microsoft.Xna.Framework.Graphics;
using System.ComponentModel;

namespace PsigaXnbLib
{
	public class Texture
	{
		public SurfaceFormat Format { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }

		private byte[] Data { get; set; }
		private byte[] DecompressedData { get; set; }

		[EditorBrowsableAttribute(EditorBrowsableState.Never)]
		public bool IsDecompressed { get { return DecompressedData != null; } }

		public Texture(SurfaceFormat format, int width, int height, byte[] data)
		{
			Width = width;
			Height = height;
			Data = data;
			Format = format;
		}

		private byte[] GetDecompressedRGBA32() {
			byte[] data;
			lock (this) {
				data = Data;
			}
			if (data == null) {
				return null;
			}
			if (Format == SurfaceFormat.Dxt5) {
				return DxtUtil.DecompressDxt5(data, Width, Height);
			} else if (Format == SurfaceFormat.Color) {
				return data;
			} else {
				throw new PsigaShimUnsupported();
			}
		}

		public byte[] GetDecompressedARGB32PreMul() {
			byte[] decompressed = null;
			lock (this) {
				decompressed = DecompressedData;
			}
			if (decompressed == null) {
				byte[] retn = GetDecompressedRGBA32();

				for (int y = 0; y < Height; y++) {
					for (int x = 0; x < Width; x++) {
						int loc = (y * Width + x) * 4;
						byte r = retn[loc];
						byte g = retn[loc + 1];
						byte b = retn[loc + 2];
						byte a = retn[loc + 3];

						r = (byte)(((int)r * (int)a) / 255);
						g = (byte)(((int)g * (int)a) / 255);
						b = (byte)(((int)b * (int)a) / 255);

						retn[loc] = b;
						retn[loc + 1] = g;
						retn[loc + 2] = r;
						retn[loc + 3] = a;
					}
				}

				decompressed = retn;
				lock (this) {
					DecompressedData = decompressed;
					Data = null;
				}
			}

			return decompressed;
		}
	}
}

