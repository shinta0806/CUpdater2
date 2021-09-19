﻿// ============================================================================
// 
// アプリケーションごとの自動更新の設定を管理
// 
// ============================================================================

// ----------------------------------------------------------------------------
// 設定が変更される度にすみやかに保存されるべき
// ----------------------------------------------------------------------------

using Shinta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.Models.Settings
{
	public class AutoUpdateSettings : SerializableSettings
	{
		// --------------------------------------------------------------------
		// コンストラクター（引数あり）
		// --------------------------------------------------------------------
		public AutoUpdateSettings(LogWriter? logWriter, String? settingsPath)
				: base(logWriter, settingsPath)
		{
		}

		// --------------------------------------------------------------------
		// コンストラクター（引数なし：シリアライズに必要）
		// --------------------------------------------------------------------
		public AutoUpdateSettings()
				: base(null, null)
		{
		}

		// ====================================================================
		// public プロパティー
		// ====================================================================

		// ダウンロード・インストールしないバージョン（既にやったか、またはユーザーに不要と言われた）
		public String SkipVer { get; set; } = String.Empty;

		// ダウンロードを試行している、もしくは完了したバージョン
		public String? DownloadVer { get; set; }

		// ダウンロードを試行した回数
		public Int32 DownloadTry { get; set; }

		// ダウンロードしたファイルのあるべき MD5
		public String? DownloadMD5 { get; set; }
	}
}