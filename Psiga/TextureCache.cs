using System;
using PsigaPkgLib.Entries;
using System.Collections.Generic;
using PsigaPkgLib;

namespace Psiga
{
	public static class TextureCache
	{
		// Cache change lock must be locked when the cache is changing.
		private static volatile Dictionary<TextureEntry, byte[]> cache = new Dictionary<TextureEntry, byte[]>();

	    private static bool disableCache = false;

        static TextureCache() {
			// Need to make a new cache when something happens. We can't clear the old one in case a thread is still in 
			// Get. Then it would put data from the old root in the now cleared cache.
			PackageManager.OnRootChanged += () => {
				cache = new Dictionary<TextureEntry, byte[]>();
			};
			PackageManager.OnPackageUnload += (p) => {
				cache = new Dictionary<TextureEntry, byte[]>();
			};

		    if (IntPtr.Size == 4)
		    {
                // We DO NOT have the memory for this lol.
		        disableCache = true;
		    }
		}

		public static void Flush(TextureEntry e) {
		    if (disableCache)
		    {
		        return;
		    }

			Dictionary<TextureEntry, byte[]> workingCache = cache;
			workingCache.Remove(e);
		}

		/// <summary>
		/// Gets the texture entries data in BGRA format, other wise known as ARGB32 Little endian.
		/// </summary>
		/// <param name="e">The texture entry</param>
		public static byte[] Get(TextureEntry e)
		{
		    if (disableCache)
		    {
		        return Decompress(e);
		    }

            Dictionary<TextureEntry, byte[]> workingCache = cache;
			lock (workingCache) {
				if (workingCache.ContainsKey(e)) {
					return workingCache[e];
				}
			}

		    var decompressed = Decompress(e);

		    lock (workingCache) {
				workingCache[e] = decompressed;
				return decompressed;
			}
		}

	    private static byte[] Decompress(TextureEntry e)
	    {
	        byte[] decompressed = e.Texture.DecompressData();
	        for (int i = 0; i < decompressed.Length; i += 4)
	        {
	            byte r = decompressed[i];
	            byte g = decompressed[i + 1];
	            byte b = decompressed[i + 2];
	            byte a = decompressed[i + 3];

	            r = (byte) (((int) r * (int) a) / 255);
	            g = (byte) (((int) g * (int) a) / 255);
	            b = (byte) (((int) b * (int) a) / 255);

	            decompressed[i] = b;
	            decompressed[i + 1] = g;
	            decompressed[i + 2] = r;
	            decompressed[i + 3] = a;
	        }
	        return decompressed;
	    }
	}
}

