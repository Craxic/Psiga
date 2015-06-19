/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using PsigaPkgLib.Entries;
using System.Threading;
using Gdk;
using System.IO;
using Cairo;
using System.Collections.Generic;
using Gtk;
using System.Runtime.InteropServices;
using PsigaPkgLib;
using PsigaPkgLib.Entries.Atlas;

namespace Psiga
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class TextureWidget : Gtk.Bin, Shape
	{
		private SubAtlas atlas;
		public SubAtlas Atlas {
			get {
				return atlas;
			}
			set {
				if (atlas != value) {
					atlas = value;
					TriggerRedraw();
				}
			}
		}

		private TextureEntry viewing;
		public TextureEntry Viewing {
			get {
				return viewing;
			}
			set {
				if (viewing != value) {
					decompressed = null;
					viewing = value;
					propertyview.Viewing = viewing;
					if (viewing != null) {
						LoadTextureAsync();
					}
				}
			}
		}

		// private ImageSurface textureSurface = null;
		private StatusReceiver statusReceiver;
		public StatusReceiver StatusReceiver { get; set; }
		private List<AtlasEntry> atlasEntries = new List<AtlasEntry>();
		public TextureWidget()
		{
			this.Build();
			pannabledrawingarea1.Shape = this;
		}

		public void ShowPreview() {
			notebook1.CurrentPage = 0;
		}

		private void SearchEntry(PsigaPkgLib.Entries.Entry e) {
			AtlasEntry ae = e as AtlasEntry;
			if (ae != null) {
				if (ae.IsReference && ae.ReferencedTextureName == viewing.Name) {
					atlasEntries.Add(ae);
				}
			}
		}

		private void FindAtlasses() {
			// Search for all atlasses concerning this texture.
			atlasEntries.Clear();
			lock (PackageManager.Lock) {
				foreach (var lp in PackageManager.LoadedPackages) {
					foreach (var e in lp.Value.ManifestContents) {
						SearchEntry(e);
					}
					foreach (var e in lp.Value.PackageContents) {
						SearchEntry(e);
					}
				}
			}
		}

		private void TriggerRedraw() {
			Gtk.Application.Invoke((s, e) => {
				pannabledrawingarea1.QueueDraw();
			});
		}

		private byte[] decompressed = null;

		private void LoadTextureAsync() {
			if (viewing != null && viewing.Texture.IsDecompressed && Atlas != null) {
				decompressed = viewing.Texture.GetDecompressedARGB32PreMul();
				return;
			} else {
				(new Thread(() => {
					if (viewing != null) {
						var dc = viewing.Texture.GetDecompressedARGB32PreMul();
						decompressed = dc;
						TriggerRedraw();
						FindAtlasses();
						TriggerRedraw();
					}
				})).Start();
			}
		}

		public unsafe void Draw(Cairo.Context context, double scale)
		{
			if (decompressed == null || viewing == null) {
				return;
			}

			int texOffsetX = 0; 
			int texOffsetY = 0;

			int top, left, right, bottom;

			if (Atlas != null) {
				int offsetX = Atlas.TopLeft.X - Atlas.Rect.Left;
				int offsetY = Atlas.TopLeft.Y - Atlas.Rect.Top;

				top = Atlas.Rect.Top + offsetY;
				left = Atlas.Rect.Left + offsetX;
				right = Atlas.Rect.Right + offsetX;
				bottom = Atlas.Rect.Bottom + offsetY;

				texOffsetX = -Atlas.Rect.Left + left;
				texOffsetY = -Atlas.Rect.Top + top;
			} else {
				top = 0;
				left = 0;
				right = viewing.Texture.Width;
				bottom = viewing.Texture.Height;
			}

			context.MoveTo(left, top);
			context.LineTo(left, bottom);
			context.LineTo(right, bottom);
			context.LineTo(right, top);
			context.LineWidth = 0;

			if (blackBackground.Active) {
				context.SetSourceRGB(0,0,0);
				context.FillPreserve();
			}

			fixed (byte * decompressedFixed = decompressed)
			{
				var buf = new ImageSurface(
					(IntPtr)decompressedFixed, Format.ARGB32, 
					viewing.Texture.Width, viewing.Texture.Height, 
					viewing.Texture.Width * 4);
				context.SetSourceSurface(buf, texOffsetX, texOffsetY);
				context.Fill();
				context.SetSourceRGB(1, 0, 0);
				buf.Dispose();
			}

			if (Atlas == null) {
				if (showAtlasBoxes.Active) {
					foreach (var e in atlasEntries) {
						foreach (var sa in e.Entries) {
							context.MoveTo (sa.Rect.Left, sa.Rect.Top);
							context.LineTo (sa.Rect.Right, sa.Rect.Top);
							context.LineTo (sa.Rect.Right, sa.Rect.Bottom);
							context.LineTo (sa.Rect.Left, sa.Rect.Bottom);
							context.LineTo (sa.Rect.Left, sa.Rect.Top);
						}
					}
					context.LineWidth = 1 / scale;
					context.Stroke ();
				}

				if (showAtlasLabels.Active) {
					foreach (var e in atlasEntries) {
						foreach (var sa in e.Entries) {
							context.MoveTo (sa.Rect.Left, sa.Rect.Bottom);
							context.SetFontSize (16 / scale);
							context.ShowText (sa.Name);
						}
					}
				}
			}
		}

		protected void onExportPNG(object sender, EventArgs e)
		{
			Gtk.Window window = null;
			Widget toplevel = Toplevel;
			if (toplevel.IsTopLevel)
			{
				window = toplevel as Gtk.Window;
			}
			Gtk.FileChooserDialog fileChooser =
				new Gtk.FileChooserDialog("Save PNG",
					window,
					FileChooserAction.Save,
					"Cancel", ResponseType.Cancel,
					"Save", ResponseType.Accept);
			var filter = new FileFilter();
			filter.Name = "PNG Image (*.png)";
			filter.AddPattern("*.png");
			fileChooser.AddFilter(filter);

			if (fileChooser.Run() == (int)ResponseType.Accept) 
			{
				if (decompressed != null) {
					ImageSurface buf;
					if (Atlas == null) {
						buf = new ImageSurface(
							decompressed, Format.Argb32, 
							viewing.Texture.Width, viewing.Texture.Height, 
							viewing.Texture.Width * 4);
					} else {
						int top = Atlas.Rect.Top;
						int left = Atlas.Rect.Left;
						int right = Atlas.Rect.Right;
						int bottom = Atlas.Rect.Bottom;
						unsafe {
							fixed (byte * dcu = decompressed) {
								buf = new ImageSurface (
									(IntPtr)(dcu + (top * viewing.Texture.Width + left) * 4), Format.Argb32, 
									Atlas.Rect.Width, Atlas.Rect.Height, 
									viewing.Texture.Width * 4);
							}
						}
					}
					var filename = fileChooser.Filename;
					if (!filename.EndsWith(".png")) {
						filename = fileChooser.Filename + ".png";
					}
					buf.WriteToPng(filename);
					buf.Dispose();
				}
			}

			fileChooser.Destroy();
		}

		protected void onResetTransforms(object sender, EventArgs e)
		{
			pannabledrawingarea1.ResetTransforms();
		}

		protected void showAtlasLabelsToggled(object sender, EventArgs e)
		{
			pannabledrawingarea1.QueueDraw();
		}

		protected void showAtlasBoxesToggled(object sender, EventArgs e)
		{
			pannabledrawingarea1.QueueDraw();
		}

		protected void toggleBlackBackground(object sender, EventArgs e)
		{
			pannabledrawingarea1.QueueDraw();
		}
	}
}

