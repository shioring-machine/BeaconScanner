// このファイルが属するKotlinパッケージを宣言する。
package com.example.beaconscanner

// Androidの権限定数を利用する。
import android.Manifest
// 権限を確認済みの箇所でLint警告を抑制する。
import android.annotation.SuppressLint
// Bluetoothアダプターの状態確認と有効化要求に利用する。
import android.bluetooth.BluetoothAdapter
// BluetoothアダプターをAndroidシステムから取得する。
import android.bluetooth.BluetoothManager
// BLEスキャン結果を受け取るコールバックを利用する。
import android.bluetooth.le.ScanCallback
// 1件のBLEスキャン結果を表す。
import android.bluetooth.le.ScanResult
// AndroidのシステムサービスとSharedPreferencesを取得する。
import android.content.Context
// Bluetoothを有効にするための画面を起動するIntentを利用する。
import android.content.Intent
// 端末機能と権限許可状態の確認に利用する。
import android.content.pm.PackageManager
// 機能状態表示の文字色と背景色を設定する。
import android.graphics.Color
// 重力センサーや加速度センサーの種類を表す。
import android.hardware.Sensor
// 1回分のセンサー測定値を表す。
import android.hardware.SensorEvent
// センサー測定値と精度変化を受け取る。
import android.hardware.SensorEventListener
// 端末のセンサーを取得し、監視を登録する。
import android.hardware.SensorManager
// Androidバージョンに応じて権限を切り替える。
import android.os.Build
// Activity生成時の保存状態を受け取る。
import android.os.Bundle
// メインスレッド上で定期処理を実行する。
import android.os.Handler
// メインスレッド用Looperを取得する。
import android.os.Looper
// システム時刻変更の影響を受けない経過時間を取得する。
import android.os.SystemClock
// 診断情報をLogcatへ出力する。
import android.util.Log
// 監視中の画面消灯を防ぐウィンドウフラグを利用する。
import android.view.WindowManager
// 実行時権限やBluetooth有効化画面の結果を受け取る。
import androidx.activity.result.contract.ActivityResultContracts
// AndroidX互換のActivity基底クラスを利用する。
import androidx.appcompat.app.AppCompatActivity
// Androidバージョンに依存せず権限状態を確認する。
import androidx.core.content.ContextCompat
// activity_main.xmlから自動生成されたView Bindingクラスを利用する。
import com.example.beaconscanner.databinding.ActivityMainBinding
// 発動開始日時を文字列へ整形する。
import java.text.SimpleDateFormat
// ミリ秒の時刻を日時として扱う。
import java.util.Date
// UUIDの大文字化や日本語形式の文字列整形に利用する。
import java.util.Locale

// メイン画面を表し、同時にセンサーイベントを受け取るクラスである。
class MainActivity : AppCompatActivity(), SensorEventListener {

    // クラス全体で共有する変更不能な定数をまとめる。
    companion object {
        // Logcatでこのアプリのログを抽出するためのタグである。
        private const val TAG = "BeaconScanner"
        // 対象iBeaconのUUIDである。
        private const val TARGET_UUID = "FDA50693-A4E2-4FB1-AFCF-C6EB07647825"
        // 対象iBeaconのMajor値である。
        private const val TARGET_MAJOR = 1
        // 対象iBeaconのMinor値である。
        private const val TARGET_MINOR = 1
        // R1 Beacon本体の設定用広告を診断表示するためのMACアドレスである。
        private const val DIAGNOSTIC_TARGET_MAC = "D5:4B:3B:1F:22:50"
        // RSSI中央値を求めるために保持する直近測定値の個数である。
        private const val RSSI_WINDOW_SIZE = 5
        // RSSI中央値がこの値以上なら接近候補とする。
        private const val NEAR_RSSI_THRESHOLD = -65
        // 接近中にRSSI中央値がこの値未満になれば離脱とする。
        private const val EXIT_RSSI_THRESHOLD = -78
        // 接近候補がこの時間継続したら接近状態へ移る。
        private const val NEAR_CONFIRM_DURATION_MILLIS = 2_000L
        // 対象iBeaconをこの時間受信しなければ離脱とする。
        private const val BEACON_TIMEOUT_MILLIS = 8_000L
        // Z値がこの値未満なら伏せ候補とする。
        private const val FACE_DOWN_ENTER_Z = -8.0f
        // 伏せ中にZ値がこの値より大きくなれば伏せ解除候補とする。
        private const val FACE_DOWN_EXIT_Z = -6.0f
        // 伏せ候補がこの時間継続したら伏せ状態へ移る。
        private const val FACE_DOWN_CONFIRM_DURATION_MILLIS = 1_500L
        // 伏せ解除候補がこの時間継続したら伏せ状態を解除する。
        private const val FACE_UP_CONFIRM_DURATION_MILLIS = 700L
        // 状態判定と画面更新を実行する間隔である。
        private const val STATE_CHECK_INTERVAL_MILLIS = 100L
        // 発動履歴を保存するSharedPreferencesのファイル名である。
        private const val PREFERENCES_NAME = "beacon_activation_preferences"
        // 直近の発動開始日時を保存するキーである。
        private const val PREF_LAST_ACTIVATION_START_WALL_TIME =
            "last_activation_start_wall_time"
        // 直近の発動継続時間を保存するキーである。
        private const val PREF_LAST_ACTIVATION_DURATION =
            "last_activation_duration"
    }

    // activity_main.xml内の各画面部品へ型安全にアクセスするための変数である。
    private lateinit var binding: ActivityMainBinding
    // 定期判定をメインスレッドで実行するHandlerである。
    private val mainHandler = Handler(Looper.getMainLooper())

    // BluetoothManagerを初回参照時にAndroidシステムから取得する。
    private val bluetoothManager: BluetoothManager by lazy {
        getSystemService(Context.BLUETOOTH_SERVICE) as BluetoothManager
    }
    // BluetoothManagerから現在のBluetoothAdapterを取得する。
    private val bluetoothAdapter: BluetoothAdapter?
        get() = bluetoothManager.adapter
    // SensorManagerを初回参照時にAndroidシステムから取得する。
    private val sensorManager: SensorManager by lazy {
        getSystemService(Context.SENSOR_SERVICE) as SensorManager
    }
    // 重力センサーを優先し、存在しなければ加速度センサーを使用する。
    private val gravitySensor: Sensor? by lazy {
        sensorManager.getDefaultSensor(Sensor.TYPE_GRAVITY)
            ?: sensorManager.getDefaultSensor(Sensor.TYPE_ACCELEROMETER)
    }
    // 発動履歴を端末内へ保存するSharedPreferencesを取得する。
    private val preferences by lazy {
        getSharedPreferences(PREFERENCES_NAME, Context.MODE_PRIVATE)
    }

    // BLEと姿勢の監視中かを保持する。
    private var isMonitoring = false
    // 実際にTYPE_GRAVITYを使用しているかを保持する。
    private var isUsingGravitySensor = false
    // 対象iBeaconが接近中かを保持する。
    private var isNearTargetBeacon = false
    // 端末が伏せ状態かを保持する。
    private var isFaceDown = false
    // 「接近中かつ伏せ状態」の機能が発動中かを保持する。
    private var isFeatureActive = false
    // 現在のZ軸方向の重力推定値を保持する。
    private var currentGravityZ = Float.NaN
    // 接近候補が始まった経過時刻を保持する。
    private var nearCandidateStartElapsed: Long? = null
    // 伏せ候補が始まった経過時刻を保持する。
    private var faceDownCandidateStartElapsed: Long? = null
    // 伏せ解除候補が始まった経過時刻を保持する。
    private var faceUpCandidateStartElapsed: Long? = null
    // 対象iBeaconを最後に受信した経過時刻を保持する。
    private var lastTargetBeaconSeenElapsed: Long? = null
    // 現在の発動が始まった経過時刻を保持する。
    private var featureActivationStartElapsed: Long? = null
    // 現在の発動が始まった実日時を保持する。
    private var featureActivationStartWallTime: Long? = null
    // R1 Beacon設定用広告を最後に受信した経過時刻を保持する。
    private var lastDiagnosticMacSeenElapsed: Long? = null
    // R1 Beacon設定用広告の最新RSSIを保持する。
    private var lastDiagnosticMacRssi: Int? = null
    // R1 Beacon設定用広告の最新生データを保持する。
    private var lastDiagnosticMacRawData: String? = null
    // 対象iBeaconの直近RSSI値を到着順に保持する。
    private val recentRssiValues = ArrayDeque<Int>()

    // Androidの実行時権限要求と、その結果に応じた処理を登録する。
    private val permissionLauncher =
        registerForActivityResult(ActivityResultContracts.RequestMultiplePermissions()) {
            // 権限要求後にすべての必要権限が許可されたかを再確認する。
            if (hasRequiredPermissions()) {
                // Bluetoothの状態を確認してから監視開始へ進む。
                checkBluetoothAndStartMonitoring()
            } else {
                // 未許可権限があることを画面へ表示する。
                binding.statusTextView.text = "必要な権限が許可されていません。"
            }
        }

    // Bluetooth有効化画面から戻った後の処理を登録する。
    private val bluetoothEnableLauncher =
        registerForActivityResult(ActivityResultContracts.StartActivityForResult()) {
            // 利用者がBluetoothを有効にしたかを確認する。
            if (bluetoothAdapter?.isEnabled == true) {
                // Bluetoothが有効なら実際の監視を開始する。
                startMonitoring()
            } else {
                // Bluetoothが無効のままなら画面へ通知する。
                binding.statusTextView.text = "Bluetoothが有効になっていません。"
            }
        }

    // BLE広告を受信するコールバックを定義する。
    private val scanCallback = object : ScanCallback() {
        // BLE広告を1件受信したときに呼ばれる。
        override fun onScanResult(callbackType: Int, result: ScanResult) {
            super.onScanResult(callbackType, result)
            // 広告を解析し、対象ビーコンならRSSI履歴を更新する。
            processScanResult(result)
        }

        // 複数のBLE広告結果がまとめて届いたときに呼ばれる。
        override fun onBatchScanResults(results: MutableList<ScanResult>) {
            super.onBatchScanResults(results)
            // 届いた全結果を1件ずつ通常の解析処理へ渡す。
            results.forEach { result -> processScanResult(result) }
        }

        // AndroidがBLEスキャン開始に失敗した場合に呼ばれる。
        override fun onScanFailed(errorCode: Int) {
            super.onScanFailed(errorCode)
            // エラーコードをLogcatへ出力する。
            Log.e(TAG, "BLE scan failed: errorCode=$errorCode")
            // UI操作をメインスレッドで実行する。
            runOnUiThread {
                // 監視一式を安全に停止し、画面へエラーを表示する。
                stopMonitoring("BLEスキャンに失敗しました。 エラーコード: $errorCode")
            }
        }
    }

    // 100ミリ秒ごとに接近、伏せ、発動、画面表示を更新する処理である。
    private val stateCheckRunnable = object : Runnable {
        override fun run() {
            // 監視停止後は次回処理を予約せず終了する。
            if (!isMonitoring) return
            // 判定基準にする単調増加の経過時間を取得する。
            val nowElapsed = SystemClock.elapsedRealtime()
            // RSSI履歴と未受信時間から接近状態を更新する。
            updateBeaconProximityState(nowElapsed)
            // Z値と継続時間から伏せ状態を更新する。
            updateFaceDownState(nowElapsed)
            // 接近状態と伏せ状態の論理積から機能状態を更新する。
            updateFeatureState(nowElapsed)
            // 現在の全状態を画面へ反映する。
            updateInterface(nowElapsed)
            // 100ミリ秒後に同じ処理を再実行する。
            mainHandler.postDelayed(this, STATE_CHECK_INTERVAL_MILLIS)
        }
    }

    // Activityが作成されたときに画面とタップ処理を初期化する。
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // activity_main.xmlからView Bindingオブジェクトを生成する。
        binding = ActivityMainBinding.inflate(layoutInflater)
        // 生成したレイアウトを実際の画面として表示する。
        setContentView(binding.root)

        // XMLのstartButtonと「監視開始」機能を紐づける。
        binding.startButton.setOnClickListener {
            // タップ時に権限を確認し、最終的にstartMonitoring()を呼ぶ。
            requestPermissionsAndStart()
        }
        // XMLのstopButtonと「監視停止」機能を紐づける。
        binding.stopButton.setOnClickListener {
            // タップ時にBLE、センサー、定期判定、発動を停止する。
            stopMonitoring("監視を停止しました。")
        }
        // XMLのclearButtonと「発動履歴消去」機能を紐づける。
        binding.clearButton.setOnClickListener {
            // タップ時にSharedPreferences内の発動履歴を消去する。
            clearActivationHistory()
        }

        // 前回終了時までに保存した発動履歴を画面へ復元する。
        restoreActivationHistory()
        // 起動直後のボタン有効状態を設定する。
        updateButtonState()
        // 起動直後の各表示項目を初期状態へ更新する。
        updateInterface(SystemClock.elapsedRealtime())

        // 端末がBLEに対応しているかを確認する。
        if (!packageManager.hasSystemFeature(PackageManager.FEATURE_BLUETOOTH_LE)) {
            // 非対応端末であることを表示する。
            binding.statusTextView.text =
                "この端末はBluetooth Low Energyに対応していません。"
            // 実行不能なので監視開始ボタンを無効化する。
            binding.startButton.isEnabled = false
        }

        // 重力センサーも加速度センサーも取得できなかったかを確認する。
        if (gravitySensor == null) {
            // センサーを取得できないことを表示する。
            binding.gravityTextView.text =
                "重力センサーおよび加速度センサーを取得できません。"
            // 伏せ判定不能であることを表示する。
            binding.faceDownStateTextView.text = "端末状態: 判定不可"
        }
    }

    // 試作版では画面消灯などでonStopが呼ばれても自動停止しない。
    override fun onStop() {
        super.onStop()
    }

    // Activityが破棄される際に監視資源を解放する。
    override fun onDestroy() {
        // 監視中ならBLEとセンサーを停止する。
        if (isMonitoring) {
            stopMonitoring("アプリを終了したため監視を停止しました。")
        }
        super.onDestroy()
    }

    // Androidバージョンごとに必要な実行時権限を返す。
    private fun requiredPermissions(): Array<String> =
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            // Android 12以降ではBluetooth権限と位置情報権限を要求する。
            arrayOf(
                Manifest.permission.BLUETOOTH_SCAN,
                Manifest.permission.BLUETOOTH_CONNECT,
                Manifest.permission.ACCESS_COARSE_LOCATION,
                Manifest.permission.ACCESS_FINE_LOCATION
            )
        } else {
            // Android 11以前ではBLEスキャン用に位置情報権限を要求する。
            arrayOf(
                Manifest.permission.ACCESS_COARSE_LOCATION,
                Manifest.permission.ACCESS_FINE_LOCATION
            )
        }

    // 必要な実行時権限がすべて許可済みかを返す。
    private fun hasRequiredPermissions(): Boolean =
        requiredPermissions().all { permission ->
            ContextCompat.checkSelfPermission(this, permission) ==
                    PackageManager.PERMISSION_GRANTED
        }

    // 「監視開始」ボタンから呼ばれ、権限確認または権限要求を行う。
    private fun requestPermissionsAndStart() {
        // 未許可の必要権限だけを抽出する。
        val missingPermissions = requiredPermissions().filter { permission ->
            ContextCompat.checkSelfPermission(this, permission) !=
                    PackageManager.PERMISSION_GRANTED
        }
        // 未許可権限がなければBluetooth状態の確認へ進む。
        if (missingPermissions.isEmpty()) {
            checkBluetoothAndStartMonitoring()
        } else {
            // Androidの権限確認画面を表示する。
            permissionLauncher.launch(missingPermissions.toTypedArray())
        }
    }

    // 権限確認済みのBluetooth API利用箇所なのでLint警告を抑制する。
    @SuppressLint("MissingPermission")
    // Bluetoothアダプターの有無と有効状態を確認する。
    private fun checkBluetoothAndStartMonitoring() {
        // 端末のBluetoothAdapterを取得する。
        val adapter = bluetoothAdapter
        // アダプターが存在しない端末では開始しない。
        if (adapter == null) {
            binding.statusTextView.text = "Bluetoothアダプターを取得できません。"
            return
        }
        // Bluetoothが無効ならシステムの有効化確認画面を開く。
        if (!adapter.isEnabled) {
            bluetoothEnableLauncher.launch(Intent(BluetoothAdapter.ACTION_REQUEST_ENABLE))
            return
        }
        // Bluetoothが有効なら実際の監視を開始する。
        startMonitoring()
    }

    // 権限確認済みのBluetooth API利用箇所なのでLint警告を抑制する。
    @SuppressLint("MissingPermission")
    // BLEスキャン、姿勢センサー、定期判定を開始する。
    private fun startMonitoring() {
        // 重複開始を防ぐ。
        if (isMonitoring) {
            binding.statusTextView.text = "すでに監視中です。"
            return
        }
        // 念のため権限を再確認する。
        if (!hasRequiredPermissions()) {
            requestPermissionsAndStart()
            return
        }
        // BluetoothAdapterからBLEスキャナーを取得する。
        val scanner = bluetoothAdapter?.bluetoothLeScanner
        // スキャナーを取得できない場合は開始しない。
        if (scanner == null) {
            binding.statusTextView.text = "BLEスキャナーを取得できません。"
            return
        }

        // 前回監視時の一時状態を初期化する。
        resetRuntimeState()
        // 監視中状態へ切り替える。
        isMonitoring = true
        // AQUOSを置いた状態でも監視画面が消灯しにくいようにする。
        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        // 開始ボタンを無効、停止ボタンを有効にする。
        updateButtonState()

        // 利用可能な重力または加速度センサーを登録する。
        gravitySensor?.let { sensor ->
            // 画面の診断表示用に実際のセンサー種類を記録する。
            isUsingGravitySensor = sensor.type == Sensor.TYPE_GRAVITY
            // 比較的高頻度で姿勢変化を受け取る。
            sensorManager.registerListener(this, sensor, SensorManager.SENSOR_DELAY_GAME)
        }

        try {
            // フィルターなしで周囲のBLE広告スキャンを開始する。
            scanner.startScan(scanCallback)
        } catch (exception: SecurityException) {
            // セキュリティ例外をLogcatへ記録する。
            Log.e(TAG, "BLE scan permission error", exception)
            // 開始途中で登録したセンサーを解除する。
            sensorManager.unregisterListener(this)
            // 監視中状態を解除する。
            isMonitoring = false
            // 画面点灯維持を解除する。
            window.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
            // ボタン状態を停止中へ戻す。
            updateButtonState()
            // 利用者へエラーを表示する。
            binding.statusTextView.text = "BLEスキャン権限エラー: ${exception.message}"
            return
        } catch (exception: Exception) {
            // その他の開始エラーをLogcatへ記録する。
            Log.e(TAG, "BLE scan start error", exception)
            sensorManager.unregisterListener(this)
            isMonitoring = false
            window.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
            updateButtonState()
            binding.statusTextView.text = "BLEスキャン開始エラー: ${exception.message}"
            return
        }

        // 正常開始を画面へ表示する。
        binding.statusTextView.text = "対象iBeaconと端末の伏せ状態を監視しています。"
        // 古い定期処理予約があれば取り消す。
        mainHandler.removeCallbacks(stateCheckRunnable)
        // 状態判定ループをただちに開始する。
        mainHandler.post(stateCheckRunnable)
    }

    // 権限確認済みのBluetooth API利用箇所なのでLint警告を抑制する。
    @SuppressLint("MissingPermission")
    // 「監視停止」ボタン、エラー、Activity破棄時から呼ばれる停止処理である。
    private fun stopMonitoring(message: String) {
        // 定期判定と画面更新を停止する。
        mainHandler.removeCallbacks(stateCheckRunnable)
        // 実際に監視中の場合だけOS資源を解放する。
        if (isMonitoring) {
            try {
                // BLEスキャンを停止する。
                bluetoothAdapter?.bluetoothLeScanner?.stopScan(scanCallback)
            } catch (exception: SecurityException) {
                Log.e(TAG, "BLE scan stop permission error", exception)
            } catch (exception: Exception) {
                Log.e(TAG, "BLE scan stop error", exception)
            }
            // センサーイベントの通知を解除する。
            sensorManager.unregisterListener(this)
        }
        // 発動中なら停止時刻までの継続時間を保存する。
        if (isFeatureActive) stopTargetFeature(SystemClock.elapsedRealtime())
        // 監視状態と判定状態を停止中へ戻す。
        isMonitoring = false
        isNearTargetBeacon = false
        isFaceDown = false
        nearCandidateStartElapsed = null
        faceDownCandidateStartElapsed = null
        faceUpCandidateStartElapsed = null
        // 画面点灯維持を解除する。
        window.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        // 停止理由を画面へ表示する。
        binding.statusTextView.text = message
        // ボタンと状態表示を停止中へ更新する。
        updateButtonState()
        updateInterface(SystemClock.elapsedRealtime())
    }

    // 監視開始時に前回セッションの一時状態を初期化する。
    private fun resetRuntimeState() {
        recentRssiValues.clear()
        isNearTargetBeacon = false
        isFaceDown = false
        isFeatureActive = false
        nearCandidateStartElapsed = null
        faceDownCandidateStartElapsed = null
        faceUpCandidateStartElapsed = null
        lastTargetBeaconSeenElapsed = null
        featureActivationStartElapsed = null
        featureActivationStartWallTime = null
        lastDiagnosticMacSeenElapsed = null
        lastDiagnosticMacRssi = null
        lastDiagnosticMacRawData = null
        currentGravityZ = Float.NaN
    }

    // result.device.addressの利用権限は確認済みなのでLint警告を抑制する。
    @SuppressLint("MissingPermission")
    // 受信したBLE広告を診断保存し、iBeaconなら対象照合を行う。
    private fun processScanResult(result: ScanResult) {
        // 機器のMACアドレスを取得する。
        val macAddress = try {
            result.device.address
        } catch (_: SecurityException) {
            "(取得不可)"
        }
        // BLE広告の解析済みレコードを取得する。
        val scanRecord = result.scanRecord ?: return
        // 広告全体を16進数文字列へ変換する。
        val rawAdvertisement = scanRecord.bytes.toHexString()

        // R1 Beaconの設定用広告なら診断表示用データを更新する。
        if (macAddress.equals(DIAGNOSTIC_TARGET_MAC, ignoreCase = true)) {
            lastDiagnosticMacSeenElapsed = SystemClock.elapsedRealtime()
            lastDiagnosticMacRssi = result.rssi
            lastDiagnosticMacRawData = rawAdvertisement
        }

        // Manufacturer DataをiBeaconとして解析する。
        val beacon = parseIBeacon(scanRecord) ?: return
        // UUID、Major、Minorが対象と一致しなければ無視する。
        if (!isTargetBeacon(beacon)) return

        // 対象iBeaconの最終受信時刻を更新する。
        lastTargetBeaconSeenElapsed = SystemClock.elapsedRealtime()
        // 最新RSSIを履歴末尾へ追加する。
        recentRssiValues.addLast(result.rssi)
        // 履歴が指定件数を超えたら最も古い値を削除する。
        while (recentRssiValues.size > RSSI_WINDOW_SIZE) {
            recentRssiValues.removeFirst()
        }
    }

    // 解析したiBeaconが指定UUID、Major、Minorと一致するかを返す。
    private fun isTargetBeacon(beacon: IBeaconData): Boolean =
        beacon.uuid.equals(TARGET_UUID, ignoreCase = true) &&
                beacon.major == TARGET_MAJOR &&
                beacon.minor == TARGET_MINOR

    // RSSI中央値、継続時間、未受信時間から接近状態を更新する。
    private fun updateBeaconProximityState(nowElapsed: Long) {
        // 最終受信時刻を取得する。
        val lastSeen = lastTargetBeaconSeenElapsed
        // 未受信または8秒以上受信していない場合は離脱とする。
        if (lastSeen == null || nowElapsed - lastSeen >= BEACON_TIMEOUT_MILLIS) {
            isNearTargetBeacon = false
            nearCandidateStartElapsed = null
            recentRssiValues.clear()
            return
        }
        // 直近RSSIの中央値を取得する。
        val medianRssi = calculateMedianRssi() ?: return
        // 現在まだ離脱状態の場合の接近判定を行う。
        if (!isNearTargetBeacon) {
            // 中央値が-65 dBm以上なら接近候補とする。
            if (medianRssi >= NEAR_RSSI_THRESHOLD) {
                val candidateStart = nearCandidateStartElapsed
                // 初回なら接近候補開始時刻を保存する。
                if (candidateStart == null) {
                    nearCandidateStartElapsed = nowElapsed
                    // 2秒以上継続したら接近状態へ移る。
                } else if (nowElapsed - candidateStart >= NEAR_CONFIRM_DURATION_MILLIS) {
                    isNearTargetBeacon = true
                    nearCandidateStartElapsed = null
                }
            } else {
                // しきい値を下回ったら接近候補を取り消す。
                nearCandidateStartElapsed = null
            }
            // 現在接近中の場合の離脱判定を行う。
        } else if (medianRssi < EXIT_RSSI_THRESHOLD) {
            // 中央値が-78 dBm未満ならただちに離脱状態へ移る。
            isNearTargetBeacon = false
            nearCandidateStartElapsed = null
        }
    }

    // 重力Z値と継続時間から伏せ開始または伏せ解除を判定する。
    private fun updateFaceDownState(nowElapsed: Long) {
        // まだセンサー値が届いていない場合は判定しない。
        if (currentGravityZ.isNaN()) return
        // 現在伏せ状態でない場合の伏せ開始判定を行う。
        if (!isFaceDown) {
            faceUpCandidateStartElapsed = null
            // Z値が-8.0未満なら伏せ候補とする。
            if (currentGravityZ < FACE_DOWN_ENTER_Z) {
                val candidateStart = faceDownCandidateStartElapsed
                // 初回なら伏せ候補開始時刻を保存する。
                if (candidateStart == null) {
                    faceDownCandidateStartElapsed = nowElapsed
                    // 1.5秒以上継続したら伏せ状態へ移る。
                } else if (
                    nowElapsed - candidateStart >= FACE_DOWN_CONFIRM_DURATION_MILLIS
                ) {
                    isFaceDown = true
                    faceDownCandidateStartElapsed = null
                }
            } else {
                // しきい値を満たさなければ伏せ候補を取り消す。
                faceDownCandidateStartElapsed = null
            }
            // 現在伏せ状態の場合の解除判定を行う。
        } else {
            faceDownCandidateStartElapsed = null
            // Z値が-6.0より大きければ解除候補とする。
            if (currentGravityZ > FACE_DOWN_EXIT_Z) {
                val candidateStart = faceUpCandidateStartElapsed
                // 初回なら解除候補開始時刻を保存する。
                if (candidateStart == null) {
                    faceUpCandidateStartElapsed = nowElapsed
                    // 0.7秒以上継続したら伏せ状態を解除する。
                } else if (nowElapsed - candidateStart >= FACE_UP_CONFIRM_DURATION_MILLIS) {
                    isFaceDown = false
                    faceUpCandidateStartElapsed = null
                }
            } else {
                // 解除しきい値を満たさなければ解除候補を取り消す。
                faceUpCandidateStartElapsed = null
            }
        }
    }

    // 接近状態と伏せ状態の論理積に応じて機能を開始または停止する。
    private fun updateFeatureState(nowElapsed: Long) {
        // 接近中かつ伏せ状態の場合だけ発動すべきである。
        val shouldBeActive = isNearTargetBeacon && isFaceDown
        // 条件成立へ変化した瞬間だけ開始処理を呼ぶ。
        if (shouldBeActive && !isFeatureActive) {
            startTargetFeature(nowElapsed)
            // 条件不成立へ変化した瞬間だけ停止処理を呼ぶ。
        } else if (!shouldBeActive && isFeatureActive) {
            stopTargetFeature(nowElapsed)
        }
    }

    // 条件成立時に一度だけ呼ばれ、発動開始日時と開始経過時刻を記録する。
    private fun startTargetFeature(nowElapsed: Long) {
        isFeatureActive = true
        featureActivationStartElapsed = nowElapsed
        featureActivationStartWallTime = System.currentTimeMillis()
        // アプリ再起動後も確認できるよう発動開始日時と時間0を保存する。
        preferences.edit()
            .putLong(
                PREF_LAST_ACTIVATION_START_WALL_TIME,
                featureActivationStartWallTime ?: 0L
            )
            .putLong(PREF_LAST_ACTIVATION_DURATION, 0L)
            .apply()
        // 将来の実機能の開始処理はこの位置へ追加する。
        Log.d(TAG, "Target feature started.")
    }

    // 条件解除時に一度だけ呼ばれ、発動継続時間を保存する。
    private fun stopTargetFeature(nowElapsed: Long) {
        // 発動開始から停止までの経過時間を計算する。
        val duration = featureActivationStartElapsed?.let {
            (nowElapsed - it).coerceAtLeast(0L)
        } ?: 0L
        // 直近の発動時間として端末内へ保存する。
        preferences.edit()
            .putLong(PREF_LAST_ACTIVATION_DURATION, duration)
            .apply()
        // 将来の実機能の停止処理はこの位置へ追加する。
        isFeatureActive = false
        featureActivationStartElapsed = null
        featureActivationStartWallTime = null
        Log.d(TAG, "Target feature stopped. duration=$duration ms")
    }

    // 「発動履歴を消去」ボタンから呼ばれる。
    private fun clearActivationHistory() {
        // 発動中なら現在時点から新しい計測として開始日時と時間をリセットする。
        if (isFeatureActive) {
            featureActivationStartElapsed = SystemClock.elapsedRealtime()
            featureActivationStartWallTime = System.currentTimeMillis()
            preferences.edit()
                .putLong(
                    PREF_LAST_ACTIVATION_START_WALL_TIME,
                    featureActivationStartWallTime ?: 0L
                )
                .putLong(PREF_LAST_ACTIVATION_DURATION, 0L)
                .apply()
            binding.statusTextView.text = "発動中の計測時間をリセットしました。"
        } else {
            // 停止中なら保存済み開始日時と継続時間を削除する。
            preferences.edit()
                .remove(PREF_LAST_ACTIVATION_START_WALL_TIME)
                .remove(PREF_LAST_ACTIVATION_DURATION)
                .apply()
            binding.lastActivationTimeTextView.text = "直近の発動開始: まだありません"
            binding.activationDurationTextView.text = "直近の発動時間: 00:00.0"
            binding.statusTextView.text = "発動履歴を消去しました。"
        }
    }

    // SharedPreferencesから直近の発動履歴を読み出して画面へ表示する。
    private fun restoreActivationHistory() {
        val startWallTime = preferences.getLong(
            PREF_LAST_ACTIVATION_START_WALL_TIME,
            0L
        )
        val duration = preferences.getLong(PREF_LAST_ACTIVATION_DURATION, 0L)
        if (startWallTime > 0L) {
            binding.lastActivationTimeTextView.text =
                "直近の発動開始: ${formatDateTime(startWallTime)}"
            binding.activationDurationTextView.text =
                "直近の発動時間: ${formatDuration(duration)}"
        } else {
            binding.lastActivationTimeTextView.text = "直近の発動開始: まだありません"
            binding.activationDurationTextView.text = "直近の発動時間: 00:00.0"
        }
    }

    // 現在の内部状態をactivity_main.xml内の各TextViewへ反映する。
    private fun updateInterface(nowElapsed: Long) {
        // 8秒以内に対象iBeaconを受信していれば受信中と表示する。
        val lastSeen = lastTargetBeaconSeenElapsed
        val beaconReceived = lastSeen != null &&
                nowElapsed - lastSeen < BEACON_TIMEOUT_MILLIS
        binding.targetBeaconTextView.text =
            if (beaconReceived) "対象ビーコン: 受信中" else "対象ビーコン: 未受信"

        // RSSI中央値を表示する。
        val medianRssi = calculateMedianRssi()
        binding.rssiTextView.text =
            if (medianRssi != null) "RSSI中央値: $medianRssi dBm"
            else "RSSI中央値: -- dBm"
        // 接近または離脱を表示する。
        binding.proximityStateTextView.text =
            if (isNearTargetBeacon) "接近状態: 接近中" else "接近状態: 離脱"
        // 重力Z値を小数第2位まで表示する。
        binding.gravityTextView.text = if (currentGravityZ.isNaN()) {
            "重力センサーZ: -- m/s²"
        } else {
            String.format(Locale.JAPAN, "重力センサーZ: %.2f m/s²", currentGravityZ)
        }
        // 伏せ判定を表示する。
        binding.faceDownStateTextView.text =
            if (isFaceDown) "端末状態: 伏せ" else "端末状態: 伏せられていない"

        // 発動中と停止中で機能状態表示の文字と色を切り替える。
        if (isFeatureActive) {
            binding.featureStateTextView.text = "機能状態: 発動中"
            binding.featureStateTextView.setTextColor(Color.WHITE)
            binding.featureStateTextView.setBackgroundColor(Color.rgb(46, 125, 50))
            featureActivationStartWallTime?.let {
                binding.lastActivationTimeTextView.text =
                    "直近の発動開始: ${formatDateTime(it)}"
            }
            val activeDuration = featureActivationStartElapsed?.let {
                (nowElapsed - it).coerceAtLeast(0L)
            } ?: 0L
            binding.activationDurationTextView.text =
                "現在の発動時間: ${formatDuration(activeDuration)}"
        } else {
            binding.featureStateTextView.text = "機能状態: 停止中"
            binding.featureStateTextView.setTextColor(Color.BLACK)
            binding.featureStateTextView.setBackgroundColor(Color.rgb(224, 224, 224))
            // 停止中は保存済みの直近履歴を表示する。
            restoreActivationHistory()
        }
        // 画面下部の判定条件とR1 Beacon診断を更新する。
        updateDetailText(nowElapsed)
    }

    // 判定条件、センサー種類、R1 Beacon設定用広告の診断を表示する。
    private fun updateDetailText(nowElapsed: Long) {
        // 設定用広告を直近8秒以内に受信したかを判定する。
        val diagnosticRecentlyReceived = lastDiagnosticMacSeenElapsed?.let {
            nowElapsed - it < BEACON_TIMEOUT_MILLIS
        } ?: false
        // 複数行の診断文字列を組み立てる。
        binding.detailTextView.text = buildString {
            appendLine("対象iBeacon")
            appendLine("UUID: $TARGET_UUID")
            appendLine("Major=$TARGET_MAJOR, Minor=$TARGET_MINOR")
            appendLine()
            appendLine("接近開始: RSSI中央値 >= $NEAR_RSSI_THRESHOLD dBm を2秒")
            appendLine("離脱: RSSI中央値 < $EXIT_RSSI_THRESHOLD dBm")
            appendLine("未受信タイムアウト: 8秒")
            appendLine()
            appendLine("伏せ開始: Z < $FACE_DOWN_ENTER_Z を1.5秒")
            appendLine("伏せ解除: Z > $FACE_DOWN_EXIT_Z を0.7秒")
            appendLine()
            append("姿勢センサー: ")
            appendLine(
                when {
                    gravitySensor == null -> "利用不可"
                    isUsingGravitySensor -> "重力センサー"
                    else -> "加速度センサー"
                }
            )
            appendLine()
            appendLine("R1 Beacon診断")
            appendLine("MAC: $DIAGNOSTIC_TARGET_MAC")
            if (diagnosticRecentlyReceived) {
                appendLine("設定用広告: 受信中")
                appendLine("RSSI: ${lastDiagnosticMacRssi ?: "--"} dBm")
                val targetRecentlyReceived = lastTargetBeaconSeenElapsed?.let {
                    nowElapsed - it < BEACON_TIMEOUT_MILLIS
                } ?: false
                appendLine(
                    "iBeacon広告: ${if (targetRecentlyReceived) "受信中" else "未受信"}"
                )
                appendLine()
                appendLine("最後の設定用広告:")
                append(lastDiagnosticMacRawData ?: "(なし)")
            } else {
                appendLine("設定用広告: 未受信")
                append("iBeacon広告: 未受信")
            }
        }
    }

    // 直近RSSI値を並べ替え、その中央値を返す。
    private fun calculateMedianRssi(): Int? {
        // 履歴が空なら中央値は求められない。
        if (recentRssiValues.isEmpty()) return null
        // RSSIを昇順に並べる。
        val sortedValues = recentRssiValues.sorted()
        // 中央位置の添字を求める。
        val middleIndex = sortedValues.size / 2
        // 奇数件なら中央値、偶数件なら中央2値の整数平均を返す。
        return if (sortedValues.size % 2 == 1) {
            sortedValues[middleIndex]
        } else {
            (sortedValues[middleIndex - 1] + sortedValues[middleIndex]) / 2
        }
    }

    // ミリ秒の実日時を日本語向け日時文字列へ変換する。
    private fun formatDateTime(wallTimeMillis: Long): String =
        SimpleDateFormat("yyyy/MM/dd HH:mm:ss", Locale.JAPAN)
            .format(Date(wallTimeMillis))

    // ミリ秒の継続時間を時、分、秒、0.1秒単位の文字列へ変換する。
    private fun formatDuration(durationMillis: Long): String {
        // 負値を0へ補正し、0.1秒単位へ変換する。
        val totalTenths = durationMillis.coerceAtLeast(0L) / 100L
        // 時、分、秒、小数第1位を求める。
        val hours = totalTenths / 36_000L
        val minutes = (totalTenths / 600L) % 60L
        val seconds = (totalTenths / 10L) % 60L
        val tenths = totalTenths % 10L
        // 1時間以上なら時も表示する。
        return if (hours > 0L) {
            String.format(
                Locale.JAPAN,
                "%02d:%02d:%02d.%d",
                hours,
                minutes,
                seconds,
                tenths
            )
        } else {
            String.format(Locale.JAPAN, "%02d:%02d.%d", minutes, seconds, tenths)
        }
    }

    // BLE Manufacturer Specific Dataを標準iBeaconとして解析する。
    private fun parseIBeacon(
        scanRecord: android.bluetooth.le.ScanRecord
    ): IBeaconData? {
        // Apple Company ID 0x004CのManufacturer Dataを取得する。
        val data = scanRecord.getManufacturerSpecificData(0x004C) ?: return null
        // iBeaconに必要な23バイト未満なら解析しない。
        if (data.size < 23) return null
        // iBeacon識別子02 15で始まらなければ解析しない。
        if (data[0].toUnsignedInt() != 0x02 || data[1].toUnsignedInt() != 0x15) {
            return null
        }
        // 2番目から17番目までの16バイトをUUID文字列へ変換する。
        val uuidHex = data.copyOfRange(2, 18).toHexString(separator = "")
        // 32桁の16進数へUUID標準形式のハイフンを挿入する。
        val uuid = buildString {
            append(uuidHex.substring(0, 8))
            append("-")
            append(uuidHex.substring(8, 12))
            append("-")
            append(uuidHex.substring(12, 16))
            append("-")
            append(uuidHex.substring(16, 20))
            append("-")
            append(uuidHex.substring(20, 32))
        }.uppercase(Locale.US)
        // Majorの2バイトをビッグエンディアン整数として復元する。
        val major =
            (data[18].toUnsignedInt() shl 8) or data[19].toUnsignedInt()
        // Minorの2バイトをビッグエンディアン整数として復元する。
        val minor =
            (data[20].toUnsignedInt() shl 8) or data[21].toUnsignedInt()
        // 最後の1バイトを符号付きMeasured Powerとして読む。
        val measuredPower = data[22].toInt()
        // 解析結果を専用データクラスへまとめて返す。
        return IBeaconData(uuid, major, minor, measuredPower)
    }

    // 重力または加速度センサーの新しい測定値を受け取る。
    override fun onSensorChanged(event: SensorEvent) {
        // 監視停止中のイベントは無視する。
        if (!isMonitoring) return
        // 対象外センサーからのイベントは無視する。
        if (
            event.sensor.type != Sensor.TYPE_GRAVITY &&
            event.sensor.type != Sensor.TYPE_ACCELEROMETER
        ) return
        // 重力センサーならZ値を直接使用する。
        currentGravityZ = if (
            event.sensor.type == Sensor.TYPE_GRAVITY || currentGravityZ.isNaN()
        ) {
            event.values[2]
        } else {
            // 加速度センサーの場合はローパスフィルターで動きの影響を弱める。
            val alpha = 0.8f
            alpha * currentGravityZ + (1.0f - alpha) * event.values[2]
        }
    }

    // センサー精度変化は診断ログだけに記録する。
    override fun onAccuracyChanged(sensor: Sensor?, accuracy: Int) {
        Log.d(TAG, "Sensor accuracy changed: accuracy=$accuracy")
    }

    // 監視状態に応じて開始ボタンと停止ボタンの有効状態を切り替える。
    private fun updateButtonState() {
        // 監視停止中だけ開始ボタンをタップ可能にする。
        binding.startButton.isEnabled = !isMonitoring
        // 監視中だけ停止ボタンをタップ可能にする。
        binding.stopButton.isEnabled = isMonitoring
    }

    // 符号付きByteを0から255の符号なし整数へ変換する。
    private fun Byte.toUnsignedInt(): Int = toInt() and 0xFF

    // ByteArrayを指定区切り文字の大文字16進数文字列へ変換する。
    private fun ByteArray.toHexString(separator: String = " "): String =
        joinToString(separator) { byte ->
            String.format(Locale.US, "%02X", byte.toUnsignedInt())
        }

    // 1件のiBeacon解析結果をまとめて保持する不変データクラスである。
    private data class IBeaconData(
        // 128ビットのUUIDを標準文字列形式で保持する。
        val uuid: String,
        // 16ビットのMajor値を保持する。
        val major: Int,
        // 16ビットのMinor値を保持する。
        val minor: Int,
        // 距離推定用の1メートル校正RSSIを保持する。
        val measuredPower: Int
    )
}
