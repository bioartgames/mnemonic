class_name HookEditorFocusSessionTracker
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const SessionEventJsonGd = preload("res://addons/mnemonic/events/session_event_json.gd")
const JsonlEventAppenderGd = preload("res://addons/mnemonic/events/jsonl_event_appender.gd")
const HookEditorFocusTrackerGd = preload(
	"res://addons/mnemonic/editor/hook_editor_focus_tracker.gd"
)

var _events_path: String = ""
var _is_playtest_active: Callable
var _current_bucket: String = ""
var _accumulated_sec: float = 0.0
var _last_emit_unix: float = -1.0


func _init(events_path: String, is_playtest_active: Callable) -> void:
	_events_path = events_path
	_is_playtest_active = is_playtest_active


func on_focus_bucket_changed(bucket: String) -> void:
	if not HookEditorFocusTrackerGd.is_valid_focus_bucket(bucket):
		return
	_current_bucket = bucket
	_accumulated_sec = 0.0


func tick() -> void:
	if _is_playtest_active.is_valid() and _is_playtest_active.call():
		_accumulated_sec = 0.0
		return
	if _current_bucket.is_empty():
		return
	_accumulated_sec += Mc.FOCUS_SESSION_TICK_SEC
	if _accumulated_sec < Mc.EDITOR_FOCUSED_SESSION_MIN_SECONDS:
		return
	var now := float(Time.get_unix_time_from_system())
	if _last_emit_unix >= 0.0:
		if now - _last_emit_unix < Mc.EDITOR_FOCUSED_SESSION_EMIT_COOLDOWN_SECONDS:
			return
	JsonlEventAppenderGd.append(
		_events_path,
		SessionEventJsonGd.create_editor_focused_session(
			now, _current_bucket, _accumulated_sec
		)
	)
	_last_emit_unix = now
	_accumulated_sec = 0.0
