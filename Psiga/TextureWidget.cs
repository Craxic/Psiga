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

namespace Psiga
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class TextureWidget : Gtk.Bin, Shape
	{
		private TextureEntry viewing;
		public TextureEntry Viewing {
			get {
				return viewing;
			}
			set {
				viewing = value;
				propertyview.Viewing = viewing;
				LoadTextureAsync();
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
			(new Thread(() => {
				var dc = viewing.Texture.GetDecompressedARGB32PreMul();
				decompressed = dc;
				TriggerRedraw();
				FindAtlasses();
				TriggerRedraw();
			})).Start();
		}

		public unsafe void Draw(Cairo.Context context, double scale)
		{
			if (decompressed == null) {
				return;
			}
			context.MoveTo(0, 0);
			context.LineTo(0, viewing.Texture.Height);
			context.LineTo(viewing.Texture.Width, viewing.Texture.Height);
			context.LineTo(viewing.Texture.Width, 0);
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
				context.SetSource(buf);
				context.Fill();
				context.SetSourceRGB(1, 0, 0);
				buf.Dispose();
			}

			if (showAtlasBoxes.Active) {
				foreach (var e in atlasEntries) {
					foreach (var sa in e.Entries) {
						context.MoveTo(sa.Rect.Left, sa.Rect.Top);
						context.LineTo(sa.Rect.Right, sa.Rect.Top);
						context.LineTo(sa.Rect.Right, sa.Rect.Bottom);
						context.LineTo(sa.Rect.Left, sa.Rect.Bottom);
						context.LineTo(sa.Rect.Left, sa.Rect.Top);
					}
				}
				context.LineWidth = 1 / scale;
				context.Stroke();
			}

			if (showAtlasLabels.Active) {
				foreach (var e in atlasEntries) {
					foreach (var sa in e.Entries) {
						context.MoveTo(sa.Rect.Left, sa.Rect.Bottom);
						context.SetFontSize(16 / scale);
						context.ShowText(sa.Name);
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

			if (fileChooser.Run() == (int)ResponseType.Accept) 
			{
				if (decompressed != null) {
					var buf = new ImageSurface(
						decompressed, Format.Argb32, 
						viewing.Texture.Width, viewing.Texture.Height, 
						viewing.Texture.Width * 4);
					buf.WriteToPng(fileChooser.Filename);
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

