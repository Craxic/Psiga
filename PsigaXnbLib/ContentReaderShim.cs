// MIT License - Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Utilities;
using PsigaXnbLib;

namespace Microsoft.Xna.Framework.Content
{
	public sealed class ContentReaderShim : BinaryReader
	{
		private ContentTypeReaderManager typeReaderManager;
		private List<KeyValuePair<int, Action<object>>> sharedResourceFixups;
		private ContentTypeReader[] typeReaders;
		internal int sharedResourceCount;

		internal ContentTypeReader[] TypeReaders
		{
			get
			{
				return typeReaders;
			}
		}

		internal ContentReaderShim(Stream stream) : base(stream) { }

		public object ReadAsset<T>()
		{
			InitializeTypeReaders();

			// Read primary object
			object result = ReadObject<T>();

			// Read shared resources
			ReadSharedResources();

			return result;
		}

		internal void InitializeTypeReaders()
		{
			typeReaderManager = new ContentTypeReaderManager(this);
			typeReaders = typeReaderManager.LoadAssetReaders();
			sharedResourceCount = Read7BitEncodedInt();
			sharedResourceFixups = new List<KeyValuePair<int, Action<object>>>();
		}

		internal void ReadSharedResources()
		{
			if (sharedResourceCount <= 0)
				return;

			var sharedResources = new object[sharedResourceCount];
			for (var i = 0; i < sharedResourceCount; ++i)
				sharedResources[i] = InnerReadObject<object>(null);

			// Fixup shared resources by calling each registered action
			foreach (var fixup in sharedResourceFixups)
				fixup.Value(sharedResources[fixup.Key]);
		}

		public T ReadObject<T>()
		{
			return ReadObject(default(T));
		}

		public T ReadObject<T>(T existingInstance)
		{
			return InnerReadObject(existingInstance);
		}

		private T InnerReadObject<T>(T existingInstance)
		{
			var typeReaderIndex = Read7BitEncodedInt();
			if (typeReaderIndex == 0)
				return existingInstance;

			if (typeReaderIndex > typeReaders.Length)
				throw new ContentLoadException("Incorrect type reader index found!");

			var typeReader = typeReaders[typeReaderIndex - 1];
			var result = (T)typeReader.Read(this);

			return result;
		}

		internal new int Read7BitEncodedInt()
		{
			return base.Read7BitEncodedInt();
		}
	}
}
