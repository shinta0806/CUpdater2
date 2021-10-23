// ============================================================================
// 
// メインウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Messaging;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

using Shinta;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Updater.Models;
using Updater.Models.SharedMisc;
using Updater.Models.UpdaterModels;
using Updater.ViewModels.MiscWindowViewModels;

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

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 不透明度
		private Double _opacity = 0.75;
		public Double Opacity
		{
			get => _opacity;
			set => RaisePropertyChangedIfSet(ref _opacity, value);
		}

		// メインキャプション
		private String _caption = String.Empty;
		public String Caption
		{
			get => _caption;
			set => RaisePropertyChangedIfSet(ref _caption, value);
		}

		// サブキャプション
		private String _subCaption = String.Empty;
		public String SubCaption
		{
			get => _subCaption;
			set => RaisePropertyChangedIfSet(ref _subCaption, value);
		}

		// 進捗度（最大値：1）
		private Double _progress;
		public Double Progress
		{
			get => _progress;
			set => RaisePropertyChangedIfSet(ref _progress, value);
		}

		// ログ
		public ObservableCollection<String> Logs { get; set; } = new();

		// ログ選択行番号
		private Int32 _selectedLogIndex;
		public Int32 SelectedLogIndex
		{
			get => _selectedLogIndex;
			set => RaisePropertyChangedIfSet(ref _selectedLogIndex, value);
		}

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public override async void Initialize()
		{
			base.Initialize();

			Boolean showErrMsg = _params.ForceShow;
			try
			{
				// タイトルバー（ユーザーには見えないが識別に使われるかもしれないので一応設定しておく）
				Title = UpdConstants.APP_NAME_J;
#if DEBUG
				Title = "［デバッグ］" + Title;
#endif

				// 外観
				SetAppearance();

				// ログ表示
				UpdaterModel.Instance.EnvModel.LogWriter.AppendDisplayText = AppendDisplayText;

				// コマンドライン引数
				AnalyzeParams();

				// 呼びだし元アプリが背面に行くのを防止できるように配慮
				if (_params.NotifyHWnd != IntPtr.Zero)
				{
					WindowsApi.PostMessage(_params.NotifyHWnd, UpdaterLauncher.WM_UPDATER_LAUNCHED, IntPtr.Zero, IntPtr.Zero);
				}

				if (!_params.IsRequiredValid())
				{
					// ユーザーが間違って起動した可能性が高いので、ユーザーにメッセージを表示する
					showErrMsg = true;
					throw new Exception("動作に必要なパラメーターが設定されていません。");
				}

				// オンリー系の動作（動作後に終了）
				if (_params.DeleteOld)
				{
					// 未実装
					throw new Exception("パラメーターが不正です。");
				}
				else if (_params.Verbose)
				{
					UpdCommon.NotifyDisplayedIfNeeded(_params);

					// ViewModel 経由でウィンドウを開く
					using AboutWindowViewModel aboutWindowViewModel = new();
					Messenger.Raise(new TransitionMessage(aboutWindowViewModel, UpdConstants.MESSAGE_KEY_OPEN_ABOUT_WINDOW));

					throw new OperationCanceledException();
				}

#if false
				// セルフ再起動
				// .NET Core アプリから起動された場合、自身が呼びだし元のファイルをロックしている状態になっていることがある
				// セルフ再起動することにより、呼び出し元と自身のアプリの関連性が切れ、ロックが解除されるようだ
				if (!_params.SelfLaunch)
				{
					// 何らかのバグにより再起動を繰り返す事態になった場合に利用者がプロセスを殺す余地ができるように少し待機
					Thread.Sleep(1000);

					_params.SelfLaunch = true;
					_params.Launch(_params.ForceShow);

					showErrMsg = false;
					throw new OperationCanceledException("セルフ再起動したため終了します。");
				}
#endif

				// 同じパスでの多重起動防止
				// セルフ再起動しないことが確定してから確認する必要があるため App.xaml.cs では実施せずにここで実施する
				_mutex = CommonWindows.ActivateAnotherProcessWindowIfNeeded(Common.SHINTA + '_' + UpdConstants.APP_ID + '_' + UpdaterModel.Instance.EnvModel.ExeFullPath.Replace('\\', '/'));
				if (_mutex == null)
				{
					throw new MultiInstanceException();
				}

				// オンリー系ではないので先に進む
				UpdateSequence updateSequence = new(this, _params);
				await updateSequence.RunAsync();
				Debug.WriteLine("Initialize() done");
			}
			catch (Exception excep)
			{
				// 条件が独自のため ShowLogMessageAndNotify() は使わない
				if (showErrMsg && !String.IsNullOrEmpty(excep.Message))
				{
					UpdCommon.NotifyDisplayedIfNeeded(_params);
					UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, excep.Message);
				}
				else
				{
					UpdaterModel.Instance.EnvModel.LogWriter.LogMessage(TraceEventType.Error, "メインウィンドウ初期化時エラー：\n" + excep.Message);
					UpdaterModel.Instance.EnvModel.LogWriter.LogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
				}
			}

			// ウィンドウを閉じる
			CloseWindow();
		}

		// --------------------------------------------------------------------
		// 進捗度の設定
		// --------------------------------------------------------------------
		public void SetProgress(Double progress)
		{
			Progress = progress;
		}

		// --------------------------------------------------------------------
		// サブキャプションの設定
		// --------------------------------------------------------------------
		public void SetSubCaption(String text)
		{
			SubCaption = text;
		}

		// --------------------------------------------------------------------
		// インストール中
		// --------------------------------------------------------------------
		public void ShowInstallingMessage()
		{
			SetCaption("インストール中...");
			SetSubCaption(String.Empty);
		}

		// --------------------------------------------------------------------
		// 更新対象アプリ終了待ち
		// --------------------------------------------------------------------
		public void ShowWaitingMessage()
		{
			SetCaption("アプリケーションの終了を待っています...");
			SetSubCaption("アプリケーションを終了させて下さい。終了後、自動的に更新が開始されます。");
			//TextBoxLog.AppendText("［" + mLogWriter.TraceEventTypeToCaption(TraceEventType.Information) + "］アプリケーションの終了を待っています...\r\n");
		}

		// --------------------------------------------------------------------
		// ウィンドウを可視化する
		// --------------------------------------------------------------------
		public void ShowWindow()
		{
			Opacity = 1.0;
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (_isDisposed)
			{
				return;
			}

			try
			{
				// アプリケーションの終了を通知
				UpdaterModel.Instance.EnvModel.AppCancellationTokenSource.Cancel();

				// 終了処理
				//SaveExitStatus();
				try
				{
					//Directory.Delete(YlCommon.TempFolderPath(), true);
				}
				catch
				{
				}

				UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS,
						"終了しました：" + UpdConstants.APP_NAME_J + " " + UpdConstants.APP_VER + " --------------------");

				_isDisposed = true;
			}
			catch (Exception excep)
			{
				UpdCommon.ShowLogMessageAndNotify(_params, TraceEventType.Error, "メインウィンドウ破棄時エラー：\n" + excep.Message);
				UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 本来 UpdaterLauncher は起動用だが、ここでは引数管理用として使用
		private UpdaterLauncher _params = new UpdaterLauncher();

		// 多重起動防止用
		// アプリケーション終了までガベージコレクションされないようにメンバー変数で持つ
		private Mutex? _mutex;

		// Dispose フラグ
		private Boolean _isDisposed;

		// ====================================================================
		// private メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// コマンドライン引数の解析
		// --------------------------------------------------------------------
		private void AnalyzeParams()
		{
			String[] cmdParams = Environment.GetCommandLineArgs();
			String cmdParam;
			String opt;
			Int32 optInt32;

			for (Int32 i = 1; i < cmdParams.Length; i++)
			{
				// 本当は、パラメーターによってはインデックスをさらに 1 つ進める必要があるが、面倒くさいので進めない
				cmdParam = cmdParams[i];
				if (i == cmdParams.Length - 1)
				{
					opt = String.Empty;
				}
				else
				{
					opt = cmdParams[i + 1];
				}

				// 共通オプション
				if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_ID, true) == 0)
				{
					_params.ID = opt;
				}
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_NAME, true) == 0)
				{
					_params.Name = opt;
				}
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_WAIT, true) == 0)
				{
					if (Int32.TryParse(opt, out optInt32))
					{
						_params.Wait = optInt32;
					}
				}
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_FORCE_SHOW, true) == 0)
				{
					_params.ForceShow = true;
				}
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_NOTIFY_HWND, true) == 0)
				{
					if (Int32.TryParse(opt, out optInt32))
					{
						_params.NotifyHWnd = (IntPtr)optInt32;
					}
				}
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_SELF_LAUNCH, true) == 0)
				{
					_params.SelfLaunch = true;
				}

				// 共通オプション（オンリー系）
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_VERBOSE, true) == 0)
				{
					_params.Verbose = true;
				}
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_DELETE_OLD, true) == 0)
				{
					_params.DeleteOld = true;
				}

				// 最新情報確認用オプション
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_LATEST_RSS, true) == 0)
				{
					_params.LatestRss = opt;
				}

				// 更新（自動アップデート）用オプション
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_UPDATE_RSS, true) == 0)
				{
					_params.UpdateRss = opt;
				}
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_CURRENT_VER, true) == 0)
				{
					_params.CurrentVer = opt;
				}
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_PID, true) == 0)
				{
					if (Int32.TryParse(opt, out optInt32))
					{
						_params.PID = optInt32;
					}
				}
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_RELAUNCH, true) == 0)
				{
					_params.Relaunch = opt;
				}
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_CLEAR_UPDATE_CACHE, true) == 0)
				{
					_params.ClearUpdateCache = true;
				}
				else if (String.Compare(cmdParam, UpdaterLauncher.PARAM_STR_FORCE_INSTALL, true) == 0)
				{
					_params.ForceInstall = true;
				}
			}

			UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, "対象 ID：" + _params.ID);
		}

		// --------------------------------------------------------------------
		// ログ文字列に追加
		// --------------------------------------------------------------------
		private void AppendDisplayText(String text)
		{
			Logs.Add(text);
			SelectedLogIndex = Logs.Count - 1;
		}

		// --------------------------------------------------------------------
		// マテリアルデザインの外観を設定
		// --------------------------------------------------------------------
		private void SetAppearance()
		{
			IEnumerable<Swatch> swatches = new SwatchesProvider().Swatches;
			PaletteHelper paletteHelper = new();
			ITheme theme = paletteHelper.GetTheme();
			Swatch? orangeSwatch = swatches.FirstOrDefault(x => x.Name == "orange");
			if (orangeSwatch != null)
			{
				theme.SetPrimaryColor(orangeSwatch.ExemplarHue.Color);
			}
			Swatch? limeSwatch = swatches.FirstOrDefault(x => x.Name == "yellow");
			if (limeSwatch != null)
			{
				theme.SetSecondaryColor(limeSwatch.ExemplarHue.Color);
			}
			paletteHelper.SetTheme(theme);
		}

		// --------------------------------------------------------------------
		// メインキャプションとログ
		// --------------------------------------------------------------------
		private void SetCaption(String text)
		{
			Caption = text;
			UpdCommon.ShowLogMessageAndNotify(_params, Common.TRACE_EVENT_TYPE_STATUS, text);
		}
	}
}
