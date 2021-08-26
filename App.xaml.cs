﻿// ============================================================================
// 
// アプリケーション
// 
// ============================================================================

// ----------------------------------------------------------------------------
//
// ----------------------------------------------------------------------------

using Livet;

using Shinta;

using System;
using System.Diagnostics;
using System.Windows;

using Updater.Models.UpdaterModels;

namespace Updater
{
	public partial class App : Application
	{
		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 多重起動防止用
		// アプリケーション終了までガベージコレクションされないようにメンバー変数で持つ
		//private Mutex? _mutex;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// スタートアップ
		// --------------------------------------------------------------------
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			// Livet コード
			DispatcherHelper.UIDispatcher = Dispatcher;

			// 集約エラーハンドラー設定
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

#if false
			// 多重起動チェック
			_mutex = CommonWindows.ActivateAnotherProcessWindowIfNeeded(Common.SHINTA + '_' + UpdConstants.APP_ID + '_' + UpdaterModel.Instance.EnvModel.ExeFullPath.Replace('\\', '/'));
			if (_mutex == null)
			{
				throw new MultiInstanceException();
			}
#endif
		}

		// --------------------------------------------------------------------
		// 集約エラーハンドラー
		// --------------------------------------------------------------------
		private void CurrentDomain_UnhandledException(Object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
		{
			if (unhandledExceptionEventArgs.ExceptionObject is MultiInstanceException)
			{
				// 多重起動の場合は何もしない
			}
			else
			{
				if (unhandledExceptionEventArgs.ExceptionObject is Exception excep)
				{
					// UpdaterModel 未生成の可能性があるためまずはメッセージ表示のみ
					MessageBox.Show("不明なエラーが発生しました。アプリケーションを終了します。\n" + excep.Message + "\n" 
							+ excep.InnerException?.GetType().Name + "\n" + excep.InnerException?.Message + "\n" + excep.StackTrace,
							"エラー", MessageBoxButton.OK, MessageBoxImage.Error);

					try
					{
						// 可能であればログする。UpdaterModel 生成中に例外が発生する可能性がある
						UpdaterModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "集約エラーハンドラー：\n" + excep.Message + "\n" + excep.InnerException?.Message);
						UpdaterModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
					}
					catch (Exception)
					{
						MessageBox.Show("エラーの記録ができませんでした。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}

			Environment.Exit(1);
		}
	}
}
