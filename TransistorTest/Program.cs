// LICENCE: A lot of this was decompiled from https://github.com/mono/MonoGame so its licened under the Microsoft Public License (Ms-PL). 
// Everything else: http://www.wtfpl.net/txt/copying/

using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace TransistorTest
{
	class DDSWriter {
		const uint DDSD_CAPS 		= 0x1;		// Required in every .dds file.
		const uint DDSD_HEIGHT		= 0x2;		// Required in every .dds file.
		const uint DDSD_WIDTH 		= 0x4;		// Required in every .dds file.
		const uint DDSD_PITCH 		= 0x8;		// Required when pitch is provided for an uncompressed texture.
		const uint DDSD_PIXELFORMAT = 0x1000;	// Required in every .dds file.
		const uint DDSD_MIPMAPCOUNT = 0x20000;	// Required in a mipmapped texture.
		const uint DDSD_LINEARSIZE 	= 0x80000;	// Required when pitch is provided for a compressed texture.
		const uint DDSD_DEPTH 		= 0x800000;	// Required in a depth texture.

		const uint DDPF_ALPHAPIXELS = 0x1;      // Texture contains alpha data; dwRGBAlphaBitMask contains valid data.
		const uint DDPF_ALPHA       = 0x2;      // Used in some older DDS files for alpha channel only uncompressed data (dwRGBBitCount contains the alpha channel bitcount; dwABitMask contains valid data)
		const uint DDPF_FOURCC      = 0x4;      // Texture contains compressed RGB data; dwFourCC contains valid data.
		const uint DDPF_RGB         = 0x40;     // Texture contains uncompressed RGB data; dwRGBBitCount and the RGB masks (dwRBitMask, dwGBitMask, dwBBitMask) contain valid data.
		const uint DDPF_YUV         = 0x200;    // Used in some older DDS files for YUV uncompressed data (dwRGBBitCount contains the YUV bit count; dwRBitMask contains the Y mask, dwGBitMask contains the U mask, dwBBitMask contains the V mask)
		const uint DDPF_LUMINANCE   = 0x20000;  // Used in some older DDS files for single channel color uncompressed data (dwRGBBitCount contains the luminance channel bit count; dwRBitMask contains the channel mask). Can be combined with DDPF_ALPHAPIXELS for a two channel DDS file.
		
		const uint DDSCAPS_COMPLEX  = 0x8;      // Optional; must be used on any file that contains more than one surface (a mipmap, a cubic environment map, or mipmapped volume texture).
		const uint DDSCAPS_MIPMAP   = 0x400000; // Optional; should be used for a mipmap.
		const uint DDSCAPS_TEXTURE  = 0x1000;   // Required
		public static void WriteDDS(string filename, uint width, uint height, byte[] textureData) {
			FileStream fs = new FileStream(filename, FileMode.Create);
			BinaryWriter bw = new BinaryWriter(fs);

			uint dwPitchOrLinearSize = 0x4000;

			bw.Write((uint)0x20534444); // dwMagic = 'DDS '
			bw.Write((uint)124); // header.dwSize = 124
			bw.Write(DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT | DDSD_MIPMAPCOUNT | DDSD_LINEARSIZE); // header.dwFlags
			bw.Write(height); // header.dwHeight
			bw.Write(width); // header.dwWidth
			bw.Write(dwPitchOrLinearSize); // header.dwPitchOrLinearSize
			bw.Write((uint)0); // header.dwDepth
			bw.Write((uint)0); // header.dwMipMapCount
			// header.dwReserved1[11]
			for (int i = 0; i < 11; i++) {
				bw.Write((uint)0); 
			}

			bw.Write((uint)32); // header.ddspf.dwSize
			bw.Write(DDPF_FOURCC); // header.ddspf.dwFlags
			bw.Write(System.Text.ASCIIEncoding.ASCII.GetBytes("DXT5")); // header.ddspf.dwFourCC
			bw.Write((uint)0); // header.ddspf.dwRGBBitCount
			bw.Write((uint)0x00000000); // header.ddspf.dwRBitMask
			bw.Write((uint)0x00000000); // header.ddspf.dwGBitMask
			bw.Write((uint)0x00000000); // header.ddspf.dwBBitMask
			bw.Write((uint)0x00000000); // header.ddspf.dwABitMask
			
			bw.Write(DDSCAPS_TEXTURE); // header.ddspf.dwCaps 
			bw.Write((uint)0); // header.ddspf.dwCaps2
			bw.Write((uint)0); // header.ddspf.dwCaps3
			bw.Write((uint)0); // header.ddspf.dwCaps4
			bw.Write((uint)0); // header.ddspf.dwReserved2
			
			//bw.Write((uint)0); // header.ddspf.bdata
			//bw.Write((uint)0); // header.ddspf.bdata2

			//bw.Write((uint)0xFFFFFFFF); 
			bw.Write(textureData);
		}
	}

	class RippedTexture2D : Texture2D {
		public RippedTexture2D() : base() {

		}
	}

	class Texture2DRipperReader : ContentTypeReader {
		public override object Read(BinaryReader reader, string file)
		{
			SurfaceFormat pixelFormat = (SurfaceFormat)reader.ReadInt32();
			int width = reader.ReadInt32();
			int height = reader.ReadInt32();
			reader.ReadInt32();
			int num = reader.ReadInt32();
			byte[] array = new byte[num];
			int num2 = reader.Read(array, 0, num);

			string outfbland = MainClass.OUT_DIR + file.Replace("\\","/");
			string outf = outfbland + "." + width + "." + height + "." + pixelFormat.ToString();
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outf));
			System.IO.File.WriteAllBytes(outf, array);

			if (pixelFormat == SurfaceFormat.Dxt5) {
				DDSWriter.WriteDDS(outfbland + ".dds", (uint)width, (uint)height, array);
				Console.WriteLine("Ripped: " + outfbland + ".dds");
			} else if (pixelFormat == SurfaceFormat.Color) {
				var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				var locked = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				if (array.Length != width * height * 4)
					throw new Exception();
				unsafe {
					byte* bytes = (byte*)locked.Scan0;
					for (int i=0; i<width * height * 4; i++) {
						bytes[i] = array[i];
					}
				}
				bitmap.UnlockBits(locked);
				bitmap.Save(outfbland + ".png");
				Console.WriteLine("Ripped: " + outfbland + ".png");
			} else {
				Console.WriteLine("!!!FAILED!!!: " + pixelFormat + "");
				throw new Exception();
			}

			return new RippedTexture2D();
		}
	}

	class ExtractorGameAssetManager : GSGE.GameAssetManager {
		public ExtractorGameAssetManager(Game game, IServiceProvider serviceProvider, string rootDirectory, string packageDirectory)
			: base(game, serviceProvider, rootDirectory, packageDirectory)
		{
		}

		protected new T ReadAsset<T>(string assetName, Action<IDisposable> recordDisposableObject, object existingPrimaryObject = null) where T : class
		{
			if (string.IsNullOrEmpty(assetName))
			{
				throw new ArgumentNullException("assetName");
			}
			object obj = null;
			using (Stream stream = this.OpenStream(assetName))
			{
				using (BinaryReader binaryReader = new BinaryReader(stream))
				{
					byte b = binaryReader.ReadByte();
					byte b2 = binaryReader.ReadByte();
					byte b3 = binaryReader.ReadByte();
					byte b4 = binaryReader.ReadByte();
					if (b != 88 || b2 != 78 || b3 != 66 || (b4 != 119 && b4 != 120 && b4 != 109 && b4 != 112))
					{
						throw new ContentLoadException("Asset does not appear to be a valid XNB file. Did you process your content for Windows?");
					}
					byte b5 = binaryReader.ReadByte();
					byte b6 = binaryReader.ReadByte();
					bool flag = (b6 & 128) != 0;
					if (b5 != 5 && b5 != 4)
					{
						throw new ContentLoadException("Invalid XNB version");
					}
					int num = binaryReader.ReadInt32();
					ContentReader contentReader;
					if (flag)
					{
						LzxDecoder lzxDecoder = new LzxDecoder(16);
						int num2 = num - 14;
						int num3 = binaryReader.ReadInt32();
						int num4 = num3 + 10;
						MemoryStream memoryStream = new MemoryStream(num3);
						int num5 = 0;
						int i = 0;
						while (i < num2)
						{
							stream.Seek((long)(i + 14), SeekOrigin.Begin);
							int num6 = stream.ReadByte();
							int num7 = stream.ReadByte();
							int num8 = num6 << 8 | num7;
							int num9 = 32768;
							if (num6 == 255)
							{
								num6 = num7;
								num7 = (int)((byte)stream.ReadByte());
								num9 = (num6 << 8 | num7);
								num6 = (int)((byte)stream.ReadByte());
								num7 = (int)((byte)stream.ReadByte());
								num8 = (num6 << 8 | num7);
								i += 5;
							}
							else
							{
								i += 2;
							}
							if (num8 == 0 || num9 == 0)
							{
								break;
							}
							int num10 = lzxDecoder.Decompress(stream, num8, memoryStream, num9);
							i += num8;
							num5 += num9;
						}
						if (memoryStream.Position != (long)num3)
						{
							throw new ContentLoadException("Decompression of " + assetName + "failed.  Try decompressing with nativeDecompressXnb first.");
						}
						memoryStream.Seek(0L, SeekOrigin.Begin);
						contentReader = new ContentReader(this, memoryStream, null, assetName);
					}
					else
					{
						Console.WriteLine("Reading: " + assetName + ".dds");
						contentReader = new ContentReader(this, stream, null, assetName);
					}

					using (contentReader)
					{
						obj = contentReader.ReadAsset<T>(assetName);
					}
				}
			}
			return (T)((object)obj);
		}

		public override T Load<T>(string assetName)
		{
			object _loadLock
				= MainClass.GetInstanceField(typeof(GSGE.GameAssetManager), this, "_loadLock");
			Dictionary<string, object> _loadedAssets
				= MainClass.GetInstanceField(typeof(GSGE.GameAssetManager), this, "_loadedAssets") as Dictionary<string, object>;

			object asset = null;
			string name = ContentManager.CleanPath(assetName);
			T result;
			lock (_loadLock)
			{
				if (_loadedAssets.TryGetValue(name, out asset))
				{
					result = (T)((object)asset);
					return result;
				}
			}
			asset = this.ReadAsset<T>(name, null, null);
			lock (_loadLock)
			{
				_loadedAssets[name] = asset;
			}
			result = (T)((object)asset);
			return result;
		}
	}

	class MainClass
	{
		public const string OUT_DIR = "/home/matthew/Documents/TransistorUnpacked/";

		/// <summary>
		/// Uses reflection to get the field value from an object.
		/// </summary>
		///
		/// <param name="type">The instance type.</param>
		/// <param name="instance">The instance object.</param>
		/// <param name="fieldName">The field's name which is to be fetched.</param>
		///
		/// <returns>The field value from the object.</returns>
		internal static object GetInstanceField(Type type, object instance, string fieldName)
		{
			BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.Static;
			FieldInfo field = type.GetField(fieldName, bindFlags);
			return field.GetValue(instance);
		}

		public static void Main(string[] args)
		{
			Directory.Delete(OUT_DIR, true);
			Directory.CreateDirectory(OUT_DIR);

			var game = new Game();
			{
				GSGE.TextureHandleManager.Init(16384);
				ExtractorGameAssetManager gameAssetManager = new ExtractorGameAssetManager(null, game.Services, "Content", "Win\\Packages");
				GSGE.AssetManager.Initialize(gameAssetManager);
			}
			{
				var files = Directory.EnumerateFiles("Content/Win/Packages/", "*.pkg", SearchOption.TopDirectoryOnly);
				List<string> list = new List<string>();
				foreach (string f in files) {
					if (f.EndsWith(".pkg")) {
						list.Add(f.Substring(0, f.Length - 4));
					}
				}

				var files2 = Directory.EnumerateFiles("Content/Win/Packages/720p/", "*.pkg", SearchOption.TopDirectoryOnly);
				List<string> list2 = new List<string>();
				foreach (string f in files2) {
					if (f.EndsWith(".pkg")) {
						list2.Add(f.Substring(0, f.Length - 4));
					}
				}

				foreach (var f in list) {
					list2.Remove(f.Replace("Content/Win/Packages/", "Content/Win/Packages/720p/"));
				}
				
				foreach (var f in list) {
					var f2 = f.Replace("Content/Win/Packages/", "");
					Console.WriteLine("Loading Package: " + f2);
					GSGE.AssetManager.LoadPackage(f2, GSGE.PackageGroup.Base);
				}
			}

		}
	}
}
