using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Threading;

namespace uTILLIty.WPF.Demo
{
	/// <summary>
	///   Helper methods for the WPF application
	/// </summary>
	public static class ApplicationHelper
	{
		//private static IExceptionService GetExceptionService()
		//{
		//	if (!ServiceLocator.IsLocationProviderSet)
		//		return null;

		//	return ServiceLocator.Current.GetInstance<IExceptionService>();
		//}

		private static bool? _isInDesignMode;

		/// <summary>
		///   Executes the supplied <paramref name="action" /> on the UI thread,
		///   rethrowing any occuring exception
		/// </summary>
		public static void ExecuteOnUIThread(Action action)
		{
			Exception ex;
			ExecuteOnUIThread(action, out ex);
			if (ex != null)
				throw ex;
		}

		/// <summary>
		///   Executes the supplied <paramref name="action" /> on the UI thread,
		///   returning any occuring exception
		/// </summary>
		/// <returns>true, if the <paramref name="action" /> was executed successfully</returns>
		public static bool ExecuteOnUIThread(Action action, out Exception ex)
		{
			try
			{
				var inUIThread = InUIThread();
				if (inUIThread)
					action();
				else
				{
					Application.Current.Dispatcher.Invoke(action);
				}
				ex = null;
				return true;
			}
			catch (Exception theEx)
			{
				ex = theEx;
				//TODO: logging and ex-handling
				Debug.WriteLine(theEx.Message);
				//ex = null;
				//IExceptionService exService = GetExceptionService();
				//if (exService != null)
				//{
				//	var result = exService.HandleException(theEx, ExceptionPolicies.ApplicationHelper);
				//	if (result.Item1)
				//		throw;
				//	ex = result.Item2;
				//}
				return false;
			}
		}

		/// <summary>
		///   Returns true, if the current thread is the UI thread
		/// </summary>
		public static bool InUIThread()
		{
			var inUIThread = Application.Current == null
			                 || Application.Current.Dispatcher == null
			                 || Application.Current.Dispatcher.CheckAccess();
			return inUIThread;
		}

		/// <summary>
		///   Returns wether the code is currently being executed by a designer
		/// </summary>
		public static bool IsInDesignMode()
		{
			if (!_isInDesignMode.HasValue)
				_isInDesignMode =
					(bool) DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof (DependencyObject)).DefaultValue;
			return _isInDesignMode.Value;
		}

		/// <summary>
		///   Simulate Application.DoEvents function of System.Windows.Forms.Application class.
		/// </summary>
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public static void DoEvents()
		{
			var frame = new DispatcherFrame();
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
				new DispatcherOperationCallback(ExitFrames), frame);

			try
			{
				Dispatcher.PushFrame(frame);
			}
			catch (InvalidOperationException)
			{
			}
		}

		private static object ExitFrames(object frame)
		{
			((DispatcherFrame) frame).Continue = false;

			return null;
		}
	}
}