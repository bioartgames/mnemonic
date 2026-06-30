class_name HookSceneTracker
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const SessionEventJsonGd = preload("res://addons/mnemonic_hook/events/session_event_json.gd")
const JsonlEventAppenderGd = preload("res://addons/mnemonic_hook/events/jsonl_event_appender.gd")

var _events_path: String = ""
var _last_save_path: String = ""
var _last_save_unix: float = -1.0
var _last_transition_scene: String = ""
var _last_transition_unix: float = -1.0


func _init(events_path: String) -> void:
	_events_path = events_path


static func dedupe_allows(
	last_key: String,
	last_unix: float,
	key: String,
	now: float,
	dedupe_sec: float = -1.0,
) -> bool:
	if key.is_empty():
		return false
	if last_key != key:
		return true
	if last_unix < 0.0:
		return true
	var window := dedupe_sec if dedupe_sec > 0.0 else Mc.SCENE_EVENT_DEDUPE_SECONDS
	return now - last_unix >= window


func on_scene_saved(filepath: String) -> void:
	var path := filepath.strip_edges()
	if path.is_empty():
		return
	var now := float(Time.get_unix_time_from_system())
	if not dedupe_allows(_last_save_path, _last_save_unix, path, now):
		return
	_last_save_path = path
	_last_save_unix = now
	JsonlEventAppenderGd.append(
		_events_path,
		SessionEventJsonGd.create_scene_save(now, path)
	)


func on_scene_changed(scene_root: Node) -> void:
	if scene_root == null:
		return
	var to_scene := scene_root.scene_file_path
	if to_scene.is_empty():
		return
	var now := float(Time.get_unix_time_from_system())
	if not dedupe_allows(_last_transition_scene, _last_transition_unix, to_scene, now):
		return
	_last_transition_scene = to_scene
	_last_transition_unix = now
	JsonlEventAppenderGd.append(
		_events_path,
		SessionEventJsonGd.create_scene_transition(now, to_scene)
	)
