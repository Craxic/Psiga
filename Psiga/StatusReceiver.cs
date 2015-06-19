/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;

namespace Psiga
{
	public interface StatusReceiver
	{
		void OnStatusReceived(string statusText, double statusProgress);
	}
}

