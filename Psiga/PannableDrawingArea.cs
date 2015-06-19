/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using Gtk;
using System.Collections.Generic;

namespace Psiga
{
	[System.ComponentModel.ToolboxItem(true)]
	public class PannableDrawingArea : DrawingArea
	{
		private const double SCROLL_FACTOR = 0.9;

		private Shape shape;
		public Shape Shape { 
			get {
				return shape;
			}
			set {
				shape = value;
				QueueDraw();
			}
		}

		double scale = 1;
		double transformX = 0;
		double transformY = 0;

		public PannableDrawingArea() {
			AddEvents((int)Gdk.EventMask.AllEventsMask);
		}

		public void ResetTransforms() {
			transformY = transformX = 0;
			scale = 1;
			QueueDraw();
		}

		protected override bool OnExposeEvent(Gdk.EventExpose evnt)
		{
			base.OnExposeEvent(evnt);

			Cairo.Context context = Gdk.CairoHelper.Create(GdkWindow);
			context.Save();
			context.Translate(transformX, transformY);
			context.Scale(scale, scale);

			if (shape != null) {
				shape.Draw(context, scale);
			}

			((IDisposable)context).Dispose();

			return true;
		}

		public void ScreenToDrawingCoords(double sx, double sy, out double dx, out double dy) {
			dx = (sx - transformX) / scale;
			dy = (sy - transformY) / scale;
		}

		public void DrawingToScreenCoords(double dx, double dy, out double sx, out double sy) {
			sx = dx * scale + transformX;
			sy = dx * scale + transformY;
		}

		bool isLeftDown = false;
		double lastMouseX = 0;
		double lastMouseY = 0;

		protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
		{
			if (evnt.Button == 1) 
				isLeftDown = true;
			return base.OnButtonPressEvent(evnt);
		}

		protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
		{
			if (evnt.Button == 1) 
				isLeftDown = false;
			return base.OnButtonReleaseEvent(evnt);
		}

		protected override bool OnMotionNotifyEvent(Gdk.EventMotion evnt)
		{
			if (isLeftDown) {
				transformX += evnt.X - lastMouseX;
				transformY += evnt.Y - lastMouseY;
				QueueDraw();
			}
			lastMouseX = evnt.X;
			lastMouseY = evnt.Y;
			return base.OnMotionNotifyEvent(evnt);
		}

		protected override bool OnScrollEvent(Gdk.EventScroll evnt)
		{
			if (evnt.Direction == Gdk.ScrollDirection.Down || 
				evnt.Direction == Gdk.ScrollDirection.Up) {
				double startX, startY;
				ScreenToDrawingCoords(evnt.X, evnt.Y, out startX, out startY);
				if (evnt.Direction == Gdk.ScrollDirection.Down) {
					scale *= SCROLL_FACTOR;
				} else {
					scale /= SCROLL_FACTOR;
				}
				double endX, endY;
				ScreenToDrawingCoords(evnt.X, evnt.Y, out endX, out endY);

				transformX += (endX - startX) * scale;
				transformY += (endY - startY) * scale;

				QueueDraw();
				return true;
			}
			return false;
		}
	}
}

