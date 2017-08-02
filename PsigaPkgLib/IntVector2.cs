/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;

namespace PsigaPkgLib
{
	public class IntVector2
	{
		public int X;
		public int Y;
		
		public IntVector2(int x, int y) { 
			X = x;
			Y = y;
		}

		public override string ToString()
		{
			return string.Format("IntVector2({0},{1})", X, Y);
		}
	}
}

