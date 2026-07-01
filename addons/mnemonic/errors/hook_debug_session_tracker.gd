class_name HookDebugSessionTracker
extends RefCounted

const SessionEventJsonGd = preload("res://addons/mnemonic/events/session_event_json.gd")
const JsonlEventAppenderGd = preload("res://addons/mnemonic/events/jsonl_event_appender.gd")

var _events_path: String = ""
var _open: bool = false
var _start_unix: float = 0.0


func _init(events_path: String) -> void:
	_events_path = events_path


func on_debug_session_started(is_playtest_active: bool) -> void:
	if not is_playtest_active or _open:
		return
	_open = true
	_start_unix = float(Time.get_unix_time_from_system())
	JsonlEventAppenderGd.append(
		_events_path,
		SessionEventJsonGd.create_debug_session_start(_start_unix)
	)


func on_playtest_stopped() -> void:
	_close_if_open()


func detach() -> void:
	_close_if_open()


func _close_if_open() -> void:
	if not _open:
		return
	var t_stop := float(Time.get_unix_time_from_system())
	JsonlEventAppenderGd.append(
		_events_path,
		SessionEventJsonGd.create_debug_session_stop(t_stop, t_stop - _start_unix)
	)
	_open = false
	_start_unix = 0.0
