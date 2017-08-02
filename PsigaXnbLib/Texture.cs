/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using Microsoft.Xna.Framework.Graphics;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
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

        private byte[] Data { get; set; }

		//[EditorBrowsableAttribute(EditorBrowsableState.Never)]
		//public byte[] RGBAData { get; set; }
		
		// private static int id = 0;

		public Texture(SurfaceFormat format, int width, int height, byte[] data)
		{
			Width = width;
			Height = height;
			Format = format;
		    Data = data;
		}

	    public byte[] DecompressData()
	    {
	        if (Format == SurfaceFormat.Alpha8)
	        {
	            return Alpha8ToRgba(Data);
	        }
	        else if (Format == SurfaceFormat.Color)
	        {
	            return Data;
	        }
	        else if (Format == SurfaceFormat.Dxt5)
	        {
	            return DxtUtil.DecompressDxt5(Data, Width, Height);
	        }
	        else if (Format == (SurfaceFormat)28)
	        {
	            return DetexCS.DetexCS.DecodeBc7(Data, Width, Height);
	        }
	        else if (Format == (SurfaceFormat)27)
	        {
	            return LumAlpha8ToRgba(Data);
	        }
	        else
	        {
	            throw new PsigaShimUnsupported();
	        }
        }

		private static byte[] LumAlpha8ToRgba(byte[] data)
		{
			if (data.Length % 2 != 0)
			{
				throw new PsigaShimUnsupported();
			}
			byte[] rgba = new byte[data.Length * 4];
			for (int p = 0; p < data.Length/2; p++)
			{
				rgba[p * 4 + 0] = data[p*2];
				rgba[p * 4 + 1] = data[p*2];
				rgba[p * 4 + 2] = data[p*2];
				rgba[p * 4 + 3] = data[p*2+1];
			}
			return rgba;
		}

		private static byte[] Alpha8ToRgba(byte[] data)
		{
			byte[] rgba = new byte[data.Length * 4];
			for (int p = 0; p < data.Length; p++)
			{
				rgba[p * 4 + 3] = data[p];
			}
			return rgba;
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

        private static unsafe byte[] CompressToDxt5(int w, int h, byte[] rgba) {
			var retn = new byte[LibSquish.dxt5_size(w, h)];
			fixed (byte * dataUCFixed = rgba) {
				fixed (byte * retnFixed = retn) {
					LibSquish.compress_dxt5((IntPtr)dataUCFixed, w, h, (IntPtr)retnFixed);
				}
			}
			return retn;
		}

	    public void SetRgbaData(byte[] newData)
	    {
	        if (Format == SurfaceFormat.Color)
	        {
	            Data = newData;
	        }
	        else if (Format == SurfaceFormat.Dxt5)
	        {
	            Data = CompressToDxt5(Width, Height, newData);
	        }
	        else
	        {
	            throw new PsigaShimUnsupported();
	        }
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
				byte[] textureData = Data;
				/*if (Format == SurfaceFormat.Dxt5) {
					textureData = CompressToDxt5();
				} else if (Format == SurfaceFormat.Color) {
					textureData = RGBAData;
				} else {
					throw new PsigaShimUnsupported();
				}*/
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

