/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using Microsoft.Xna.Framework.Content;
using PsigaXnbLib;

namespace Microsoft.Xna.Framework.Content
{
	public sealed class ContentTypeReaderManager
	{
		private ContentReaderShim _reader;
		private ContentTypeReader[] contentReaders;
		public ContentTypeReaderManager(ContentReaderShim reader)
		{
			this._reader = reader;
		}
		internal ContentTypeReader[] LoadAssetReaders()
		{
			int num = this._reader.Read7BitEncodedInt();
			this.contentReaders = new ContentTypeReader[num];
			for (int i = 0; i < num; i++)
			{
				string text = this._reader.ReadString();
				if (text == "Microsoft.Xna.Framework.Content.Texture2DReader")
					this.contentReaders[i] = new Texture2DRipperReader();
				else 
					throw new PsigaShimUnsupported("Unsupported Asset Type");
				int num2 = this._reader.ReadInt32();
			}
			return this.contentReaders;
		}
	}

}

