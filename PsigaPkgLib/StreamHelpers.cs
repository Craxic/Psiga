/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.IO;
using System.Text;

namespace PsigaPkgLib
{
	public static class StreamHelpers
	{
		private const string ERR_STRING_OOB = "Could not read string from stream, not enough data. Needed {0}, got {1}";

		public static byte[] MakeInt32BE(int input) {
			var ret = BitConverter.GetBytes(input);
			if (BitConverter.IsLittleEndian) {
				Array.Reverse(ret);
			}
			return ret;
		}

		public static string ReadString(this Stream stream)
		{
			int stringLength = stream.ReadByte();
			byte[] buffer = new byte[stringLength];
			int bytesRead = stream.Read(buffer, 0, stringLength);
			if (bytesRead != stringLength) {
				throw new IndexOutOfRangeException(string.Format(ERR_STRING_OOB, stringLength, bytesRead));
			}
			return Encoding.ASCII.GetString(buffer, 0, stringLength);
		}

		public static int StringSize(this Stream stream, string write) {
			if (write.Length > byte.MaxValue) {
				throw new ArgumentOutOfRangeException("write", "Transistor Packages do not support strings longer than 255 characters.");
			}
			return 1 + Encoding.ASCII.GetBytes(write).Length;
		}

		public static void WriteString(this Stream stream, string write)
		{
			if (write.Length > byte.MaxValue) {
				throw new ArgumentOutOfRangeException("write", "Transistor Packages do not support strings longer than 255 characters.");
			}
			stream.WriteByte((byte)write.Length);
			byte[] writeBuffer = Encoding.ASCII.GetBytes(write);
			stream.Write(writeBuffer, 0, writeBuffer.Length);
		}

		public static void WriteSingleBE(this Stream stream, float write)
		{
			stream.WriteBE(BitConverter.GetBytes(write));
		}

		public static void WriteUInt16BE(this Stream stream, UInt16 write)
		{
			stream.WriteBE(BitConverter.GetBytes(write));
		}

		public static void WriteInt16BE(this Stream stream, Int16 write)
		{
			stream.WriteBE(BitConverter.GetBytes(write));
		}

		public static void WriteUInt32BE(this Stream stream, UInt32 write)
		{
			stream.WriteBE(BitConverter.GetBytes(write));
		}

		public static void WriteInt32BE(this Stream stream, Int32 write)
		{
			stream.WriteBE(BitConverter.GetBytes(write));
		}

		public static void WriteBE(this Stream stream, byte[] bytes) {
			if (BitConverter.IsLittleEndian) {
				Array.Reverse(bytes);
			}
			stream.Write(bytes, 0, bytes.Length);
		}

		// Some of the code below from http://stackoverflow.com/a/15274591/308098

		public static Single ReadSingleBE(this Stream stream)
		{
			return BitConverter.ToSingle(stream.ReadBigEndianBytes(sizeof(Single)), 0);
		}

		public static UInt16 ReadUInt16BE(this Stream stream)
		{
			return BitConverter.ToUInt16(stream.ReadBigEndianBytes(sizeof(UInt16)), 0);
		}

		public static Int16 ReadInt16BE(this Stream stream)
		{
			return BitConverter.ToInt16(stream.ReadBigEndianBytes(sizeof(Int16)), 0);
		}

		public static UInt32 ReadUInt32BE(this Stream stream)
		{
			return BitConverter.ToUInt32(stream.ReadBigEndianBytes(sizeof(UInt32)), 0);
		}

		public static Int32 ReadInt32BE(this Stream stream)
		{
			return BitConverter.ToInt32(stream.ReadBigEndianBytes(sizeof(Int32)), 0);
		}

		public static string ReadBigString(this Stream stream)
		{
			int len = stream.ReadInt32BE();
			if (len < 0)
			{
				throw new EndOfStreamException(string.Format("Positive length required from stream, but got {0}.", len));
			}
			
			var bytes = new byte[len];
			var read = stream.Read(bytes, 0, len);
			if (read != len)
			{
				throw new EndOfStreamException(string.Format("{0} bytes required from stream, but only {1} returned.", len, read));
			}
			return Encoding.UTF8.GetString(bytes);
		}

		public static void WriteBigString(this Stream stream, string write)
		{
			var bytes = Encoding.UTF8.GetBytes(write);
			stream.WriteInt32BE(bytes.Length);
			stream.Write(bytes, 0, bytes.Length);
		}

		public static byte[] ReadBigEndianBytes(this Stream stream, int byteCount)
		{
			var buffer = new byte[byteCount];
			var readCount = stream.Read(buffer, 0, byteCount);
			if (readCount != byteCount)
				throw new EndOfStreamException(string.Format("{0} bytes required from stream, but only {1} returned.", byteCount, readCount));
			if (BitConverter.IsLittleEndian) {
				Array.Reverse(buffer);
			}
			return buffer;
		}
	}
}

