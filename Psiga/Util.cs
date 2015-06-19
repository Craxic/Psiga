/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using Gtk;

namespace Psiga
{
	public class Util
	{
		/// <summary>
		/// Thanks to http://stackoverflow.com/a/4975942/308098
		/// </summary>
		/// <returns>The to string.</returns>
		/// <param name="byteCount">Byte count.</param>
		public static String NumBytesToString(long byteCount)
		{
			string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
			if (byteCount == 0)
				return "0" + suf[0];
			long bytes = Math.Abs(byteCount);
			int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
			double num = Math.Round(bytes / Math.Pow(1024, place), 1);
			return (Math.Sign(byteCount) * num).ToString() + suf[place];
		}

		public static int MapSort<T>(TreeModel model, TreeIter a, TreeIter b, int column, Comparison<T> comparer) {
			var sa = (T)model.GetValue(a, column);
			var ba = (T)model.GetValue(b, column);
			return comparer(sa, ba);
		}
	}
}

