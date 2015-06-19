/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Gdk;
using PsigaPkgLib;
using PsigaPkgLib.Entries;
using PsigaPkgLib.Entries.Atlas;
using Cairo;

namespace Psiga
{
	[System.ComponentModel.ToolboxItem(true)]
	public class SubtexturesWidget : TreeviewPaneWidget, Shape
	{
		private class TreeNode
		{
			public SubAtlas Atlas { get; private set; }
			public string Package { get { return string.Join(", ", PackageNames); } }
			public string TexturePackage { get { return string.Join(", ", TexturePackageNames); } }
			private HashSet<string> PackageNames { get; set; }
			private HashSet<string> TexturePackageNames { get; set; }
			public string Name { get; private set; }
			public Dictionary<string, TreeNode> SubFolders { get; private set; }

			public TreeNode(string name) {
				Name = name;
				PackageNames = new HashSet<string>();
				TexturePackageNames = new HashSet<string>();
				SubFolders = new Dictionary<string, TreeNode>();
			}

			public void AddDirectory(TreeNode d) {
				SubFolders.Add(d.Name, d);
			}

			private void EnsurePath(SubAtlas atlas, string[] s, int index) {
				PackageNames.Add(atlas.Parent.Container.DisplayName);
				var texturePackage = PackageManager.GetTexturePackageName(atlas.Parent.ReferencedTextureName);
				if (texturePackage != null) {
					TexturePackageNames.Add(texturePackage);
				}

				if (index == s.Length) {
					// Leaf node.
					Atlas = atlas;
					return;
				}

				// Branch node.
				var folderToFind = s[index];
				if (!SubFolders.ContainsKey(folderToFind)) {
					AddDirectory(new TreeNode(folderToFind));
				}
				SubFolders[folderToFind].EnsurePath(atlas, s, index + 1);
			}

			public void EnsurePath(SubAtlas atlas) {
				EnsurePath(atlas, atlas.Name.Split(new char[] { '/', '\\' }), 0);
			}

			public void AppendTo(TreeStore model, TreeIter iter) {
				foreach (var d in SubFolders) {
					var dirIter = model.AppendValues(iter, d.Value.Name, d.Value.Package, d.Value.TexturePackage, d.Value.Atlas);
					d.Value.AppendTo(model, dirIter);
				}
			}
		}

		public const int NAME_COLUMN = 0;
		public const int PACKAGE_COLUMN = 1;
		public const int TEXTURE_PACKAGE_COLUMN = 2;
		public const int SUBATLAS_COLUMN = 3;

		private PannableDrawingArea drawingArea;

		public SubtexturesWidget()
		{
			SetDividerPosition(600);

			drawingArea = new PannableDrawingArea();
			drawingArea.Shape = this;
			SetRightPaneChild(drawingArea);

			PackageManager.OnPackageLoad += (n, p) => InvokeUpdateModel();
			PackageManager.OnRootChanged += InvokeUpdateModel;
			UpdateModel();
		}

		private void AddAtlasEntries(TreeNode root, IList<PsigaPkgLib.Entries.Entry> contents) {
			foreach (var e in contents) {
				AtlasEntry ae = e as AtlasEntry;
				if (ae != null) {
					foreach (var sa in ae.Entries) {
						root.EnsurePath(sa);
					}
				}
			}
		}

		private void InvokeUpdateModel() {
			Gtk.Application.Invoke((s, e) => {
				UpdateModel();
			});
		}

		private void UpdateModel() {
			TreeView.Model = null;
			Model.Clear();
			TreeNode root = new TreeNode("");
			lock (PackageManager.Lock) {
				foreach (var package in PackageManager.LoadedPackages) {
					AddAtlasEntries(root, package.Value.ManifestContents);
					AddAtlasEntries(root, package.Value.PackageContents);
				}
			}
			foreach (var d in root.SubFolders) {
				var iter = Model.AppendValues(d.Value.Name, d.Value.Package, d.Value.TexturePackage, d.Value.Atlas);
				d.Value.AppendTo(Model, iter);
			}
			TreeView.Model = Model;
			UpdateRightPane();
		}

		protected override void InitializeTreeViewColumns() {
			TreeView.Selection.Mode = SelectionMode.Single;

			var nameCol = TreeView.AppendColumn("Name", new CellRendererText(), "text", NAME_COLUMN);
			var packageCol = TreeView.AppendColumn("Atlas Package", new CellRendererText(), "text", PACKAGE_COLUMN);
			var texturePackageCol = TreeView.AppendColumn("Texture Package", new CellRendererText(), "text", TEXTURE_PACKAGE_COLUMN);

			nameCol.Expand = true;
			nameCol.Clickable = true;
			nameCol.SortColumnId = NAME_COLUMN;
			nameCol.SortIndicator = true;

			packageCol.Expand = true;
			packageCol.Clickable = true;
			packageCol.SortColumnId = PACKAGE_COLUMN;
			packageCol.SortIndicator = true;

			texturePackageCol.Expand = true;
			texturePackageCol.Clickable = true;
			texturePackageCol.SortColumnId = TEXTURE_PACKAGE_COLUMN;
			texturePackageCol.SortIndicator = true;
		}

		protected override TreeStore InitializeModel() {
			var model = new Gtk.TreeStore(typeof(string), typeof(string), typeof(string), typeof(SubAtlas));
			model.SetSortFunc(NAME_COLUMN, (m, x, y) => Util.MapSort<string>(m, x, y, NAME_COLUMN, string.Compare));
			model.SetSortFunc(PACKAGE_COLUMN, (m, x, y) => Util.MapSort<string>(m, x, y, PACKAGE_COLUMN, string.Compare));
			model.SetSortFunc(TEXTURE_PACKAGE_COLUMN, (m, x, y) => Util.MapSort<string>(m, x, y, TEXTURE_PACKAGE_COLUMN, string.Compare));
			return model;
		}

		protected override void UpdateRightPane() {
			drawingArea.QueueDraw();
			SelectedSubAtlas = null;
			AssociatedTex = null;
			if (TreeView == null || TreeView.Selection == null) {
				return;
			}
			var rows = TreeView.Selection.GetSelectedRows();
			if (rows.Length != 1) {
				return;
			}

			TreeIter iter;
			Model.GetIter(out iter, rows[0]);
			SelectedSubAtlas = ((SubAtlas)Model.GetValue(iter, SUBATLAS_COLUMN));
			if (SelectedSubAtlas == null) {
				return;
			}

			if (SelectedSubAtlas.Parent.IsReference) {
				var referencedTexture = SelectedSubAtlas.Parent.ReferencedTextureName;
				var textures = PackageManager.TextureEntries;
				if (textures.ContainsKey(referencedTexture)) {
					AssociatedTex = textures[referencedTexture].Dereference() as TextureEntry;
				} else {
					AssociatedTex = null;
				}
			} else {
				AssociatedTex = SelectedSubAtlas.Parent.IncludedTextureEntry;
			}
		}

		private SubAtlas SelectedSubAtlas { get; set; }
		private TextureEntry AssociatedTex { get; set; }

		public unsafe void Draw(Cairo.Context context, double scale)
		{
			if (SelectedSubAtlas == null || AssociatedTex == null) {
				return;
			}

			int offsetX = SelectedSubAtlas.TopLeft.X - SelectedSubAtlas.Rect.Left;
			int offsetY = SelectedSubAtlas.TopLeft.Y - SelectedSubAtlas.Rect.Top;

			int top = SelectedSubAtlas.Rect.Top + offsetY;
			int left = SelectedSubAtlas.Rect.Left + offsetX;
			int right = SelectedSubAtlas.Rect.Right + offsetX;
			int bottom = SelectedSubAtlas.Rect.Bottom + offsetY;

			context.MoveTo(left, top);
			context.LineTo(left, bottom);
			context.LineTo(right, bottom);
			context.LineTo(right, top);
			context.LineWidth = 0;

			{
				context.SetSourceRGB(0,0,0);
				context.FillPreserve();
			}

			fixed (byte * decompressedFixed = AssociatedTex.Texture.GetDecompressedARGB32PreMul())
			{
				var buf = new ImageSurface(
					(IntPtr)decompressedFixed, Format.ARGB32, 
					AssociatedTex.Texture.Width, AssociatedTex.Texture.Height, 
					AssociatedTex.Texture.Width * 4);
				
				context.SetSourceSurface(buf, -SelectedSubAtlas.Rect.Left + left, -SelectedSubAtlas.Rect.Top + top);
				context.Fill();
				context.SetSourceRGB(1, 0, 0);
				buf.Dispose();
			}
		}
	}
}

