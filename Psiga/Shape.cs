/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;

namespace Psiga
{
	public interface Shape
	{
		void Draw(Cairo.Context context, double scale);
	}
}

