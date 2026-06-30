class_name HookEditorFocusTracker
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const SessionEventJsonGd = preload("res://addons/mnemonic_hook/events/session_event_json.gd")
const JsonlEventAppenderGd = preload("res://addons/mnemonic_hook/events/jsonl_event_appender.gd")
const HookSceneTrackerGd = preload("res://addons/mnemonic_hook/trackers/hook_scene_tracker.gd")

const VALID_FOCUS_BUCKETS := ["script", "2d", "3d", "inspector", "other"]

## Godot EditorPlugin main-screen indices when an API returns int (tests / future).
const MAIN_SCREEN_3D := 0
const MAIN_SCREEN_2D := 1
const MAIN_SCREEN_SCRIPT := 2
const MAIN_SCREEN_GAME := 3

var _events_path: String = ""
var _last_focus: String = ""
var _last_emit_unix: float = -1.0
var _on_focus_bucket_changed: Callable = Callable()


func _init(events_path: String) -> void:
	_events_path = events_path


func set_on_focus_bucket_changed(callback: Callable) -> void:
	_on_focus_bucket_changed = callback


static func focus_bucket_from_screen_name(screen_name: String) -> String:
	match screen_name.strip_edges().to_lower():
		"script":
			return "script"
		"2d":
			return "2d"
		"3d":
			return "3d"
		"game", "assetlib":
			return "other"
		"asset lib":
			return "other"
		_:
			return "other"


static func focus_bucket_from_screen_index(main_screen: int) -> String:
	match main_screen:
		MAIN_SCREEN_SCRIPT:
			return "script"
		MAIN_SCREEN_2D:
			return "2d"
		MAIN_SCREEN_3D:
			return "3d"
		MAIN_SCREEN_GAME:
			return "other"
		_:
			return "other"


static func is_valid_focus_bucket(focus: String) -> bool:
	return focus in VALID_FOCUS_BUCKETS


func on_main_screen_name_changed(screen_name: String) -> void:
	var bucket := focus_bucket_from_screen_name(screen_name)
	_emit_focus_if_changed(bucket)


func _emit_focus_if_changed(bucket: String) -> void:
	if not is_valid_focus_bucket(bucket):
		return
	if bucket == _last_focus:
		return
	var now := float(Time.get_unix_time_from_system())
	if not HookSceneTrackerGd.dedupe_allows(
		_last_focus,
		_last_emit_unix,
		bucket,
		now,
		Mc.EDITOR_FOCUS_MIN_EMIT_INTERVAL_SECONDS,
	):
		return
	_last_focus = bucket
	_last_emit_unix = now
	JsonlEventAppenderGd.append(
		_events_path,
		SessionEventJsonGd.create_editor_focus_changed(now, bucket)
	)
	if _on_focus_bucket_changed.is_valid():
		_on_focus_bucket_changed.call(bucket)
