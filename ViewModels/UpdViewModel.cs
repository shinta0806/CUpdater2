// ============================================================================
// 
// ちょちょいと自動更新 2 の基底用 ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// すべてのウィンドウの ViewModel に適用する
// ----------------------------------------------------------------------------

using Livet;
using Livet.Messaging.Windows;

using Shinta;

using System;
using System.Windows;

using Updater.Models.SharedMisc;
using Updater.Models.UpdaterModels;

namespace Updater.ViewModels
{
	public class UpdViewModel : ViewModel
	{
		// ====================================================================
		// コンストラクター・デストラクター
		// ====================================================================

		// --------------------------------------------------------------------
		// コンストラクター
		// --------------------------------------------------------------------
		public UpdViewModel()
		{
			UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " 生成中...");
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// View 通信用のプロパティー
		// --------------------------------------------------------------------

		// ウィンドウタイトル（デフォルトが null だと実行時にエラーが発生するので Empty にしておく）
		private String _title = String.Empty;
		public String Title
		{
			get => _title;
			set => RaisePropertyChangedIfSet(ref _title, value);
		}

		// --------------------------------------------------------------------
		// 一般のプロパティー
		// --------------------------------------------------------------------

		// OK、YES、No 等の結果
		public MessageBoxResult ViewModelResult { get; protected set; }

		// ====================================================================
		// public メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------
		public virtual void Initialize()
		{
			UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " 初期化中...");
		}

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// ウィンドウを閉じる
		// --------------------------------------------------------------------
		protected void CloseWindow()
		{
			Messenger.Raise(new WindowActionMessage(UpdConstants.MESSAGE_KEY_WINDOW_CLOSE));
		}

		// --------------------------------------------------------------------
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(Boolean isDisposing)
		{
			base.Dispose(isDisposing);

			UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " 破棄中...");
		}
	}
}
