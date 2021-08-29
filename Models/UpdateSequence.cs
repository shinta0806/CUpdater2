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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Updater.Models.SharedMisc;
using Updater.Models.UpdaterModels;

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
		// private メンバー定数
		// ====================================================================

		// 初代ちょちょいと自動更新と設定ファイルがかぶらないようにするためのプレフィックス
		private const String CONFIG_FILE_NAME_PREFIX = "Upd2_";

		// 設定保存ファイルのサフィックス
		private const String CONFIG_FILE_NAME_DUMMY_SUFFIX = "_Dummy";
		private const String CONFIG_FILE_NAME_LATEST_SUFFIX = "_Latest";
		private const String CONFIG_FILE_NAME_UPDATE_SUFFIX = "_Update";

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 本来 UpdaterLauncher は起動用だが、ここでは引数管理用として使用
		private UpdaterLauncher _params;

		// 表示名
		private String _displayName;

		// 最新情報
		private List<RssItem> _newItems = new();

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
			UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, _displayName + "の最新情報を確認中...");

			// RSS チェック
			RssManager rssManager = new(UpdaterModel.Instance.EnvModel.LogWriter);
			SetRssManager(rssManager, CONFIG_FILE_NAME_LATEST_SUFFIX);
			UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "PrepareLatest() location: " + _params.LatestRss);
			(Boolean result, String? errorMessage) = rssManager.ReadLatestRss(_params.LatestRss);
			if (!result)
			{
				throw new Exception(errorMessage);
			}
			_newItems = rssManager.GetNewItems();

			// 更新
			rssManager.UpdatePastRss();
			rssManager.Save();

			// 分析
			if (_newItems.Count == 0)
			{
				throw new Exception("最新情報はありませんでした。");
			}
			UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, _displayName + "の最新情報が " + _newItems.Count.ToString() + " 件見つかりました。");
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
				String latestErrorMessage = String.Empty;
				String updateErrorMessage = String.Empty;

				// 待機
				if (_params.Wait > 0)
				{
					UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, _params.Wait.ToString() + " 秒待機します...");
					await Task.Delay(_params.Wait * 1000);
				}

				// 最新情報確認
				if (_params.IsLatestMode())
				{
					(latestResult, latestErrorMessage) = CheckLatestInfo();
				}

#if false
				// 自動更新
				if (_params.IsUpdateMode())
				{
					updateResult = CheckUpdate(out updateErrorMessage);
				}
#endif

				if (!latestResult && !updateResult)
				{
					// 片方でも正常に終了していればそこでメッセージが表示される
					// どちらも正常に終了していない場合のみメッセージを表示する
					UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Error, latestErrorMessage + "\n\n" + updateErrorMessage);
				}
			}
			catch (Exception excep)
			{
				// CheckXXXX() が例外をハンドルするため、ここには到達しない想定
				UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Error, "更新確認時の予期しないエラー：\n" + excep.Message);
				UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
			finally
			{
				UpdCommon.NotifyDisplayedIfNeeded(_params);
			}
		}

		// --------------------------------------------------------------------
		// RSS マネージャーの設定を行う
		// --------------------------------------------------------------------
		private void SetRssManager(RssManager rssManager, String configFileSuffix)
		{
			// 既存設定の読込
			rssManager.SavePath = Common.UserAppDataFolderPath() + CONFIG_FILE_NAME_PREFIX + _params.ID + configFileSuffix + Common.FILE_EXT_CONFIG;
			rssManager.Load();

			// スレッド制御
			rssManager.CancellationToken = UpdaterModel.Instance.EnvModel.AppCancellationTokenSource.Token;

			// UA
			rssManager.UserAgent += " " + UpdConstants.APP_ID + "/" + Regex.Replace(UpdConstants.APP_VER, @"[^0-9\.]", "");
			UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "UA: " + rssManager.UserAgent);

#if DEBUG
			String guids = "SetRssManager() PastRssGuids:\n";
			foreach (String guid in rssManager.PastRssGuids)
			{
				guids += guid + "\n";
			}
			UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, guids);
#endif
		}

	}
}
