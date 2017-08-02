/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using Gtk;
using System.Collections.Generic;
using System.Linq;
using PsigaPkgLib;

namespace Psiga
{
	[System.ComponentModel.ToolboxItem(true)]
	public class PackageWidget : TreeviewPaneWidget
	{
		private const int PACKAGE_NAME_COLUMN = 0;
		private const int PACKAGE_SIZE_COLUMN = 1;
		private const int PACKAGE_LOADED_COLUMN = 2;
		private const int PACKAGE_REF_COLUMN = 3;

		private EntriesWidget entriesWidget;
		private Button loadPackageWidget;
		private Label noSelectionWidget;
		private Label loadingWidget;
		private Label selectOneWidget;
		private bool isLoading;

		private StatusReceiver statusReceiver;
		public StatusReceiver StatusReceiver {
			get {
				return statusReceiver;
			}
			set {
				statusReceiver = value;
				entriesWidget.StatusReceiver = value;
			}
		}

		public PackageWidget() : base() {
			InitRightPanes();

			PackageManager.OnPackageLoad += PackageManager_OnPackageLoad;
			PackageManager.OnRootChanged += PackageManager_OnRootChanged;

			PackageManager_OnRootChanged();

			SetDividerPosition(400);
		}

		private void PackageManager_OnRootChanged() {
			UpdateModel();
		}

		private void InitRightPanes() {
			entriesWidget = new EntriesWidget();

			loadPackageWidget = new Button();
			loadPackageWidget.Clicked += Load_package_widget_Clicked;

			noSelectionWidget = new Label();
			noSelectionWidget.Justify = Justification.Center;
			noSelectionWidget.Text = "No selection.\n\nPlease select a valid package or manifest file on the left.";

			loadingWidget = new Label();
			loadingWidget.Justify = Justification.Center;
			loadingWidget.Text = "Loading packages...";

			selectOneWidget = new Label();
			selectOneWidget.Justify = Justification.Center;
			selectOneWidget.Text = "Please select only one package.";
		}

		private void LoadSelectedPackages() {
			if (isLoading) {
				/*var md = new MessageDialog(ParentWindow, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, "Currently loading packages, please wait.");
				md.ButtonReleaseEvent += (o, args) => {
					md.HideAll();
				};
				md.Show();*/
				return;
			}
			isLoading = true;

			// Get all the packages that are selected
			var rows = TreeView.Selection.GetSelectedRows();
			if (rows.Length != 0) {
				// Get all the names of the un-loaded packages.
				var packageNames = new HashSet<string>();
				foreach (var row in rows) {
					TreeIter iter;
					Model.GetIter(out iter, row);
					var packageRef = (PackageReference)Model.GetValue(iter, PACKAGE_REF_COLUMN);
					if (!PackageManager.IsLoaded(packageRef.Name)) {
						packageNames.Add(packageRef.Name);
					}
				}

				// Start loading
				PackageManager.AsyncLoadPackages(packageNames, (n, p) => {
					Gtk.Application.Invoke((s, args) => {
						if (string.IsNullOrEmpty(n)) {
							if (StatusReceiver != null) {
								StatusReceiver.OnStatusReceived("", 0);
							}
							isLoading = false;
						} else {
							if (StatusReceiver != null) {
								StatusReceiver.OnStatusReceived("Loading " + n, p);
							}
						}
						UpdateRightPane();
					});
				});
			}
		}

		void Load_package_widget_Clicked(object sender, EventArgs e)
		{
			LoadSelectedPackages();
		}

		protected override void InitializeTreeViewColumns() {
			TreeView.Selection.Mode = SelectionMode.Multiple;

			var nameCol = TreeView.AppendColumn("Package Name", new CellRendererText(), "text", PACKAGE_NAME_COLUMN);
			var sizeCol = TreeView.AppendColumn("Size", new CellRendererText(), "text", PACKAGE_SIZE_COLUMN);
			var loadCol = TreeView.AppendColumn("Loaded", new CellRendererToggle(), "active", PACKAGE_LOADED_COLUMN);

			nameCol.Expand = true;
			nameCol.Clickable = true;
			nameCol.SortColumnId = PACKAGE_NAME_COLUMN;
			nameCol.SortIndicator = true;

			sizeCol.Expand = false;
			sizeCol.Clickable = true;
			sizeCol.SortColumnId = PACKAGE_SIZE_COLUMN;
			sizeCol.SortIndicator = true;

			loadCol.Expand = false;
			loadCol.Clickable = true;
			loadCol.SortColumnId = PACKAGE_LOADED_COLUMN;
			loadCol.SortIndicator = true;
		}

		protected override TreeStore InitializeModel() {
			var model = new Gtk.TreeStore(typeof(string), typeof(string), typeof(bool), typeof(PackageReference));
			model.SetSortFunc(PACKAGE_NAME_COLUMN, (m, x, y) => Util.MapSort<string>(m, x, y, PACKAGE_NAME_COLUMN, string.Compare));
			model.SetSortFunc(PACKAGE_SIZE_COLUMN, (m, x, y) => Util.MapSort<PackageReference>(m, x, y, PACKAGE_REF_COLUMN, PackageSizeCompare));
			model.SetSortFunc(PACKAGE_LOADED_COLUMN, (m, x, y) => Util.MapSort<bool>(m, x, y, PACKAGE_LOADED_COLUMN, BooleanCompare));
			model.SetSortColumnId(PACKAGE_NAME_COLUMN, SortType.Descending);
			return model;
		}

		static int BooleanCompare(bool x, bool y) {
			return -x.CompareTo(y);
		}

		static int PackageSizeCompare(PackageReference ref1, PackageReference ref2) {
			return Math.Sign(PackageManager.GetPackageSize(ref2.Name) - PackageManager.GetPackageSize(ref1.Name));
		}

		protected override void UpdateRightPane() {
			// If we're loading, show it.
			if (isLoading) {
				SetRightPaneChild(loadingWidget);
				return;
			}

			var rows = TreeView.Selection.GetSelectedRows();
			if (rows.Length != 0) {
				var packageNames = new HashSet<string>();
				var packageRefs = new List<PackageReference>();
				foreach (var row in rows) {
					TreeIter iter;
					Model.GetIter(out iter, row);
					var packageRef = (PackageReference)Model.GetValue(iter, PACKAGE_REF_COLUMN);
					packageRefs.Add(packageRef);
					packageNames.Add(packageRef.Name);
				}

				// Are all these packages loaded?
				if (packageNames.All(x => PackageManager.IsLoaded(x))) {
					if (packageNames.Count == 1) {
						entriesWidget.Packages = packageRefs.ToList();
						SetRightPaneChild(entriesWidget);
					} else {
						SetRightPaneChild(selectOneWidget);
					}
				} else {
					var countUnloaded = packageNames.Sum(x => PackageManager.IsLoaded(x) ? 0 : 1);
					var sizeUnloaded = packageNames.Sum(x => PackageManager.IsLoaded(x) ? 0 : PackageManager.GetPackageSize(x));
					loadPackageWidget.Label = string.Format(
						"Load in {0} package{1} ({2})", 
						countUnloaded,
						countUnloaded == 1 ? "" : "s",
						Util.NumBytesToString(sizeUnloaded)
					);
					SetRightPaneChild(loadPackageWidget);
				}
			} else {
				SetRightPaneChild(noSelectionWidget);
			}
		}

		void UpdateModel() {
			Model.Clear();
			if (PackageManager.Packages != null) {
				foreach (var package in PackageManager.Packages) {
					var size = Util.NumBytesToString(PackageManager.GetPackageSize(package));
					var loaded = PackageManager.IsLoaded(package);
					Gtk.TreeIter iterator = 
					Model.AppendValues(          package,                   size, loaded, new PackageReference(package, PackageReference.Files.Both));
					Model.AppendValues(iterator, package + ".pkg_manifest", "",   loaded, new PackageReference(package, PackageReference.Files.Manifest));
					Model.AppendValues(iterator, package + ".pkg",          "",   loaded, new PackageReference(package, PackageReference.Files.Package));
				}
			}
			UpdateRightPane();
		}

		void UpdateLoadStatusIter(TreeIter iter) {
			do {
				// Update load status of this row
				var package = (PackageReference)Model.GetValue(iter, 3);
				var loaded = PackageManager.IsLoaded(package.Name);
				Model.SetValue(iter, 2, loaded);

				// Update load status of any children
				TreeIter childrenIter;
				if (Model.IterChildren(out childrenIter, iter)) {
					UpdateLoadStatusIter(childrenIter);
				}
			} while (Model.IterNext(ref iter));
		}

		void PackageManager_OnPackageLoad(string loadedPackageName, PsigaPkgLib.Package loadedPackage)
		{
			Gtk.Application.Invoke((s, e) => {
				// Just update the load status of all packages.
				TreeIter iter;
				Model.GetIterFirst(out iter);

				UpdateLoadStatusIter(iter);
				UpdateRightPane();
			});
		}
	}
}

