class_name HookPlaytestTracker
extends RefCounted

const SessionEventJsonGd = preload("res://addons/mnemonic/events/session_event_json.gd")
const JsonlEventAppenderGd = preload("res://addons/mnemonic/events/jsonl_event_appender.gd")
const HookEditorSceneSnapshotGd = preload(
	"res://addons/mnemonic/ipc/hook_editor_scene_snapshot.gd"
)

const ERROR_CAPTURE_GRACE_SEC := 5.0

var _events_path: String = ""
var _was_playing: bool = false
var _play_start_unix: float = 0.0
var _last_stop_unix: float = 0.0
var _on_playtest_stopped: Callable = Callable()


func _init(events_path: String) -> void:
	_events_path = events_path


func set_on_playtest_stopped(callback: Callable) -> void:
	_on_playtest_stopped = callback


func poll(editor_interface: EditorInterface) -> void:
	var playing := editor_interface.is_playing_scene()
	if playing and not _was_playing:
		_play_start_unix = float(Time.get_unix_time_from_system())
		var scene_path := HookEditorSceneSnapshotGd.playing_scene_path(editor_interface)
		JsonlEventAppenderGd.append(
			_events_path,
			SessionEventJsonGd.create_playtest_start(_play_start_unix, scene_path)
		)
	elif not playing and _was_playing:
		var t_stop := float(Time.get_unix_time_from_system())
		var duration_sec := t_stop - _play_start_unix
		JsonlEventAppenderGd.append(
			_events_path,
			SessionEventJsonGd.create_playtest_stop(t_stop, duration_sec)
		)
		_last_stop_unix = t_stop
		_play_start_unix = 0.0
		if _on_playtest_stopped.is_valid():
			_on_playtest_stopped.call()
	_was_playing = playing


func is_within_error_capture_window(grace_sec: float = ERROR_CAPTURE_GRACE_SEC) -> bool:
	if _was_playing:
		return true
	if _last_stop_unix <= 0.0:
		return false
	return float(Time.get_unix_time_from_system()) - _last_stop_unix <= grace_sec
