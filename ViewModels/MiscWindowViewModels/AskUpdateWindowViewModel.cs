// ============================================================================
// 
// 更新適用可否確認ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet.Commands;
using Livet.Messaging.Windows;

using Shinta;

using System;
using System.Diagnostics;
using System.Windows;

using Updater.Models.SharedMisc;
using Updater.Models.UpdaterModels;

namespace Updater.ViewModels.MiscWindowViewModels
{
	public class AskUpdateWindowViewModel : UpdViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// プログラマーが使うべき引数付きコンストラクター
		// --------------------------------------------------------------------
		public AskUpdateWindowViewModel(UpdaterLauncher updaterParams, String displayName, String newVer)
		{
			_params = updaterParams;
			_newVer = newVer;

			Title = displayName + "の自動更新";
			AskMessage = displayName + "の更新版が公開されています。インストールしますか？\n"
					+ "現在のバージョン：" + _params.CurrentVer + "\n"
					+ "新しいバージョン：" + _newVer;
		}

		// --------------------------------------------------------------------
		// ダミーコンストラクター
		// --------------------------------------------------------------------
		public AskUpdateWindowViewModel()
		{
			_params = null!;
			_newVer = String.Empty;
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// 確認メッセージ
		private String _askMessage = String.Empty;
		public String AskMessage
		{
			get => _askMessage;
			set => RaisePropertyChangedIfSet(ref _askMessage, value);
		}

		// --------------------------------------------------------------------
		// 一般プロパティー
		// --------------------------------------------------------------------

		// --------------------------------------------------------------------
		// コマンド
		// --------------------------------------------------------------------

		#region はいボタンの制御
		private ViewModelCommand? _buttonYesClickedCommand;

		public ViewModelCommand ButtonYesClickedCommand
		{
			get
			{
				if (_buttonYesClickedCommand == null)
				{
					_buttonYesClickedCommand = new ViewModelCommand(ButtonYesClicked);
				}
				return _buttonYesClickedCommand;
			}
		}

		public void ButtonYesClicked()
		{
			try
			{
				ViewModelResult = MessageBoxResult.Yes;
				Messenger.Raise(new WindowActionMessage(UpdConstants.MESSAGE_KEY_WINDOW_CLOSE));
			}
			catch (Exception excep)
			{
				UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "はいボタンクリック時エラー：\n" + excep.Message);
				UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		#region いいえボタンの制御
		private ViewModelCommand? _buttonNoClickedCommand;

		public ViewModelCommand ButtonNoClickedCommand
		{
			get
			{
				if (_buttonNoClickedCommand == null)
				{
					_buttonNoClickedCommand = new ViewModelCommand(ButtonNoClicked);
				}
				return _buttonNoClickedCommand;
			}
		}

		public void ButtonNoClicked()
		{
			try
			{
				if (MessageBox.Show(_newVer + " が自動インストールされなくなりますが、よろしいですか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Warning)
						!= MessageBoxResult.Yes)
				{
					return;
				}

				ViewModelResult = MessageBoxResult.No;
				Messenger.Raise(new WindowActionMessage(UpdConstants.MESSAGE_KEY_WINDOW_CLOSE));
			}
			catch (Exception excep)
			{
				UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "いいえボタンクリック時エラー：\n" + excep.Message);
				UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}
		#endregion

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();

			try
			{

			}
			catch (Exception excep)
			{
				UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(TraceEventType.Error, "更新適用可否確認ウィンドウ初期化時エラー：\n" + excep.Message);
				UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, "　スタックトレース：\n" + excep.StackTrace);
			}
		}

		// ====================================================================
		// private メンバー変数
		// ====================================================================

		// 本来 UpdaterLauncher は起動用だが、ここでは引数管理用として使用
		private UpdaterLauncher _params;

		// 見つかった更新版のバージョン
		private String _newVer;
	}
}
