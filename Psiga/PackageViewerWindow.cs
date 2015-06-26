/// <summary>
/// Copyright (C) 2015 Matthew Ready.
/// </summary>
using System;
using Gtk;
using Psiga;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PsigaPkgLib;

public partial class PackageViewerWindow : Gtk.Window, StatusReceiver
{
	private SubtexturesWindow subtexturesWindow;

	public PackageViewerWindow() : base(Gtk.WindowType.Toplevel)
	{
		Build();

		openAction.Activated += OpenAction_Activated;
		saveAction.Activated += SaveAction_Activated;
		convertAction.Activated += ConvertAction_Activated;
		subtexturesAction.Toggled += SubtexturesAction_Toggled;
		packagewidget1.StatusReceiver = this;
	}

	void SubtexturesAction_Toggled (object sender, EventArgs e)
	{
		if (subtexturesAction.Active) {
			if (subtexturesWindow == null) {
				subtexturesWindow = new SubtexturesWindow();
			}
			subtexturesWindow.Show();
		} else {
			if (subtexturesWindow != null) {
				subtexturesWindow.HideAll();
			}
		}
	}

	public override void Dispose()
	{
		subtexturesWindow.HideAll();
		subtexturesWindow.Dispose();
		base.Dispose();
	}

	void ConvertAction_Activated (object sender, EventArgs e)
	{
		foreach (var kvp in PackageManager.LoadedPackages) {
			kvp.Value.WritePackageFiles("test_" + kvp.Key);
		}
	}

	void SaveAction_Activated (object sender, EventArgs e)
	{
		
	}

	void OpenAction_Activated (object sender, EventArgs e)
	{
		Gtk.FileChooserDialog fileChooser =
			new Gtk.FileChooserDialog("Select Transistor Package Directory",
				this,
				FileChooserAction.SelectFolder,
				"Cancel", ResponseType.Cancel,
				"Open", ResponseType.Accept);

		if (fileChooser.Run() == (int)ResponseType.Accept) 
		{
			PackageManager.SetTransistorRoot(fileChooser.Filename);
		}

		fileChooser.Destroy();
	}

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}

	public void OnStatusReceived(string statusText, double statusProgress)
	{
		Gtk.Application.Invoke((s, e) => {
			statusbar.Push(1, statusText);
			progressbar.Fraction = statusProgress;
		});
	}
}
