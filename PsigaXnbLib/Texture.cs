/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using Microsoft.Xna.Framework.Graphics;
using System.ComponentModel;
using System.IO;
using LibSquishDxt5;

namespace PsigaXnbLib
{
	public class Texture
	{
		/*
			Format name                                        | Red (byte[] order)
			---------------------------------------------------+--------------------
			System.Drawing.Imaging.PixelFormat.Format32bppArgb | 00 00 FF FF (B G R A)
			ImageSurface.Format.Argb32                         | 00 00 FF FF (B G R A)
			DxtUtil.DecompressDxt5                             | FF 00 00 FF (R G B A)
			Squish                                             | FF 00 00 FF (R G B A)
		*/

		public SurfaceFormat Format { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }

		[EditorBrowsableAttribute(EditorBrowsableState.Never)]
		public byte[] RGBAData { get; set; }

		public Texture(SurfaceFormat format, int width, int height, byte[] data)
		{
			Width = width;
			Height = height;
			Format = format;
			if (Format == SurfaceFormat.Color) {
				RGBAData = data;
			} else if (Format == SurfaceFormat.Dxt5) {
				RGBAData = DxtUtil.DecompressDxt5(data, Width, Height);
			} else {
				throw new PsigaShimUnsupported();
			}
		}

		public static void BGRAtoRGBA(byte[] input) {
			for (int i = 0; i < input.Length; i += 4) {
				var b = input[i];
				var g = input[i + 1];
				var r = input[i + 2];
				var a = input[i + 3];
				input[i] = r;
				input[i + 1] = g;
				input[i + 2] = b;
				input[i + 3] = a;
			}
		}

		private unsafe byte[] CompressToDxt5() {
			var retn = new byte[LibSquish.dxt5_size(Width, Height)];
			fixed (byte * dataUCFixed = RGBAData) {
				fixed (byte * retnFixed = retn) {
					LibSquish.compress_dxt5((IntPtr)dataUCFixed, Width, Height, (IntPtr)retnFixed);
				}
			}
			return retn;
		}

		private string TYPE_READER_STRING = "Microsoft.Xna.Framework.Content.Texture2DReader";
		private byte[] CreateXnbInner() {
			using (MemoryStream ms = new MemoryStream()) {
				BinaryWriter msw = new BinaryWriter(ms);
				ms.WriteByte(1); // num ContentTypeReaders
				msw.Write(TYPE_READER_STRING);
				msw.Write((int)0);
				ms.WriteByte(0); // sharedResourceCount == 0
				ms.WriteByte(1); // typeReaderIndex == 1

				msw.Write((int)Format);
				msw.Write((int)Width);
				msw.Write((int)Height);
				msw.Write((int)1); // Mip level.
				byte[] textureData;
				if (Format == SurfaceFormat.Dxt5) {
					textureData = CompressToDxt5();
				} else if (Format == SurfaceFormat.Color) {
					textureData = RGBAData;
				} else {
					throw new PsigaShimUnsupported();
				}
				msw.Write((int)textureData.Length);
				ms.Write(textureData, 0, textureData.Length);

				return ms.ToArray();
			}
		}

		public byte[] CreateXnb() {
			using (MemoryStream ms = new MemoryStream()) {
				BinaryWriter msw = new BinaryWriter(ms);
				ms.WriteByte((byte)'X'); // magic
				ms.WriteByte((byte)'N');
				ms.WriteByte((byte)'B');
				ms.WriteByte((byte)'w'); // platform code
				ms.WriteByte((byte)5); // version id
				ms.WriteByte((byte)0); // flags

				byte[] inner = CreateXnbInner();
				msw.Write((int)inner.Length + 10); // Plus 10 for header
				ms.Write(inner, 0, inner.Length);
				return ms.ToArray();
			}
		}
	}
}

