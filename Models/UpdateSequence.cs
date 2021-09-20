// ============================================================================
// 
// 一連の更新確認を実施するクラス
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Messaging;
using Shinta;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

using Updater.Models.Settings;
using Updater.Models.SharedMisc;
using Updater.Models.UpdaterModels;
using Updater.ViewModels;
using Updater.ViewModels.MiscWindowViewModels;

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
		public UpdateSequence(MainWindowViewModel mainWindowViewModel, UpdaterLauncher launchParams)
		{
			_mainWindowViewModel = mainWindowViewModel;
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

		// ファイル名
		private const String FILE_NAME_DOWNLOAD_ZIP = "Download.zip";

		// フォルダー名
		private const String FOLDER_NAME_NEW_ARCHIVE = "NewArchive\\";
		private const String FOLDER_NAME_NEW_EXTRACT = "NewExtract\\";
		private const String FOLDER_NAME_OLD = "Old\\";
		private const String FOLDER_NAME_UPDATE = "Update\\";

		// 自動更新用ファイルをダウンロードする回数（プログラムや RSS の不具合で永遠にダウンロードするのを防ぐ）
		private const Int32 DOWNLOAD_TRY_MAX = 5;

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// メインウィンドウのビューモデル
		private MainWindowViewModel _mainWindowViewModel;

		// 本来 UpdaterLauncher は起動用だが、ここでは引数管理用として使用
		private UpdaterLauncher _params;

		// 表示名
		private String _displayName;

		// 最新情報
		private List<RssItem> _newItems = new();

		// 更新情報
		private List<RssItem> _updateItems = new();

		// 更新制御情報
		private AutoUpdateSettings _autoUpdateSettings = new();

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ユーザーエージェントの追加部分
		// --------------------------------------------------------------------
		private String AdditionalUserAgent()
		{
			return " " + UpdConstants.APP_ID + "/" + Regex.Replace(UpdConstants.APP_VER, @"[^0-9\.]", "");
		}

		// --------------------------------------------------------------------
		// 最新情報の確認
		// --------------------------------------------------------------------
		private void AnalyzeUpdateRss()
		{
			// アップデートファイル情報のバージョンを揃える（不揃いのを切り捨てる）
			Int32 index = 1;
			while (index < _updateItems.Count)
			{
				if (_updateItems[index].Elements[RssManager.NODE_NAME_TITLE] != _updateItems[0].Elements[RssManager.NODE_NAME_TITLE])
				{
					_updateItems.RemoveAt(index);
					UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "AnalyzeRSS() バージョン不揃い: " + index.ToString());
				}
				else
				{
					index++;
				}
			}

			// 更新が必要かどうかの判定
			// 強制インストールの場合は常に必要判定
			if (_params.ForceInstall)
			{
				return;
			}

			// RSS に記載のバージョンが現在のバージョン以下なら不要
			if (StringUtils.StrAndNumCmp(_updateItems[0].Elements[RssManager.NODE_NAME_TITLE], _params.CurrentVer, true) <= 0)
			{
				throw new Exception("更新の必要はありません：更新版がありません（現行：" + _params.CurrentVer
						+ "、最新版：" + _updateItems[0].Elements[RssManager.NODE_NAME_TITLE] + "）");
			}

			// RSS に記載のバージョンが SkipVer 以下なら不要
			if (StringUtils.StrAndNumCmp(_updateItems[0].Elements[RssManager.NODE_NAME_TITLE], _autoUpdateSettings.SkipVer, true) <= 0)
			{
				throw new Exception("更新の必要はありません：ユーザーに不要と指定されたバージョンです（現行："
						+ _params.CurrentVer + "、不要版：" + _autoUpdateSettings.SkipVer + "）");
			}
		}

		// --------------------------------------------------------------------
		// 最新情報の確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void AskDisplayLatest()
		{
			UpdCommon.NotifyDisplayedIfNeeded(_params);
			if (MessageBox.Show(_displayName + "の最新情報が " + _newItems.Count.ToString() + " 件見つかりました。\n表示しますか？",
					"質問", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
			{
				throw new Exception("最新情報の表示を中止しました。");
			}
		}

		// --------------------------------------------------------------------
		// 更新するかユーザーに尋ねる
		// ＜返値＞ 更新するかどうか
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private Boolean AskUpdate()
		{
			Boolean isUpdate = false;

			UpdCommon.NotifyDisplayedIfNeeded(_params);

			// ViewModel 経由でウィンドウを開く
			using AskUpdateWindowViewModel askUpdateWindowViewModel = new();
			_mainWindowViewModel.Messenger.Raise(new TransitionMessage(askUpdateWindowViewModel, UpdConstants.MESSAGE_KEY_OPEN_ASK_UPDATE_WINDOW));

			switch (askUpdateWindowViewModel.ViewModelResult)
			{
				case MessageBoxResult.Yes:
					isUpdate = true;
					ShowInstallMessage();
					break;
				case MessageBoxResult.No:
					_autoUpdateSettings.SkipVer = _updateItems[0].Elements[RssManager.NODE_NAME_TITLE];
					_autoUpdateSettings.Save();
					UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "更新版（" + _updateItems[0].Elements[RssManager.NODE_NAME_TITLE] + "）はインストールしません。");
					break;
				default:
					UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "更新版（" + _updateItems[0].Elements[RssManager.NODE_NAME_TITLE] + "）は後でインストールします。");
					break;
			}

			return isUpdate;
		}

		// --------------------------------------------------------------------
		// 最新情報の確認
		// --------------------------------------------------------------------
		private async Task<(Boolean isMessageShown, String errorMessage)> CheckLatestInfoAsync()
		{
			Boolean isMessageShown = false;
			String errorMessage = String.Empty;

			try
			{
				await PrepareLatestAsync();
				AskDisplayLatest();
				DisplayLatest();
				isMessageShown = true;

				// 最新情報を正しく表示できたら、それがメッセージ代わりなので、別途のメッセージ表示はしない
			}
			catch (Exception excep)
			{
				errorMessage = "【最新情報の確認】\n" + excep.Message;
				UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}

			return (isMessageShown, errorMessage);
		}

		// --------------------------------------------------------------------
		// 自動更新の確認
		// --------------------------------------------------------------------
		private async Task<(Boolean isMessageShown, String errorMessage)> CheckUpdateAsync()
		{
			Boolean isMessageShown = false;
			String errorMessage = String.Empty;

			try
			{
				UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "CheckUpdate() relaunch path: " + _params.Relaunch);
				await PrepareUpdateAsync();

				Boolean isUpdate;
				if (_params.ForceInstall)
				{
					isUpdate = true;
					ShowInstallMessage();
				}
				else
				{
					isUpdate = AskUpdate();
				}
				isMessageShown = true;

				if (isUpdate)
				{
					_params.ForceShow = true;
					_mainWindowViewModel.ShowWindow();

					WaitTargetExit();
					await InstallUpdateAsync();
				}

#if false
				mParams.ForceShow = true;
				IntPtr aOldMainFormHandle = MainFormHandle;
				PostCommand(UpdaterCommand.ShowMainFormRequested);

				// ShowMainFormRequested により MainFormHandle が更新されるはずなので、それを待つ
				for (Int32 i = 0; i < WAIT_MAIN_FORM_HANDLE_CHANGE_MAX; i++)
				{
					if (MainFormHandle != aOldMainFormHandle)
					{
						mLogWriter.ShowLogMessage(TraceEventType.Verbose, "CheckUpdate() #" + i.ToString() + " で脱出");
						break;
					}
					Thread.Sleep(Common.GENERAL_SLEEP_TIME);
				}

				WaitTargetExit();
				InstallUpdate();

				String aOKMessage = String.Empty;
				aOKMessage = "更新版のインストールが完了しました。";
				if (!String.IsNullOrEmpty(mParams.Relaunch))
				{
					aOKMessage += "\n" + mDisplayName + "を再起動します。";
				}
				LogAndSendAndShowMessage(TraceEventType.Information, aOKMessage, true);
				result = true;
#endif
			}
			catch (Exception oExcep)
			{
				errorMessage = "【更新版の確認】\n" + oExcep.Message;
			}

#if false
			// 再起動
			if (result && !String.IsNullOrEmpty(mParams.Relaunch))
			{
				try
				{
					Process.Start(mParams.Relaunch);
				}
				catch
				{
					LogAndSendAndShowMessage(TraceEventType.Error, mDisplayName + "を再起動できませんでした。", true);
					errorMessage = mDisplayName + "を再起動できませんでした。";
					result = false;
				}
			}
#endif
			return (isMessageShown, errorMessage);
		}

		// --------------------------------------------------------------------
		// RSS マネージャーを生成
		// --------------------------------------------------------------------
		private RssManager CreateRssManager(String configFileSuffix)
		{
			RssManager rssManager = new(UpdaterModel.Instance.EnvModel.LogWriter, Common.UserAppDataFolderPath() + CONFIG_FILE_NAME_PREFIX + _params.ID + configFileSuffix + Common.FILE_EXT_CONFIG);

			// 既存設定の読込
			rssManager.Load();

			// スレッド制御
			rssManager.CancellationToken = UpdaterModel.Instance.EnvModel.AppCancellationTokenSource.Token;

			// UA
			rssManager.UserAgent += AdditionalUserAgent();
			UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "UA: " + rssManager.UserAgent);

#if DEBUG
			String guids = "SetRssManager() PastRssGuids:\n";
			foreach (String guid in rssManager.PastRssGuids)
			{
				guids += guid + "\n";
			}
			UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, guids);
#endif

			return rssManager;
		}
		// --------------------------------------------------------------------
		// 最新情報の確認
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void DisplayLatest()
		{
			Int32 numErrors = 0;

			foreach (RssItem newItem in _newItems)
			{
				try
				{
					UpdCommon.ShellExecute(newItem.Elements[RssManager.NODE_NAME_LINK]);
				}
				catch
				{
					// エラーでもとりあえずは続行
					numErrors++;
				}
			}
			if (numErrors == 0)
			{
				// 正常終了
				UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, _newItems.Count.ToString() + " 件の最新情報を表示完了。");
			}
			else if (numErrors < _newItems.Count)
			{
				throw new Exception("一部の最新情報を表示できませんでした。");
			}
			else
			{
				throw new Exception("最新情報を表示できませんでした。");
			}
		}

		// --------------------------------------------------------------------
		// 更新版をダウンロード
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private async Task DownloadUpdateArchiveAsync()
		{
			UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "更新版をダウンロードします。");

			// 試行回数チェック
			if (_autoUpdateSettings.DownloadVer == _updateItems[0].Elements[RssManager.NODE_NAME_TITLE]
				&& _autoUpdateSettings.DownloadTry >= DOWNLOAD_TRY_MAX
				&& !_params.ForceInstall)
			{
				throw new Exception("ダウンロード回数超過のため自動更新を中止しました。");
			}

			// 試行情報更新
			if (_autoUpdateSettings.DownloadVer == _updateItems[0].Elements[RssManager.NODE_NAME_TITLE])
			{
				_autoUpdateSettings.DownloadTry++;
			}
			else
			{
				_autoUpdateSettings.DownloadVer = _updateItems[0].Elements[RssManager.NODE_NAME_TITLE];
				_autoUpdateSettings.DownloadTry = 1;
			}
			_autoUpdateSettings.DownloadMD5 = _updateItems[0].Elements[RssManager.NODE_NAME_LINK + RssItem.RSS_ITEM_NAME_DELIMITER + RssManager.ATTRIBUTE_NAME_MD5];
			_autoUpdateSettings.Save();

			// ダウンロード保存用フォルダの初期化
			String? updateArchiveFolder = Path.GetDirectoryName(UpdateArchivePath());
			if (String.IsNullOrEmpty(updateArchiveFolder))
			{
				throw new Exception("更新版を保存するフォルダーを決定できません。");
			}
			try
			{
				Directory.Delete(updateArchiveFolder, true);
			}
			catch
			{
			}

			// 念のための（自分自身との）アクセス競合回避
			await Task.Delay(Common.GENERAL_SLEEP_TIME);
			Directory.CreateDirectory(updateArchiveFolder);

			// ミラー選択
			Int32 mirrorIndex = new Random().Next(_updateItems.Count);
			UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "DownloadUpdate() mirror index: " + mirrorIndex.ToString());
			UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "DownloadUpdate() URL: " + _updateItems[mirrorIndex].Elements[RssManager.NODE_NAME_LINK]);

			// ダウンロード
			Downloader downloader = new();
			downloader.CancellationToken = UpdaterModel.Instance.EnvModel.AppCancellationTokenSource.Token;
			downloader.UserAgent += AdditionalUserAgent();
			await downloader.DownloadAsFileAsync(_updateItems[mirrorIndex].Elements[RssManager.NODE_NAME_LINK], UpdateArchivePath());
			if (!IsUpdateArchiveMD5Valid())
			{
				throw new Exception("正常にダウンロードが完了しませんでした（内容にエラーがあります）。");
			}
			UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "更新版のダウンロードが完了しました。");
		}

		// --------------------------------------------------------------------
		// Download.zip 展開後のルートパス
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private String ExtractBasePath()
		{
			String[] files = Directory.GetFiles(ExtractPath(), "*");
			String[] folders = Directory.GetDirectories(ExtractPath(), "*");
			if (files.Length == 0 && folders.Length == 0)
			{
				throw new Exception("ダウンロードした更新版の内容が空です。");
			}

			// アーカイブの中身（直下）が ID 名で始まるフォルダのみであれば、そのフォルダがルートパス
			if (files.Length == 0 && folders.Length == 1
					&& String.Compare(Path.GetFileName(folders[0]), 0, _params.ID, 0, _params.ID.Length, true) == 0)
			{
				return folders[0] + "\\";
			}

			// それ以外なら、ExtractPath() 自体がルートパス
			return ExtractPath();
		}

		// --------------------------------------------------------------------
		// Download.zip を解凍するするフォルダー
		// --------------------------------------------------------------------
		private String ExtractPath()
		{
			return Common.TempFolderPath() + FOLDER_NAME_NEW_EXTRACT;
		}

		// --------------------------------------------------------------------
		// ファイル 1 つをインストールする
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private void InstallMove(String targetFile, String extractBasePath)
		{
			String middleName = targetFile.Substring(extractBasePath.Length);
			UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "InstallUpdate() middleName: " + middleName);
			_mainWindowViewModel.SetSubCaption(middleName);
			String destFile = Path.GetDirectoryName(UpdaterModel.Instance.EnvModel.ExeFullPath) + "\\" + middleName;
			Directory.CreateDirectory(Path.GetDirectoryName(destFile) ?? String.Empty);
			try
			{
				UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "InstallUpdate() deleting aDestFile: " + destFile);
				File.Delete(destFile);
				UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "InstallUpdate() moving: " + targetFile);
				File.Move(targetFile, destFile);
			}
			catch (Exception excep)
			{
				UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				throw new Exception("ファイルのインストールができませんでした：\n" + middleName + "\nファイルが実行中または使用中でないか確認して下さい。");
			}
		}

		// --------------------------------------------------------------------
		// Download.zip をインストールする
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private async Task InstallUpdateAsync()
		{
			_mainWindowViewModel.ShowInstallingMessage();

			// 過去のバックアップが残っていれば削除
			try
			{
				Directory.Delete(OldPath(), true);
			}
			catch
			{
			}

			// ロックに対する安全マージン
			await Task.Delay(Common.GENERAL_SLEEP_TIME);
			Directory.CreateDirectory(OldPath());

			// アーカイブ展開
			Directory.CreateDirectory(ExtractPath());
			try
			{
				ZipFile.ExtractToDirectory(UpdateArchivePath(), ExtractPath());
			}
			catch (Exception excep)
			{
				throw new Exception("ダウンロードしたアーカイブを解凍できませんでした：" + excep.Message);
			}
			await Task.Delay(Common.GENERAL_SLEEP_TIME);

			// 展開後のベースフォルダと全ファイル取得
			String extractBasePath = ExtractBasePath();
			UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "InstallUpdate() extract base folder: " + extractBasePath);
			String[] extractFiles = Directory.GetFiles(extractBasePath, "*", SearchOption.AllDirectories);

			// アーカイブを移動
			String? self = null;
			Int32 count = 0;
			foreach (String file in extractFiles)
			{
				if (String.Compare(Path.GetFileName(file), Path.GetFileName(UpdaterModel.Instance.EnvModel.ExeFullPath), true) == 0)
				{
					self = file;
					UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "InstallUpdate() セルフスキップ");
				}
				else
				{
					InstallMove(file, extractBasePath);
					count++;
				}
				_mainWindowViewModel.SetProgress((Double)count / extractFiles.Length);
			}

			// 自分自身が上書きされる場合は Old に退避
			if (!String.IsNullOrEmpty(self))
			{
				UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "InstallUpdate() 自分自身を退避");
				try
				{
					File.Move(UpdaterModel.Instance.EnvModel.ExeFullPath, OldPath() + Path.GetFileName(UpdaterModel.Instance.EnvModel.ExeFullPath));
				}
				catch
				{
					throw new Exception("現行ファイルの退避ができませんでした。");
				}
				InstallMove(self, extractBasePath);
			}
		}

		// --------------------------------------------------------------------
		// ダウンロード完了している自動更新用アーカイブの MD5 は有効か
		// --------------------------------------------------------------------
		private Boolean IsUpdateArchiveMD5Valid()
		{
			// MD5 ハッシュ値の取得
			using FileStream fileStream = new FileStream(UpdateArchivePath(), FileMode.Open, FileAccess.Read, FileShare.Read);
			using MD5CryptoServiceProvider md5Provider = new();
			Byte[] hashBytes = md5Provider.ComputeHash(fileStream);

			// ハッシュ値を文字列に変換
			String hashStr = BitConverter.ToString(hashBytes).Replace("-", String.Empty);

			// 確認
			if (String.Compare(_autoUpdateSettings.DownloadMD5, hashStr, true) != 0)
			{
				UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "ダウンロード情報の MD5 と実際の MD5 が異なります。ダウンロード情報："
						+ _autoUpdateSettings.DownloadMD5 + ", 実際：" + hashStr);
				return false;
			}
			return true;
		}

		// --------------------------------------------------------------------
		// 更新版が既にダウンロード完了しているか
		// --------------------------------------------------------------------
		private Boolean IsUpdateDownloaded()
		{
			// アーカイブがあるか
			UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "IsUpdateDownloaded() path: " + UpdateArchivePath());
			if (!File.Exists(UpdateArchivePath()))
			{
				UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "更新版は未ダウンロードです（ファイルが存在しません）。");
				return false;
			}

			// アーカイブは有効か
			if (_autoUpdateSettings.DownloadVer != _updateItems[0].Elements[RssManager.NODE_NAME_TITLE])
			{
				UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "更新版は未ダウンロードです（ダウンロード情報のバージョンと求めるバージョンが異なります）。");
				return false;
			}
			if (_autoUpdateSettings.DownloadMD5 != _updateItems[0].Elements[RssManager.NODE_NAME_LINK + RssItem.RSS_ITEM_NAME_DELIMITER + RssManager.ATTRIBUTE_NAME_MD5])
			{
				UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "更新版は未ダウンロードです（ダウンロード情報の MD5 が異なります）。");
				return false;
			}
			if (!IsUpdateArchiveMD5Valid())
			{
				return false;
			}

			UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "更新版をすでにダウンロード済です。");
			return true;
		}

		// --------------------------------------------------------------------
		// 現行ファイルのバックアップ先パス
		// --------------------------------------------------------------------
		private String OldPath()
		{
			return UpdaterModel.Instance.EnvModel.ExeFullFolder + FOLDER_NAME_UPDATE + FOLDER_NAME_OLD;
		}

		// --------------------------------------------------------------------
		// 最新情報の確認と表示準備
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private async Task PrepareLatestAsync()
		{
			UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, _displayName + "の最新情報を確認中...");

			// RSS チェック
			RssManager rssManager = CreateRssManager(CONFIG_FILE_NAME_LATEST_SUFFIX);
			UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "PrepareLatest() location: " + _params.LatestRss);
			(Boolean result, String? errorMessage) = await rssManager.ReadLatestRssAsync(_params.LatestRss);
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
		// 更新版の確認とインストール準備
		// ＜例外＞ Exception
		// --------------------------------------------------------------------
		private async Task PrepareUpdateAsync()
		{
			UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, _displayName + "の更新版を確認中...");
			Common.InitializeTempFolder();

			// 更新制御情報
			_autoUpdateSettings = new(UpdaterModel.Instance.EnvModel.LogWriter,
					Common.UserAppDataFolderPath() + CONFIG_FILE_NAME_PREFIX + _params.ID + CONFIG_FILE_NAME_UPDATE_SUFFIX + Common.FILE_EXT_CONFIG);
			if (_params.ClearUpdateCache)
			{
				UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "更新制御情報をクリアします。");

				// 空の情報で保存
				_autoUpdateSettings.Save();

				// ダウンロード済みファイルも削除
				try
				{
					File.Delete(UpdateArchivePath());
				}
				catch
				{
				}
			}
			else
			{
				_autoUpdateSettings.Load();
			}

			// RSS チェック
			RssManager rssManager = CreateRssManager(CONFIG_FILE_NAME_DUMMY_SUFFIX);
			(Boolean result, String? errorMessage) = await rssManager.ReadLatestRssAsync(_params.UpdateRss);
			if (!result)
			{
				throw new Exception(errorMessage);
			}

			// 全件取得
			_updateItems = rssManager.GetAllItems();

			// 分析
			if (_updateItems.Count == 0)
			{
				throw new Exception("自動更新用 RSS に情報がありません。");
			}
			AnalyzeUpdateRss();
			UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "新しいバージョン「" + _updateItems[0].Elements[RssManager.NODE_NAME_TITLE] + "」が見つかりました。");

			// ダウンロード
			if (!IsUpdateDownloaded())
			{
				await DownloadUpdateArchiveAsync();
			}
		}

		// --------------------------------------------------------------------
		// 一連の更新確認を実施
		// --------------------------------------------------------------------
		private async Task RunCoreAsync()
		{
			try
			{
				Boolean isLatestMessageShown = false;
				Boolean isUpdateMessageShown = false;
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
					(isLatestMessageShown, latestErrorMessage) = await CheckLatestInfoAsync();
				}

				// 自動更新
				if (_params.IsUpdateMode())
				{
					(isUpdateMessageShown, updateErrorMessage) = await CheckUpdateAsync();
				}

				if (!isLatestMessageShown && !isUpdateMessageShown)
				{
					// どちらもメッセージを表示していない場合のみメッセージを表示する
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
		// インストールを開始する旨のメッセージを表示
		// --------------------------------------------------------------------
		private void ShowInstallMessage()
		{
			UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Information, _displayName + "の更新版をインストールします。\n"
					+ _displayName + "が起動している場合は終了してから、OK ボタンをクリックして下さい。");
		}

		// --------------------------------------------------------------------
		// ダウンロードした自動更新用アーカイブを保存するパス
		// --------------------------------------------------------------------
		private String UpdateArchivePath()
		{
			return UpdaterModel.Instance.EnvModel.ExeFullFolder + FOLDER_NAME_UPDATE + FOLDER_NAME_NEW_ARCHIVE + FILE_NAME_DOWNLOAD_ZIP;
		}

		// --------------------------------------------------------------------
		// 更新版のアーカイブをダウンロードしたフルパス
		// --------------------------------------------------------------------
		private void WaitTargetExit()
		{

			if (_params.PID == 0)
			{
				// 更新対象アプリが起動していることを知らされていない
				UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Verbose, "WaitTargetExit() PID が 0 なので待機しない");
				return;
			}
			_mainWindowViewModel.ShowWaitingMessage();

			// プロセスの終了を待機
			Process targetProcess;
			try
			{
				targetProcess = Process.GetProcessById(_params.PID);
			}
			catch
			{
				UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "終了検知ができません。終了を待たずに続行します。");
				return;
			}
			targetProcess.WaitForExit();
			targetProcess.Dispose();
			UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "終了を検知しました。続行します。");
		}
	}
}
