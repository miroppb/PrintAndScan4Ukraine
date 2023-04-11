using System;

namespace PrintAndScan4Ukraine.ViewModel
{
	public abstract class ClosableViewModel
	{
		public event EventHandler? ClosingRequest;

		protected void OnClosingRequest()
		{
			if (ClosingRequest != null)
			{
				ClosingRequest(this, EventArgs.Empty);
			}
		}
	}
}
