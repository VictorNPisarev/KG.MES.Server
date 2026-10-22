using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;

namespace KG.MES.Sales.Desktop;

public class MainForm : Form
{
	private readonly WebView2 _webView;
	private readonly Process? _serverProcess;
	private const string AppVersion = "1.0.1"; // ← меняйте при каждом релизе

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

			await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.isWebView2 = true;");

			// 2. Проверяем версию и очищаем localStorage, если она изменилась
			// Этот скрипт выполнится ДО загрузки Blazor и его скриптов
			string clearScript = $@"
				(function() {{
					const currentVersion = '{AppVersion}';
					const storedVersion = localStorage.getItem('app_version') ?? 0;
					
					if (storedVersion !== currentVersion) {{
						console.log('Version changed from ' + storedVersion + ' to ' + currentVersion + '. Clearing localStorage.');
						const keysToRemove = [];
						for (let i = 0; i < localStorage.length; i++) {{
							const key = localStorage.key(i);
							// Оставляем device_id и license_key
							if (key !== 'device_id' && key !== 'license_key' && key !== 'app_version') {{
								keysToRemove.push(key);
							}}
						}}
						keysToRemove.forEach(k => localStorage.removeItem(k));
						localStorage.setItem('app_version', currentVersion);
					}}
				}})();
			";
			await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(clearScript);

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