// ============================================================================
// 
// 一連の更新確認を実施するクラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Updater.Models.SharedMisc;

namespace Updater.Models
{
	public class UpdateSequence
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public UpdateSequence(UpdaterLauncher launchParams)
		{
			_params = launchParams;

			// 表示名の設定
			_displayName = "「" + (String.IsNullOrEmpty(_params.Name) ? _params.ID : _params.Name) + "」";
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 一連の更新確認を実施
		// --------------------------------------------------------------------
		public Task RunAsync()
		{
			return Task.Run(() => RunCoreAsync());
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 本来 UpdaterLauncher は起動用だが、ここでは引数管理用として使用
		private UpdaterLauncher _params;

		// 表示名
		private String _displayName;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 最新情報の確認
		// --------------------------------------------------------------------
		private (Boolean result, String errorMessage) CheckLatestInfo()
		{
			Boolean result = false;
			String errorMessage = String.Empty;

			try
			{
				PrepareLatest();
#if false
				AskDisplayLatest();
				DisplayLatest();
#endif
				result = true;

				// 最新情報を正しく表示できたら、それがメッセージ代わりなので、別途のメッセージ表示はしない
			}
			catch (Exception oExcep)
			{
				errorMessage = "【最新情報の確認】\n" + oExcep.Message;
			}

			return (result, errorMessage);
		}

		// --------------------------------------------------------------------
		// 最新情報の確認と表示準備
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void PrepareLatest()
		{
#if false
			UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Information, _displayName + "の最新情報を確認中...");

			// RSS チェック
			RssManager aRssManager = new RssManager();
			SetRssManager(aRssManager, FILE_NAME_LATEST_SUFFIX);
			mLogWriter.ShowLogMessage(TraceEventType.Verbose, "PrepareLatest() location: " + mParams.LatestRss);
			String aErr;
			if (!aRssManager.ReadLatestRss(mParams.LatestRss, out aErr))
			{
				throw new Exception(aErr);
			}
			aRssManager.GetNewItems(out mNewItems);

			// 更新
			aRssManager.UpdatePastRss();
			aRssManager.Save();

			// 分析
			if (mNewItems.Count == 0)
			{
				throw new Exception("最新情報はありませんでした。");
			}
			LogAndSendAndShowMessage(TraceEventType.Information, mDisplayName + "の最新情報が "
					+ mNewItems.Count.ToString() + " 件見つかりました。", false);
#endif
		}

		// --------------------------------------------------------------------
		// 一連の更新確認を実施
		// --------------------------------------------------------------------
		private async Task RunCoreAsync()
		{
			try
			{
				Boolean latestResult = false;
				Boolean updateResult = false;
				String latestErr = String.Empty;
				String updateErr = String.Empty;

				// 待機
				if (_params.Wait > 0)
				{
					UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Information, _params.Wait.ToString() + " 秒待機します...");
					await Task.Delay(_params.Wait * 1000);
				}

#if false
				// 最新情報確認
				if (_params.IsLatestMode())
				{
					latestResult = CheckLatestInfo(out latestErr);
				}

				// 自動更新
				if (_params.IsUpdateMode())
				{
					updateResult = CheckUpdate(out updateErr);
				}

				if (!latestResult && !updateResult)
				{
					// 片方でも正常に終了していればそこでメッセージが表示される
					// どちらも正常に終了していない場合のみメッセージを表示する
					LogAndSendAndShowMessage(TraceEventType.Error, latestErr + "\n\n" + updateErr, true);
					aTotalResult = false;
				}
#endif
			}
			catch (Exception excep)
			{
				UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Error, "更新確認時エラー：\n" + excep.Message);
				UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			finally
			{
				UpdCommon.NotifyDisplayedIfNeeded(_params);
			}

		}

	}
}
