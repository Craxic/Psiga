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
using System.Drawing;
using PsigaXnbLib;

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
					} else {
						pannabledrawingarea.QueueDraw();
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
			pannabledrawingarea.Shape = this;
		}

		public void ShowPreview() {
			notebook1.CurrentPage = 0;
		}

		private static void StartWorker(TextureEntry viewingLocal, Action<byte[], List<AtlasEntry>> cb) {
			(new Thread(() => {
				byte[] decompressedLocal = TextureCache.Get(viewingLocal);
				List<AtlasEntry> atlasEntriesLocal = PackageManager.GetAtlasesByTextureName(viewingLocal.Name);
				Gtk.Application.Invoke((s, e) => {
					cb(decompressedLocal, atlasEntriesLocal);
				});
			})).Start();
		}

		private void TriggerRedraw() {
			Gtk.Application.Invoke((s, e) => pannabledrawingarea.QueueDraw());
		}

		private byte[] decompressed = null;
		private void LoadTextureAsync() {
			StartWorker(viewing, (d, a) => {
				decompressed = d;
				atlasEntries = a;
				pannabledrawingarea.QueueDraw();
			});
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

		protected void importPNG_Clicked(object sender, EventArgs e)
		{
			if (Atlas != null) {
				return;
			}
			Gtk.Window window = null;
			Widget toplevel = Toplevel;
			if (toplevel.IsTopLevel)
			{
				window = toplevel as Gtk.Window;
			}
			Gtk.FileChooserDialog fileChooser =
				new Gtk.FileChooserDialog("Save PNG",
					window,
					FileChooserAction.Open,
					"Cancel", ResponseType.Cancel,
					"Open", ResponseType.Accept);
			var filter = new FileFilter();
			filter.Name = "PNG Image (*.png)";
			filter.AddPattern("*.png");
			fileChooser.AddFilter(filter);
			if (fileChooser.Run() == (int)ResponseType.Accept) 
			{
				var file = (Bitmap)Bitmap.FromFile(fileChooser.Filename);
				var locked = file.LockBits(
					new System.Drawing.Rectangle(0, 0, file.Width, file.Height), 
					System.Drawing.Imaging.ImageLockMode.ReadOnly, 
					System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				byte[] newData = new byte[file.Width * file.Height * 4];
				Marshal.Copy(locked.Scan0, newData, 0, newData.Length);
				file.UnlockBits(locked);
				Texture.BGRAtoRGBA(newData);
			    viewing.Texture.SetRgbaData(newData);
				TextureCache.Flush(viewing);
				decompressed = null;
				LoadTextureAsync();
			}

			fileChooser.Destroy();
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
			pannabledrawingarea.ResetTransforms();
		}

		protected void showAtlasLabelsToggled(object sender, EventArgs e)
		{
			pannabledrawingarea.QueueDraw();
		}

		protected void showAtlasBoxesToggled(object sender, EventArgs e)
		{
			pannabledrawingarea.QueueDraw();
		}

		protected void toggleBlackBackground(object sender, EventArgs e)
		{
			pannabledrawingarea.QueueDraw();
		}
	}
}

