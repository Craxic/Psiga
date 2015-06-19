// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Utilities;

namespace Microsoft.Xna.Framework.Content
{
	public class ContentManagerShim
	{
		const byte ContentCompressedLzx = 0x80;
		const byte ContentCompressedLz4 = 0x40;

		static List<char> targetPlatformIdentifiers = new List<char>()
		{
			'w', // Windows (DirectX)
			'x', // Xbox360
			'm', // WindowsPhone
			'i', // iOS
			'a', // Android
			'd', // DesktopGL
			'X', // MacOSX
			'W', // WindowsStoreApp
			'n', // NativeClient
			'u', // Ouya
			'p', // PlayStationMobile
			'M', // WindowsPhone8
			'r', // RaspberryPi
			'P', // PlayStation4

			// Old WindowsGL and Linux platform chars
			'w',
			'l',
		};

		public static ContentReaderShim GetContentReaderFromXnb(string originalAssetName, ref Stream stream, BinaryReader xnbReader)
		{
			// The first 4 bytes should be the "XNB" header. i use that to detect an invalid file
			byte x = xnbReader.ReadByte();
			byte n = xnbReader.ReadByte();
			byte b = xnbReader.ReadByte();
			byte platform = xnbReader.ReadByte();

			if (x != 'X' || n != 'N' || b != 'B' ||
				!(targetPlatformIdentifiers.Contains((char)platform)))
			{
				throw new ContentLoadException("Asset does not appear to be a valid XNB file. Did you process your content for Windows?");
			}

			byte version = xnbReader.ReadByte();
			byte flags = xnbReader.ReadByte();

			bool compressedLzx = (flags & ContentCompressedLzx) != 0;
			bool compressedLz4 = (flags & ContentCompressedLz4) != 0;
			if (version != 5 && version != 4)
			{
				throw new ContentLoadException("Invalid XNB version");
			}

			// The next int32 is the length of the XNB file
			int xnbLength = xnbReader.ReadInt32();

			ContentReaderShim reader;
			if (compressedLzx || compressedLz4)
			{
				// Decompress the xnb
				int decompressedSize = xnbReader.ReadInt32();
				MemoryStream decompressedStream = null;

				if (compressedLzx)
				{
					//thanks to ShinAli (https://bitbucket.org/alisci01/xnbdecompressor)
					// default window size for XNB encoded files is 64Kb (need 16 bits to represent it)
					LzxDecoder dec = new LzxDecoder(16);
					decompressedStream = new MemoryStream(decompressedSize);
					int compressedSize = xnbLength - 14;
					long startPos = stream.Position;
					long pos = startPos;

					while (pos - startPos < compressedSize)
					{
						// the compressed stream is seperated into blocks that will decompress
						// into 32Kb or some other size if specified.
						// normal, 32Kb output blocks will have a short indicating the size
						// of the block before the block starts
						// blocks that have a defined output will be preceded by a byte of value
						// 0xFF (255), then a short indicating the output size and another
						// for the block size
						// all shorts for these cases are encoded in big endian order
						int hi = stream.ReadByte();
						int lo = stream.ReadByte();
						int block_size = (hi << 8) | lo;
						int frame_size = 0x8000; // frame size is 32Kb by default
						// does this block define a frame size?
						if (hi == 0xFF)
						{
							hi = lo;
							lo = (byte)stream.ReadByte();
							frame_size = (hi << 8) | lo;
							hi = (byte)stream.ReadByte();
							lo = (byte)stream.ReadByte();
							block_size = (hi << 8) | lo;
							pos += 5;
						}
						else
							pos += 2;

						// either says there is nothing to decode
						if (block_size == 0 || frame_size == 0)
							break;

						dec.Decompress(stream, block_size, decompressedStream, frame_size);
						pos += block_size;

						// reset the position of the input just incase the bit buffer
						// read in some unused bytes
						stream.Seek(pos, SeekOrigin.Begin);
					}

					if (decompressedStream.Position != decompressedSize)
					{
						throw new ContentLoadException("Decompression of " + originalAssetName + " failed. ");
					}

					decompressedStream.Seek(0, SeekOrigin.Begin);
				}
				else if (compressedLz4)
				{
					// Decompress to a byte[] because Windows 8 doesn't support MemoryStream.GetBuffer()
					var buffer = new byte[decompressedSize];
					using (var decoderStream = new Lz4DecoderStream(stream))
					{
						if (decoderStream.Read(buffer, 0, buffer.Length) != decompressedSize)
						{
							throw new ContentLoadException("Decompression of " + originalAssetName + " failed. ");
						}
					}
					// Creating the MemoryStream with a byte[] shares the buffer so it doesn't allocate any more memory
					decompressedStream = new MemoryStream(buffer);
				}

				reader = new ContentReaderShim(decompressedStream);
			}
			else
			{
				reader = new ContentReaderShim(stream);
			}
			return reader;
		}
	}
}

