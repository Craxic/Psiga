/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using PsigaXnbLib;

namespace Microsoft.Xna.Framework.Content
{
	public sealed class ContentTypeReaderManager
	{
		private static readonly Dictionary<Type, ContentTypeReader> ContentReaders = new Dictionary<Type, ContentTypeReader>();

		static ContentTypeReaderManager()
		{
			// ContentTypeReaderManager.AddTypeReader(new EffectReader());
			ContentReaders.Add(typeof(Texture), new Texture2DRipperReader());
			ContentReaders.Add(typeof(Texture3D), new Texture3DRipperReader());
			// ContentTypeReaderManager.AddTypeReader(new SpriteFontReader());
		}

		internal static ContentTypeReader GetTypeReader(Type t)
		{
			return ContentTypeReaderManager.ContentReaders[t];
		}
		
		private ContentReaderShim _reader;
		public ContentTypeReaderManager(ContentReaderShim reader)
		{
			this._reader = reader;
		}
	}

}

