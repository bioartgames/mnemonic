class_name HookResourceTracker
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const SessionEventJsonGd = preload("res://addons/mnemonic_hook/events/session_event_json.gd")
const JsonlEventAppenderGd = preload("res://addons/mnemonic_hook/events/jsonl_event_appender.gd")
const HookSceneTrackerGd = preload("res://addons/mnemonic_hook/trackers/hook_scene_tracker.gd")

var _events_path: String = ""
var _last_save_path: String = ""
var _last_save_unix: float = -1.0


func _init(events_path: String) -> void:
	_events_path = events_path


static func is_scene_path(path: String) -> bool:
	return path.to_lower().ends_with(".tscn")


static func normalize_resource_path(filepath: String) -> String:
	var path := filepath.strip_edges().replace("\\", "/")
	if path.is_empty():
		return ""
	if path.begins_with("res://"):
		return path
	if path.begins_with("uid://"):
		return ""
	return "res://%s" % path.lstrip("/")


func on_script_saved(filepath: String) -> void:
	var path := normalize_resource_path(filepath)
	if path.is_empty() or is_scene_path(path):
		return
	var now := float(Time.get_unix_time_from_system())
	if not HookSceneTrackerGd.dedupe_allows(
		_last_save_path, _last_save_unix, path, now, Mc.RESOURCE_EVENT_DEDUPE_SECONDS
	):
		return
	_last_save_path = path
	_last_save_unix = now
	JsonlEventAppenderGd.append(
		_events_path,
		SessionEventJsonGd.create_resource_saved(now, path)
	)
	if _is_script_path(path):
		JsonlEventAppenderGd.append(
			_events_path,
			SessionEventJsonGd.create_script_save(now, path)
		)


static func _is_script_path(path: String) -> bool:
	var lower := path.to_lower()
	for ext in Mc.SCRIPT_SAVE_EXTENSIONS:
		if lower.ends_with(ext):
			return true
	return false
