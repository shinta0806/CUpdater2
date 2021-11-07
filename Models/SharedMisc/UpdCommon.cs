// ============================================================================
// 
// ちょちょいと自動更新 2 全体で使う関数
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Diagnostics;

using Updater.Models.UpdaterModels;

namespace Updater.Models.SharedMisc
{
	public class UpdCommon
	{
		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 表示用のアプリケーション名
		// --------------------------------------------------------------------
		public static String DisplayName(UpdaterLauncher launchParams)
		{
			return "「" + (String.IsNullOrEmpty(launchParams.Name) ? launchParams.ID : launchParams.Name) + "」";
		}

		// --------------------------------------------------------------------
		// 通知対象ウィンドウに、UI を表示したことを通知（1 度だけ）
		// --------------------------------------------------------------------
		public static void NotifyDisplayedIfNeeded(UpdaterLauncher launchParams)
		{
			if (_notified || launchParams.NotifyHWnd == IntPtr.Zero)
			{
				return;
			}

			// 通知
			_notified = true;
			WindowsApi.PostMessage(launchParams.NotifyHWnd, UpdaterLauncher.WM_UPDATER_UI_DISPLAYED, IntPtr.Zero, IntPtr.Zero);
		}

		// --------------------------------------------------------------------
		// ログ表示を行い、かつ、呼びだし元アプリに表示したことを通知
		// --------------------------------------------------------------------
		public static void ShowLogMessageAndNotify(UpdaterLauncher launcherParams, TraceEventType eventType, String message)
		{
			if (String.IsNullOrEmpty(message))
			{
				return;
			}

			// 通知（表示される場合のみ）
			if (launcherParams.ForceShow)
			{
				switch (eventType)
				{
					case TraceEventType.Critical:
					case TraceEventType.Error:
					case TraceEventType.Warning:
					case TraceEventType.Information:
						UpdCommon.NotifyDisplayedIfNeeded(launcherParams);
						break;
				}
			}

			// ログおよび表示
			UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(eventType, message, !launcherParams.ForceShow);
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 通知対象ウィンドウに、UI を表示したことを通知したかどうか
		private static Boolean _notified = false;
	}
}
