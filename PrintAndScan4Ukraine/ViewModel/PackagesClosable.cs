using System;

namespace PrintAndScan4Ukraine.ViewModel
{
	public abstract class ClosableViewModel
	{
		public event EventHandler? ClosingRequest;

		protected void OnClosingRequest()
		{
			ClosingRequest?.Invoke(this, EventArgs.Empty);
		}
	}
}
