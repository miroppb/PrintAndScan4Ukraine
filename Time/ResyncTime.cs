using miroppb;
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
			Libmiroppb.Log("We are admin!");
			ServiceController serviceController = new("w32time");
			if (serviceController.Status != ServiceControllerStatus.Running)
			{
				Libmiroppb.Log("Starting w32time Service");
				serviceController.Start();
			}
		}

		private static bool SyncDateTime()
		{
			try
			{
				Libmiroppb.Log("Sending a resync request");
				using Process processTime = new();
				processTime.StartInfo.FileName = "w32tm";
				processTime.StartInfo.Arguments = "/resync";
				processTime.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				processTime.Start();
				processTime.WaitForExit();

				Libmiroppb.Log("Request was successful");
				return true;
			}
			catch (Exception ex)
			{
				// Handle any exceptions here
				Libmiroppb.Log($"Error with request: {ex.Message}");
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
