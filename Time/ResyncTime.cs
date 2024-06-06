using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;

namespace Time
{
	public class ResyncTime
	{
		private static bool IsAdministrator() => new WindowsPrincipal(WindowsIdentity.GetCurrent())
					.IsInRole(WindowsBuiltInRole.Administrator);

		private static void StartTimeService()
		{
			ServiceController serviceController = new("w32time");
			if (serviceController.Status != ServiceControllerStatus.Running)
			{
				serviceController.Start();
			}
		}

		private static bool SyncDateTime()
		{
			try
			{
				using Process processTime = new();
				processTime.StartInfo.FileName = "w32tm";
				processTime.StartInfo.Arguments = "/resync";
				processTime.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				processTime.Start();
				processTime.WaitForExit();

				return true;
			}
			catch (Exception)
			{
				// Handle any exceptions here
				return false;
			}
		}

		public static void TryToResyncTime()
		{
			if (IsAdministrator())
				StartTimeService();
			SyncDateTime();
		}
	}
}
