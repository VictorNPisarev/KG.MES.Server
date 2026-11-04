using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;

namespace KG.MES.Sales.Desktop;

internal static class Program
{
	[STAThread]
	static void Main()
	{
		ApplicationConfiguration.Initialize();

		// 1. Запускаем Blazor-сервер в фоне
		var serverProcess = StartBlazorServer();

		// 2. Ждём, пока сервер поднимется
		if (!WaitForServer("http://localhost:5000/sales", 30))
		{
			MessageBox.Show("Не удалось запустить сервер приложения.", "Ошибка",
				MessageBoxButtons.OK, MessageBoxIcon.Error);
			serverProcess?.Kill();
			return;
		}

		// 3. Открываем окно с WebView2
		var form = new MainForm(serverProcess);
		Application.Run(form);

		// 4. При закрытии окна — убиваем сервер
		try
		{
			if (serverProcess != null && !serverProcess.HasExited)
				serverProcess.Kill();
		}
		catch { }
	}

	private static Process? StartBlazorServer()
	{
		var serverPath = Path.Combine(AppContext.BaseDirectory, "server", "KG.MES.Sales.exe");

		if (!File.Exists(serverPath))
		{
			MessageBox.Show($"Файл сервера не найден:\n{serverPath}", "Ошибка",
				MessageBoxButtons.OK, MessageBoxIcon.Error);
			return null;
		}

		var psi = new ProcessStartInfo
		{
			FileName = serverPath,
			WorkingDirectory = Path.GetDirectoryName(serverPath)!,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			Environment =
			{
				["ASPNETCORE_ENVIRONMENT"] = "Production",
				["ASPNETCORE_URLS"] = "http://localhost:5000"
			}
		};

		return Process.Start(psi);
	}

	private static bool WaitForServer(string url, int timeoutSeconds)
	{
		using var http = new HttpClient();
		var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

		while (DateTime.UtcNow < deadline)
		{
			try
			{
				var response = http.GetAsync(url).Result;
				if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect)
					return true;
			}
			catch { }

			Thread.Sleep(500);
		}

		return false;
	}
}