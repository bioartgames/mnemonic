class_name HookCaptureSession
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookCoreLauncherGd = preload("res://addons/mnemonic_hook/ipc/hook_core_launcher.gd")
const HookCoreProcessProbeGd = preload("res://addons/mnemonic_hook/ipc/hook_core_process_probe.gd")
const HookCoreShutdownGd = preload("res://addons/mnemonic_hook/ipc/hook_core_shutdown.gd")
const HookSettingsIoGd = preload("res://addons/mnemonic_hook/ipc/hook_settings_io.gd")


static func start_recording(plugin: EditorPlugin) -> Dictionary:
	if plugin == null:
		return { "ok": false, "message": "Mnemonic Hook is not ready." }
	var paths: MnemonicDataRootPaths = plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		return { "ok": false, "message": "DataRoot unavailable." }

	HookSettingsIoGd.write_start_recording_on_launch(paths, true)

	if not HookCoreProcessProbeGd.is_running():
		var launch_result: Dictionary = plugin.launch_core()
		if not bool(launch_result.get("ok", false)):
			return launch_result
		if not _wait_for_core_running():
			return {
				"ok": false,
				"message": "Core did not start in time — check Editor Settings or build mnemonic-core",
			}

	if not plugin.request_resume_capture():
		return { "ok": false, "message": "Start recording failed" }

	if _wait_for_recording(plugin):
		return { "ok": true, "message": "Recording" }
	return { "ok": true, "message": "Start requested" }


static func stop_recording(plugin: EditorPlugin) -> Dictionary:
	if plugin == null:
		return { "ok": false, "message": "Mnemonic Hook is not ready." }
	var paths: MnemonicDataRootPaths = plugin.get_data_root_paths()
	if paths == null or not paths.is_valid():
		return { "ok": false, "message": "DataRoot unavailable." }

	if not HookCoreProcessProbeGd.is_running():
		return { "ok": true, "message": "Core not running" }

	if not HookCoreShutdownGd.request_graceful_shutdown(paths):
		return { "ok": false, "message": "Stop recording failed — Core still running" }
	return { "ok": true, "message": "Recording stopped" }


static func _wait_for_core_running() -> bool:
	var deadline_ms := Time.get_ticks_msec() + int(Mc.CORE_LAUNCH_WAIT_SEC * 1000.0)
	while Time.get_ticks_msec() < deadline_ms:
		if HookCoreProcessProbeGd.is_running():
			return true
		OS.delay_msec(int(Mc.CORE_LAUNCH_POLL_SEC * 1000.0))
	return false


static func _wait_for_recording(plugin: EditorPlugin) -> bool:
	var deadline_ms := Time.get_ticks_msec() + int(Mc.CORE_RECORDING_WAIT_SEC * 1000.0)
	while Time.get_ticks_msec() < deadline_ms:
		var snap = plugin.get_status_snapshot()
		if snap != null:
			var st: String = snap.state.strip_edges().to_lower()
			if st == Mc.CAPTURE_STATE_RECORDING and snap.recording:
				return true
		OS.delay_msec(int(Mc.CORE_LAUNCH_POLL_SEC * 1000.0))
	return false
