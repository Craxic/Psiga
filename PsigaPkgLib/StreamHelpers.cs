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

		public static void WriteString(this Stream stream, string write)
		{
			if (write.Length > byte.MaxValue) {
				throw new ArgumentOutOfRangeException("write", "Transistor Packages do not support strings longer than 255 characters.");
			}
			stream.WriteByte((byte)write.Length);
			byte[] writeBuffer = Encoding.ASCII.GetBytes(write);
			stream.Write(writeBuffer, 0, writeBuffer.Length);
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

