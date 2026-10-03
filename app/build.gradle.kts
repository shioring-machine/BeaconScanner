// Androidアプリ用プラグインを適用するためのブロックである。
plugins {
    // バージョンカタログに登録されたAndroid Applicationプラグインを適用する。
    alias(libs.plugins.android.application)
}

// Androidアプリのビルド条件を設定するブロックである。
android {
    // 生成されるRクラスやBuildConfigなどが所属する名前空間を指定する。
    namespace = "com.example.beaconscanner"

    // コンパイル時に使用するAndroid SDKを指定する。
    compileSdk {
        // Android Studioが生成したAPI 37向けの記法をそのまま使用する。
        version = release(37)
    }

    // アプリの基本設定をまとめる。
    defaultConfig {
        // Android端末上でアプリを一意に識別するアプリケーションIDを指定する。
        applicationId = "com.example.beaconscanner"
        // アプリをインストールできる最小Android APIレベルを指定する。
        minSdk = 26
        // アプリが動作確認の対象とするAndroid APIレベルを指定する。
        targetSdk = 37
        // アプリ更新判定に使用する整数のバージョン番号を指定する。
        versionCode = 1
        // 利用者に表示するバージョン名を指定する。
        versionName = "1.0"

        // Android実機テストで使用するテストランナーを指定する。
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    // リリース用など、ビルド種別ごとの設定を定義する。
    buildTypes {
        // リリース版の設定を定義する。
        release {
            // コード最適化に関する設定をまとめる。
            optimization {
                // 現段階では最適化を無効にし、デバッグしやすい状態を保つ。
                enable = false
            }
        }
    }

    // Javaソースと生成バイトコードの互換バージョンを設定する。
    compileOptions {
        // Java 11の文法を使用する。
        sourceCompatibility = JavaVersion.VERSION_11
        // Java 11互換のバイトコードを生成する。
        targetCompatibility = JavaVersion.VERSION_11
    }

    // Android Gradle Pluginが提供する追加ビルド機能を設定する。
    buildFeatures {
        // activity_main.xmlからActivityMainBindingを自動生成する。
        viewBinding = true
    }
}

// アプリが利用するライブラリを宣言する。
dependencies {
    // Activity Result APIなど、AndroidX ActivityのKotlin拡張を利用する。
    implementation(libs.androidx.activity.ktx)
    // AppCompatActivityなど、AndroidX AppCompatを利用する。
    implementation(libs.androidx.appcompat)
    // 既存テンプレートとの互換性のためConstraintLayoutを残す。
    implementation(libs.androidx.constraintlayout)
    // ContextCompatなど、AndroidX CoreのKotlin拡張を利用する。
    implementation(libs.androidx.core.ktx)
    // Material Design関連の標準部品とテーマを利用する。
    implementation(libs.material)

    // ローカル単体テストでJUnitを利用する。
    testImplementation(libs.junit)
    // Android実機UIテストでEspressoを利用する。
    androidTestImplementation(libs.androidx.espresso.core)
    // Android実機テストでAndroidX JUnitを利用する。
    androidTestImplementation(libs.androidx.junit)
}
