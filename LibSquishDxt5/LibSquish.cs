using System;
using System.Runtime.InteropServices;

namespace LibSquishDxt5
{
	public class LibSquish
	{
		[DllImport("squish")]
		public static extern void compress_dxt5(IntPtr pixels, int width, int height, IntPtr output);

		[DllImport("squish")]
		public static extern int dxt5_size(int width, int height);
	}
}

