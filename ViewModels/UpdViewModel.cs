﻿// ============================================================================
// 
// ちょちょいと自動更新 2 の基底用 ViewModel
// 
// ============================================================================

// ----------------------------------------------------------------------------
// すべてのウィンドウの ViewModel に適用する
// ----------------------------------------------------------------------------

using Livet;

using Shinta;

using System;

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

		// OK ボタン・削除ボタン等、キャンセル以外のボタンが押されて閉じられた
		public Boolean IsOk { get; protected set; }

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
		// リソース解放
		// --------------------------------------------------------------------
		protected override void Dispose(Boolean isDisposing)
		{
			base.Dispose(isDisposing);

			UpdaterModel.Instance.EnvModel.LogWriter.ShowLogMessage(Common.TRACE_EVENT_TYPE_STATUS, GetType().Name + " 破棄中...");
		}
	}
}