// Androidアプリ用プラグインを適用するためのブロックである。
plugins {
    // バージョンカタログに登録されたAndroid Applicationプラグインを適用する。
    alias(libs.plugins.android.application) apply false
}