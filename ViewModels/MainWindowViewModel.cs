// ============================================================================
// 
// メインウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Diagnostics;

using Updater.Models.SharedMisc;
using Updater.Models.UpdaterModels;

namespace Updater.ViewModels
{
	public class MainWindowViewModel : UpdViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public MainWindowViewModel()
		{
		}

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();

			try
			{
				// タイトルバー
				Title = UpdConstants.APP_NAME_J;
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif
			}
			catch (Exception excep)
			{
				UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "メインウィンドウ初期化時エラー：\n" + excep.Message);
				UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
	}
}
