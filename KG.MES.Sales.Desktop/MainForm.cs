using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;

namespace KG.MES.Sales.Desktop;

public class MainForm : Form
{
	private readonly WebView2 _webView;
	private readonly Process? _serverProcess;

	public MainForm(Process? serverProcess)
	{
		_serverProcess = serverProcess;

		Text = "KG.MES Sales";
		Width = 1400;
		Height = 900;
		StartPosition = FormStartPosition.CenterScreen;
		WindowState = FormWindowState.Maximized;

		_webView = new WebView2
		{
			Dock = DockStyle.Fill
		};

		Controls.Add(_webView);

		Load += async (_, _) =>
		{
			await _webView.EnsureCoreWebView2Async();
			_webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
			_webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
			_webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
			_webView.Source = new Uri("http://localhost:5000/sales");
		};
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		try
		{
			if (_serverProcess != null && !_serverProcess.HasExited)
				_serverProcess.Kill();
		}
		catch { }

		base.OnFormClosing(e);
	}
}