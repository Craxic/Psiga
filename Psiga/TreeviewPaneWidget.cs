/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;

namespace Psiga
{
	[System.ComponentModel.ToolboxItem(false)]
	public abstract partial class TreeviewPaneWidget : Gtk.Bin
	{
		protected TreeStore Model { get; private set; }
		protected TreeView TreeView { get { return treeview; } }

		public TreeviewPaneWidget()
		{
			this.Build();

			InitializeTreeViewColumns();
			Model = InitializeModel();
			treeview.Model = Model;
			treeview.Selection.Changed += (object sender, EventArgs e) => UpdateRightPane();
		}

		protected void SetDividerPosition(int pos) {
			hpaned3.Position = pos;
		}

		protected void SetRightPaneChild(Widget newChild) {
			foreach (Widget child in rightPane.AllChildren) {
				if (child == newChild) {
					return;
				}
				rightPane.Remove(child);
				child.HideAll();
			}
			if (newChild != null) {
				rightPane.Add(newChild);
				newChild.ShowAll();
			}
		}

		protected abstract void UpdateRightPane();
		protected abstract TreeStore InitializeModel();
		protected abstract void InitializeTreeViewColumns();
	}
}

