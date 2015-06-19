/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using Gtk;
using System.Reflection;
using System.Linq;
using System.ComponentModel;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using PsigaXnbLib;

namespace Psiga
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class PropertyView : Gtk.Bin
	{
		private static readonly HashSet<Type> EXPAND_TYPES = new HashSet<Type> {
			typeof(Texture)
		};

		private static readonly HashSet<Type> IGNORE_ENUMERABLES = new HashSet<Type> {
			typeof(string),
			typeof(byte[]),
		};

		private const int TYPE_COLUMN = 0;
		private const int NAME_COLUMN = 1;
		private const int VALUE_COLUMN = 2;

		private TreeStore model;

		public PropertyView()
		{
			this.Build();

			InitializeTreeViewColumns();
			InitializeModel();
		}

		object viewing = null;
		public object Viewing {
			get { 
				return viewing;
			} 
			set {
				viewing = value; 
				UpdateTreeView();
			}
		}

		private bool ShouldShowProperty(PropertyInfo propertyInfo) {
			var attribs = Attribute.GetCustomAttributes(propertyInfo, typeof(EditorBrowsableAttribute));
			foreach (var prop in attribs) {
				var propCast = prop as EditorBrowsableAttribute;
				if (propCast.State == EditorBrowsableState.Never)
					return false;
			}
			return true;
		}

		public bool IsValueType(object obj) {
			return obj != null && obj.GetType().IsValueType;
		}

		private void AppendProperties(TreeIter iter, object viewing) {
			foreach (var propertyInfo in viewing.GetType().GetProperties()) {
				if (ShouldShowProperty(propertyInfo)) {
					var v = propertyInfo.GetValue(viewing);
					List<object> vl = null;
					if (v != null && !(IGNORE_ENUMERABLES.Contains(v.GetType()))) {
						var ve = v as IEnumerable;
						if (ve != null) {
							vl = new List<object>();
							foreach (var item in ve) {
								vl.Add(item);
							}
						}
					}

					var valueString = v == null ? "(null)" : v.ToString();
					if (vl != null) {
						valueString = "IEnumerable (Length = " + vl.Count + ")";
					}

					TreeIter childIter;
					if (iter.Equals(TreeIter.Zero)) {
						childIter = model.AppendValues(propertyInfo.PropertyType.Name, propertyInfo.Name, valueString);
					} else {
						childIter = model.AppendValues(iter, propertyInfo.PropertyType.Name, propertyInfo.Name, valueString);
					}
					
					if (vl != null) {
						int i = 0;
						foreach (var item in vl) {
							string itemType = "(null type)";
							string itemString = "(null)";
							if (item != null) {
								itemType = item.GetType().Name;
								itemString = item.ToString();
							}
							AppendProperties(model.AppendValues(childIter, itemType, "Item " + i.ToString("0000"), itemString), item);
							i++;
						}
					} else if (v != null && EXPAND_TYPES.Contains(v.GetType())) {
						AppendProperties(childIter, v);
					}
				}
			}
		}

		private void UpdateTreeView() {
			treeview.Model = null;
			model.Clear();
			AppendProperties(TreeIter.Zero, Viewing);
			treeview.Model = model;
		}

		private void InitializeTreeViewColumns() {
			treeview.Selection.Mode = SelectionMode.Multiple;

			var typeCol = treeview.AppendColumn("Type", new CellRendererText(), "text", TYPE_COLUMN);
			var nameCol = treeview.AppendColumn("Name", new CellRendererText(), "text", NAME_COLUMN);
			var valueCol = treeview.AppendColumn("Value", new CellRendererText(), "text", VALUE_COLUMN);

			typeCol.Expand = false;
			typeCol.Clickable = true;
			typeCol.SortColumnId = TYPE_COLUMN;
			typeCol.SortIndicator = true;

			nameCol.Expand = true;
			nameCol.Clickable = true;
			nameCol.SortColumnId = NAME_COLUMN;
			nameCol.SortIndicator = true;

			valueCol.Expand = true;
			valueCol.Clickable = true;
			valueCol.SortColumnId = VALUE_COLUMN;
			valueCol.SortIndicator = true;
		}

		private void InitializeModel() {
			model = new Gtk.TreeStore(typeof(string), typeof(string), typeof(string), typeof(PropertyInfo));
			model.SetSortFunc(TYPE_COLUMN, (m, x, y) => Util.MapSort<string>(m, x, y, TYPE_COLUMN, string.Compare));
			model.SetSortFunc(NAME_COLUMN, (m, x, y) => Util.MapSort<string>(m, x, y, NAME_COLUMN, string.Compare));
			model.SetSortFunc(VALUE_COLUMN, (m, x, y) => Util.MapSort<object>(m, x, y, VALUE_COLUMN, ObjectCompare));
			model.SetSortColumnId(NAME_COLUMN, SortType.Descending);
		}

		private int ObjectCompare(object a, object b) {
			IComparable ac = a as IComparable;
			IComparable bc = b as IComparable;
			if (ac == bc) {
				return 0;
			}
			if (ac == null) {
				return 1;
			}
			if (bc == null) {
				return -1;
			}
			return ac.CompareTo(bc);
		}
	}
}

