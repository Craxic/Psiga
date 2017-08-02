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

namespace Psiga
{
	[System.ComponentModel.ToolboxItem(true)]
	public class EntriesWidget : TreeviewPaneWidget
	{
		public const int ENTRY_ICON_COLUMN = 0;
		public const int ENTRY_TYPE_COLUMN = 1;
		public const int ENTRY_NAME_COLUMN = 2;
		public const int ENTRY_INDEX_COLUMN = 3;
		public const int ENTRY_REF_COLUMN = 4;

		private Dictionary<string, PackageReference.Files> packages;

		private Dictionary<EntryType, Pixbuf> typeImages;
		private PropertyView propertyView;
		private TextureWidget textureWidget;

		private StatusReceiver statusReceiver;
		public StatusReceiver StatusReceiver {
			get {
				return statusReceiver;
			}
			set {
				statusReceiver = value;
				textureWidget.StatusReceiver = value;
			}
		}

		public List<PackageReference> Packages {
			get {
				return packages.Select(x => new PackageReference(x.Key, x.Value)).ToList();
			}
			set {
				packages.Clear();
				foreach (var p in value) {
					if (packages.ContainsKey(p.Name)) {
						if (packages[p.Name] != p.ReferencedFiles) {
							packages[p.Name] = PackageReference.Files.Both;
						}
					} else {
						packages.Add(p.Name, p.ReferencedFiles);
					}
				}
				UpdateModel();
			}
		}

		public EntriesWidget()
		{
			packages = new Dictionary<string, PackageReference.Files>();
			typeImages = new Dictionary<EntryType, Pixbuf>() {
				{ EntryType.Atlas, new Pixbuf("./img/atlas.png") },
				{ EntryType.Bink, new Pixbuf("./img/bink.png") },
				{ EntryType.BinkAtlas, new Pixbuf("./img/bink-atlas.png") },
				{ EntryType.IncludePackage, new Pixbuf("./img/include-package.png") },
				{ EntryType.Texture, new Pixbuf("./img/texture.png") },
				{ EntryType.Texture3D, new Pixbuf("./img/texture3d.png") },
				{ EntryType.Spine, new Pixbuf("./img/spine.png") }
			};
			propertyView = new PropertyView();
			textureWidget = new TextureWidget();

			SetDividerPosition(600);
		}

		protected override void UpdateRightPane() {
			var rows = TreeView.Selection.GetSelectedRows();
			if (rows.Length == 1) {
				TreeIter iter;
				Model.GetIter(out iter, rows[0]);
				var entry = ((EntryReference)Model.GetValue(iter, ENTRY_REF_COLUMN)).Dereference();
				if (entry is TextureEntry) {
					textureWidget.Viewing = entry as TextureEntry;
					SetRightPaneChild(textureWidget);
					textureWidget.ShowPreview();
				} else {
					propertyView.Viewing = entry;
					SetRightPaneChild(propertyView);
				}
			} else {
				SetRightPaneChild(null);
			}
		}

		private void UpdateModel() {
			TreeView.Model = null;
			Model.Clear();
			foreach (var package in packages) {
				Package p;
				if (!PackageManager.LoadedPackages.TryGetValue(package.Key, out p)) {
					continue;
				}
				var references = new List<EntryReference>();
				if (package.Value == PackageReference.Files.Both || package.Value == PackageReference.Files.Manifest) {
					var entries = p.ManifestContents;
					for (int i = 0; i < entries.Count; i++) {
						references.Add(new EntryReference(i, new PackageReference(package.Key, PackageReference.Files.Manifest)));
					}
				}
				if (package.Value == PackageReference.Files.Both || package.Value == PackageReference.Files.Package) {
					var entries = p.PackageContents;
					for (int i = 0; i < entries.Count; i++) {
						references.Add(new EntryReference(i, new PackageReference(package.Key, PackageReference.Files.Package)));
					}
				}
				foreach (var reference in references) {
					Model.AppendValues(typeImages[reference.Type], reference.Type.ToString(), reference.Name, reference.EntryIndex.ToString(), reference);
				}
			}
			TreeView.Model = Model;
			UpdateRightPane();
		}

		protected override void InitializeTreeViewColumns() {
			TreeView.Selection.Mode = SelectionMode.Multiple;

			var iconCol = TreeView.AppendColumn("", new CellRendererPixbuf(), "pixbuf", ENTRY_ICON_COLUMN);
			var typeCol = TreeView.AppendColumn("Type", new CellRendererText(), "text", ENTRY_TYPE_COLUMN);
			var nameCol = TreeView.AppendColumn("Name", new CellRendererText(), "text", ENTRY_NAME_COLUMN);
			var indexCol = TreeView.AppendColumn("Index", new CellRendererText(), "text", ENTRY_INDEX_COLUMN);

			iconCol.Expand = false;
			iconCol.Clickable = true;
			iconCol.SortColumnId = ENTRY_ICON_COLUMN;
			iconCol.SortIndicator = true;
			iconCol.FixedWidth = 16;

			typeCol.Expand = false;
			typeCol.Clickable = true;
			typeCol.SortColumnId = ENTRY_TYPE_COLUMN;
			typeCol.SortIndicator = true;

			nameCol.Expand = true;
			nameCol.Clickable = true;
			nameCol.SortColumnId = ENTRY_NAME_COLUMN;
			nameCol.SortIndicator = true;

			indexCol.Expand = false;
			indexCol.Clickable = true;
			indexCol.SortColumnId = ENTRY_INDEX_COLUMN;
			indexCol.SortIndicator = true;
		}

		protected override TreeStore InitializeModel() {
			var model = new Gtk.TreeStore(typeof(Pixbuf), typeof(string), typeof(string), typeof(string), typeof(EntryReference));
			model.SetSortFunc(ENTRY_ICON_COLUMN, (m, x, y) => Util.MapSort<string>(m, x, y, ENTRY_TYPE_COLUMN, string.Compare));
			model.SetSortFunc(ENTRY_TYPE_COLUMN, (m, x, y) => Util.MapSort<string>(m, x, y, ENTRY_TYPE_COLUMN, string.Compare));
			model.SetSortFunc(ENTRY_NAME_COLUMN, (m, x, y) => Util.MapSort<string>(m, x, y, ENTRY_NAME_COLUMN, string.Compare));
			model.SetSortFunc(ENTRY_INDEX_COLUMN, (m, x, y) => Util.MapSort<EntryReference>(m, x, y, ENTRY_REF_COLUMN, IndexCompare));
			model.SetSortColumnId(ENTRY_INDEX_COLUMN, SortType.Descending);
			return model;
		}

		private int IndexCompare(EntryReference entryref1, EntryReference entryref2) {
			if (entryref1 == entryref2) {
				return 0;
			}
			if (entryref1 == null) {
				return 1;
			}
			if (entryref2 == null) {
				return -1;
			}
			return entryref1.EntryIndex - entryref2.EntryIndex;
		}
	}
}

