﻿// ============================================================================
// 
// ちょちょいと自動更新 2 の設定を管理
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 
// ----------------------------------------------------------------------------

using Shinta;

using System;
using System.Windows;

namespace Updater.Models.SerializableSettings
{
	public class UpdSettings : SerializableSettings
	{
		// ====================================================================
		// public プロパティー
		// ====================================================================

		// --------------------------------------------------------------------
		// 終了時の状態（一般）
		// --------------------------------------------------------------------

		// 前回起動時のバージョン
		public String PrevLaunchVer { get; set; } = String.Empty;

		// 前回起動時のパス
		public String PrevLaunchPath { get; set; } = String.Empty;

		// ウィンドウ位置
		public Rect DesktopBounds { get; set; }

		// RSS 確認日
		public DateTime RssCheckDate { get; set; }

		// ====================================================================
		// protected メンバー関数
		// ====================================================================

		// --------------------------------------------------------------------
		// 保存パス
		// --------------------------------------------------------------------
		protected override String SettingsPath()
		{
			return Common.UserAppDataFolderPath() + nameof(UpdSettings) + Common.FILE_EXT_CONFIG;
		}
	}
}