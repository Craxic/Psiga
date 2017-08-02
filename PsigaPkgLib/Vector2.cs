/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;

namespace PsigaPkgLib
{
	public class Vector2
	{
		public float X;
		public float Y;

		public static Vector2 One { get { return new Vector2(1, 1); } }
		public static Vector2 UnitX  { get { return new Vector2(1, 0); } }
		public static Vector2 UnitY { get { return new Vector2(0, 1); } }
		public static Vector2 Zero { get { return new Vector2(0, 0); } }

		public Vector2(float value) { 
			X = value;
			Y = value;
		}

		public Vector2(float x, float y) { 
			X = x;
			Y = y;
		}

		public override string ToString()
		{
			return string.Format("Vector2({0},{1})", X, Y);
		}
	}
}

