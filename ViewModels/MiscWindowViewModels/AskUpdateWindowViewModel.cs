// ============================================================================
// 
// 更新適用可否確認ウィンドウの ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Livet;
using Livet.Commands;
using Livet.EventListeners;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.Messaging.Windows;
using Shinta;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using Updater.Models;
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
		// コンストラクター
		// --------------------------------------------------------------------
		public AskUpdateWindowViewModel()
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

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
	}
}
