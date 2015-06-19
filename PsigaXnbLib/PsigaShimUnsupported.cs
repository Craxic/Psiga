/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;

namespace PsigaXnbLib
{
	public class PsigaShimUnsupported : Exception
	{
		public PsigaShimUnsupported() : base()
		{
		}

		public PsigaShimUnsupported(string message) : base(message)
		{
		}
	}
}

