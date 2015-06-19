/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.Drawing;
using System.ComponentModel;

namespace PsigaPkgLib.Entries.Atlas
{
	public class SubAtlas
	{
		[EditorBrowsableAttribute(EditorBrowsableState.Never)]
		public AtlasEntry Parent { get; set; }
		public string Name { get; set; }
		public Rectangle Rect { get; set; }
		public Point TopLeft { get; set; }
		public Point OriginalSize { get; set; }
		public Vector2 ScaleRatio { get; set; }
		public bool IsMultiTexture { get; set; }
		public bool IsMip { get; set; }
	}
}

