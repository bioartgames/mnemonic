class_name MnemonicDataRootPaths
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")

var root: String = ""


func _init(override_root: String = "") -> void:
	if not override_root.is_empty():
		root = override_root
	else:
		root = _resolve_default_root()


func is_supported_platform() -> bool:
	return OS.get_name() == "Windows"


func is_valid() -> bool:
	return not root.is_empty()


func get_root() -> String:
	return root


func get_scratch_dir() -> String:
	return _join("scratch")


func get_clips_dir() -> String:
	return _join("clips")


func get_events_dir() -> String:
	return _join("events")


func get_session_events_file() -> String:
	return _join("events", "session_events.jsonl")


func get_control_dir() -> String:
	return _join("control")


func get_status_file() -> String:
	return _join("control", "status.json")


func get_segment_history_file() -> String:
	return get_control_dir().path_join(Mc.SEGMENT_HISTORY_FILE_NAME)


func get_editor_scene_file() -> String:
	return get_control_dir().path_join(Mc.EDITOR_SCENE_FILE_NAME)


func get_clips_index_file() -> String:
	return get_control_dir().path_join(Mc.CLIPS_INDEX_FILE_NAME)


func get_suggested_groups_file() -> String:
	return get_control_dir().path_join(Mc.SUGGESTED_GROUPS_FILE_NAME)


func get_commands_dir() -> String:
	return _join("control", "commands")


func get_rebuild_clips_index_file() -> String:
	return get_commands_dir().path_join(Mc.REBUILD_CLIPS_INDEX_FILE_NAME)


func get_flag_current_file() -> String:
	return get_commands_dir().path_join("flag_current.json")


func get_pause_capture_file() -> String:
	return get_commands_dir().path_join("pause_capture.json")


func get_resume_capture_file() -> String:
	return get_commands_dir().path_join("resume_capture.json")


func get_exit_core_file() -> String:
	return get_commands_dir().path_join(Mc.EXIT_CORE_COMMAND_FILE_NAME)


func get_settings_file() -> String:
	return _join("settings.json")


func get_logs_dir() -> String:
	return _join("logs")


func _resolve_default_root() -> String:
	var local_app_data := OS.get_environment("LOCALAPPDATA").strip_edges()
	if local_app_data.is_empty():
		return ""
	return local_app_data.path_join(Mc.DATA_ROOT_FOLDER_NAME)


func _join(part: String, part2: String = "") -> String:
	var path := root.path_join(part)
	if part2.is_empty():
		return path
	return path.path_join(part2)
