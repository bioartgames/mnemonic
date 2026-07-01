@tool
extends EditorPlugin

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const DataRootPathsGd = preload("res://addons/mnemonic/ipc/data_root_paths.gd")
const HookCoreLauncherGd = preload("res://addons/mnemonic/ipc/hook_core_launcher.gd")
const HookEditorWorkflowSettingsGd = preload(
	"res://addons/mnemonic/ipc/hook_editor_workflow_settings.gd"
)
const HookDockLayoutGd = preload("res://addons/mnemonic/ui/hook_dock_layout.gd")
const HookPluginWindowLayoutGd = preload("res://addons/mnemonic/ui/hook_plugin_window_layout.gd")
const HookDockShortcutGd = preload("res://addons/mnemonic/ui/hook_dock_shortcut.gd")
const HookEditorStartupGateGd = preload("res://addons/mnemonic/ipc/hook_editor_startup_gate.gd")
const HookEditorSceneSnapshotGd = preload(
	"res://addons/mnemonic/ipc/hook_editor_scene_snapshot.gd"
)
const _SIGNAL_RESOURCE_SAVED := &"resource_saved"

#
# Plugin-load stays lightweight: defer dock/pollers to `_activate_after_editor_layout()`.
# Instantiate runtime types via global class_name (load().new() fails on Godot 4.5+).
#
var _paths: MnemonicDataRootPaths
var _editor_startup_finished := false
var _pending_dock_slot: int = Mc.DEFAULT_DOCK_SLOT
var _dock_slot: int = Mc.DEFAULT_DOCK_SLOT

var _git_poll: HookGitPoll
var _playtest: HookPlaytestTracker
var _scene_tracker: HookSceneTracker
var _resource_tracker: HookResourceTracker
var _focus_tracker: HookEditorFocusTracker
var _focus_session_tracker: HookEditorFocusSessionTracker
var _lifecycle_poller: HookLifecyclePoller
var _resource_saved_connected := false
var _main_screen_changed_connected := false
var _playtest_timer: Timer
var _git_timer: Timer
var _focus_session_timer: Timer
var _status_timer: Timer
var _dock: EditorMnemonicDock
var _runtime_error_debugger: HookRuntimeErrorDebuggerPlugin

var _t_enter_tree_us := 0
var _t_activate_us := 0


func _enter_tree() -> void:
	_paths = DataRootPathsGd.new()
	if not _paths.is_supported_platform():
		push_warning("Mnemonic: Windows only in v1; paths unavailable.")
		return
	if not _paths.is_valid():
		push_error("Mnemonic: Could not resolve DataRoot (LOCALAPPDATA missing).")
		return
	_log_verbose("Mnemonic: DataRoot = %s" % _paths.get_root())
	_t_enter_tree_us = Time.get_ticks_usec()
	_log_verbose("Mnemonic: _enter_tree_us=%d" % _t_enter_tree_us)
	HookCoreLauncherGd.ensure_editor_setting_registered(get_editor_interface())
	HookDockLayoutGd.ensure_editor_setting_registered(get_editor_interface())
	HookEditorWorkflowSettingsGd.ensure_registered(get_editor_interface())


func _set_window_layout(configuration: ConfigFile) -> void:
	# Pin dock placement to the same left-side stack as Scene/FileSystem.
	# We still keep layout persistence codepaths for future flexibility.
	_pending_dock_slot = Mc.DEFAULT_DOCK_SLOT
	call_deferred("_try_activate_after_editor_layout")


func _get_window_layout(configuration: ConfigFile) -> void:
	var slot := _dock_slot
	if is_instance_valid(_dock):
		_dock_slot = HookPluginWindowLayoutGd.clamp_dock_slot(slot)
	HookPluginWindowLayoutGd.write_dock_slot(configuration, _dock_slot)


func _try_activate_after_editor_layout() -> void:
	if _editor_startup_finished:
		return
	if _paths == null or not _paths.is_valid():
		return
	if not HookEditorStartupGateGd.can_activate_heavy_work(get_editor_interface()):
		call_deferred("_try_activate_after_editor_layout")
		return
	_activate_after_editor_layout()


func _activate_after_editor_layout() -> void:
	if _editor_startup_finished:
		return
	_editor_startup_finished = true
	_t_activate_us = Time.get_ticks_usec()

	var events_path := _paths.get_session_events_file()
	_git_poll = HookGitPoll.new(_paths)
	_playtest = HookPlaytestTracker.new(events_path)
	_scene_tracker = HookSceneTracker.new(events_path)
	_resource_tracker = HookResourceTracker.new(events_path)
	_focus_tracker = HookEditorFocusTracker.new(events_path)
	_focus_session_tracker = HookEditorFocusSessionTracker.new(
		events_path,
		Callable(self, "_is_playing_scene"),
	)
	_focus_tracker.set_on_focus_bucket_changed(
		_focus_session_tracker.on_focus_bucket_changed
	)
	_runtime_error_debugger = HookRuntimeErrorDebuggerPlugin.new(
		events_path,
		func() -> bool:
			if _is_playing_scene():
				return true
			return _playtest != null and _playtest.is_within_error_capture_window()
	)
	add_debugger_plugin(_runtime_error_debugger)
	_playtest.set_on_playtest_stopped(_runtime_error_debugger.on_playtest_stopped)
	_lifecycle_poller = HookLifecyclePoller.new(_paths, _is_verbose_logging())

	scene_saved.connect(_on_scene_saved)
	scene_changed.connect(_on_scene_changed)
	main_screen_changed.connect(_on_main_screen_changed)
	_main_screen_changed_connected = true
	_connect_resource_saved_if_available()

	_playtest_timer = Timer.new()
	_playtest_timer.wait_time = Mc.PLAYTEST_POLL_INTERVAL_SEC
	_playtest_timer.one_shot = false
	_playtest_timer.process_mode = Node.PROCESS_MODE_ALWAYS
	add_child(_playtest_timer)
	_playtest_timer.timeout.connect(_on_playtest_tick)

	_git_timer = Timer.new()
	_git_timer.wait_time = Mc.GIT_POLL_INTERVAL_SEC
	_git_timer.one_shot = false
	_git_timer.process_mode = Node.PROCESS_MODE_ALWAYS
	add_child(_git_timer)
	_git_timer.timeout.connect(_on_git_tick)

	_focus_session_timer = Timer.new()
	_focus_session_timer.wait_time = Mc.FOCUS_SESSION_TICK_SEC
	_focus_session_timer.one_shot = false
	_focus_session_timer.process_mode = Node.PROCESS_MODE_ALWAYS
	add_child(_focus_session_timer)
	_focus_session_timer.timeout.connect(_on_focus_session_tick)

	_status_timer = Timer.new()
	_status_timer.wait_time = Mc.STATUS_POLL_INTERVAL_SEC
	_status_timer.one_shot = false
	_status_timer.process_mode = Node.PROCESS_MODE_ALWAYS
	add_child(_status_timer)
	_status_timer.timeout.connect(_on_status_tick)

	_playtest_timer.start()
	_git_timer.start()
	_focus_session_timer.start()
	_status_timer.start()

	_dock = EditorMnemonicDock.new()
	_dock.setup(self)
	_dock_slot = _pending_dock_slot
	add_control_to_dock(_dock_slot, _dock, HookDockShortcutGd.make_default())
	call_deferred("_deferred_plugin_ready")

	var dt_ms := int((_t_activate_us - _t_enter_tree_us) / 1000)
	_log_verbose("Mnemonic: activate_after_editor_layout dt=%d ms" % dt_ms)


func _deferred_plugin_ready() -> void:
	if _lifecycle_poller != null:
		_lifecycle_poller.poll(true)
	if is_instance_valid(_dock):
		_dock.on_plugin_ready()
	call_deferred("_maybe_auto_start_recording_on_open")


func _maybe_auto_start_recording_on_open() -> void:
	if _paths == null or not _paths.is_valid():
		return
	if not HookEditorWorkflowSettingsGd.read_auto_launch_core():
		return
	if HookCoreRunningCache.read():
		return
	var result := start_recording_session()
	if is_instance_valid(_dock):
		_dock.show_workflow_toast(
			str(result.get("message", "Starting recording…"))
			if bool(result.get("ok", false))
			else (
				"Auto-start failed — set mnemonic/core_windows_exe in Editor Settings "
				+ "or build mnemonic-core"
			)
		)


func _exit_tree() -> void:
	if _paths != null and _paths.is_valid():
		if HookEditorWorkflowSettingsGd.read_stop_core_on_editor_exit():
			HookCoreShutdown.request_graceful_shutdown(_paths)
	# Hook stops Core on editor exit only when stop_core_on_editor_exit is enabled.
	if is_instance_valid(_dock):
		remove_control_from_docks(_dock)
		_dock.queue_free()
		_dock = null
	if is_instance_valid(_playtest_timer):
		_playtest_timer.stop()
		_playtest_timer.queue_free()
		_playtest_timer = null
	if is_instance_valid(_git_timer):
		_git_timer.stop()
		_git_timer.queue_free()
		_git_timer = null
	if is_instance_valid(_focus_session_timer):
		_focus_session_timer.stop()
		_focus_session_timer.queue_free()
		_focus_session_timer = null
	if is_instance_valid(_status_timer):
		_status_timer.stop()
		_status_timer.queue_free()
		_status_timer = null
	if _runtime_error_debugger != null:
		if _runtime_error_debugger.has_method("detach"):
			_runtime_error_debugger.detach()
		remove_debugger_plugin(_runtime_error_debugger)
		_runtime_error_debugger = null
	if scene_saved.is_connected(_on_scene_saved):
		scene_saved.disconnect(_on_scene_saved)
	if scene_changed.is_connected(_on_scene_changed):
		scene_changed.disconnect(_on_scene_changed)
	if _main_screen_changed_connected and main_screen_changed.is_connected(_on_main_screen_changed):
		main_screen_changed.disconnect(_on_main_screen_changed)
		_main_screen_changed_connected = false
	_disconnect_resource_saved_if_connected()
	_git_poll = null
	_playtest = null
	_scene_tracker = null
	_resource_tracker = null
	_focus_tracker = null
	_focus_session_tracker = null
	_lifecycle_poller = null
	_paths = null
	_editor_startup_finished = false


func _on_playtest_tick() -> void:
	if _playtest == null:
		return
	var editor_interface := get_editor_interface()
	if editor_interface == null:
		return
	_playtest.poll(editor_interface)
	_refresh_editor_scene_snapshot()


func _on_git_tick() -> void:
	if _git_poll == null:
		return
	_git_poll.tick()


func _on_focus_session_tick() -> void:
	if _focus_session_tracker == null:
		return
	_focus_session_tracker.tick()


func _on_scene_saved(filepath: String) -> void:
	if _scene_tracker == null:
		return
	_scene_tracker.on_scene_saved(filepath)
	_refresh_editor_scene_snapshot()


func _on_scene_changed(scene_root: Node) -> void:
	if _scene_tracker == null:
		return
	_scene_tracker.on_scene_changed(scene_root)
	_refresh_editor_scene_snapshot()


func _on_resource_saved(resource: Resource) -> void:
	if _resource_tracker == null or resource == null:
		return
	var path := resource.resource_path
	if path.is_empty():
		return
	_resource_tracker.on_script_saved(path)


func _on_main_screen_changed(screen_name: String) -> void:
	if _focus_tracker == null:
		return
	_focus_tracker.on_main_screen_name_changed(screen_name)


func _connect_resource_saved_if_available() -> void:
	if _resource_saved_connected:
		return
	if not has_signal(_SIGNAL_RESOURCE_SAVED):
		push_warning("Mnemonic: resource_saved signal unavailable; resource_saved events disabled.")
		return
	connect(_SIGNAL_RESOURCE_SAVED, _on_resource_saved)
	_resource_saved_connected = true


func _disconnect_resource_saved_if_connected() -> void:
	if not _resource_saved_connected:
		return
	if is_connected(_SIGNAL_RESOURCE_SAVED, _on_resource_saved):
		disconnect(_SIGNAL_RESOURCE_SAVED, _on_resource_saved)
	_resource_saved_connected = false


func _is_playing_scene() -> bool:
	var ei := get_editor_interface()
	return ei != null and ei.is_playing_scene()


func _on_status_tick() -> void:
	_refresh_editor_scene_snapshot()
	if _lifecycle_poller == null:
		return
	_lifecycle_poller.poll()
	if is_instance_valid(_dock) and _dock.should_refresh_from_status_poll():
		_dock.refresh_from_status_poll()


func get_data_root_paths() -> MnemonicDataRootPaths:
	return _paths


func _refresh_editor_scene_snapshot() -> void:
	if _paths == null or not _paths.is_valid():
		return
	var editor_interface := get_editor_interface()
	if editor_interface == null:
		return
	HookEditorSceneSnapshotGd.write_snapshot(_paths, editor_interface)


func set_verbose_logging(enabled: bool) -> void:
	HookEditorWorkflowSettingsGd.write_verbose_logging(enabled)
	if _lifecycle_poller != null:
		_lifecycle_poller.set_verbose_logging(enabled)


func _is_verbose_logging() -> bool:
	return HookEditorWorkflowSettingsGd.read_verbose_logging()


func _log_verbose(message: String) -> void:
	if _is_verbose_logging():
		print(message)


func get_lifecycle_state() -> HookLifecycleState:
	if _lifecycle_poller == null:
		return HookLifecycleState.new()
	return _lifecycle_poller.get_last()


func is_core_running() -> bool:
	return get_lifecycle_state().core_running


func get_last_status_read() -> HookStatusReadResult:
	if _lifecycle_poller == null:
		return null
	return _lifecycle_poller.get_last().status


func get_status_snapshot() -> HookStatusSnapshot:
	var result := get_last_status_read()
	if result == null or result.code != HookStatusReadResult.Code.OK:
		return null
	return result.snapshot


func request_manual_preserve() -> bool:
	if _paths == null or not _paths.is_valid():
		push_warning("Mnemonic: cannot write flag; DataRoot unavailable.")
		return false
	return HookFlagWriter.write_flag_current(_paths)


func request_pause_capture() -> bool:
	if _paths == null or not _paths.is_valid():
		push_warning("Mnemonic: cannot write pause command; DataRoot unavailable.")
		return false
	return HookCaptureCommandWriter.write_pause(_paths)


func request_resume_capture() -> bool:
	if _paths == null or not _paths.is_valid():
		push_warning("Mnemonic: cannot write resume command; DataRoot unavailable.")
		return false
	return HookCaptureCommandWriter.write_resume(_paths)


func request_rebuild_clips_index() -> bool:
	if _paths == null or not _paths.is_valid():
		push_warning("Mnemonic: cannot write rebuild clips index command; DataRoot unavailable.")
		return false
	return HookCaptureCommandWriter.write_rebuild_clips_index(_paths)


func launch_core() -> Dictionary:
	if _paths == null or not _paths.is_valid():
		push_warning("Mnemonic: cannot launch Core; DataRoot unavailable.")
		return {
			"ok": false,
			"message": (
				"Launch failed — set mnemonic/core_windows_exe in Editor Settings "
				+ "or build mnemonic-core"
			),
		}
	return HookCoreLauncherGd.try_launch(get_editor_interface())


func start_recording_session() -> Dictionary:
	var result: Dictionary = HookCaptureSession.start_recording(self)
	HookCoreRunningCache.invalidate()
	return result


func stop_recording_session() -> Dictionary:
	var result: Dictionary = HookCaptureSession.stop_recording(self)
	HookCoreRunningCache.invalidate()
	return result
