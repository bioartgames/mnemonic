class_name HookRuntimeErrorTracker
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookRuntimeErrorParserGd = preload("res://addons/mnemonic_hook/errors/hook_runtime_error_parser.gd")
const SessionEventJsonGd = preload("res://addons/mnemonic_hook/events/session_event_json.gd")
const JsonlEventAppenderGd = preload("res://addons/mnemonic_hook/events/jsonl_event_appender.gd")

var _events_path: String = ""
var _emit_times: Array[float] = []


func _init(events_path: String) -> void:
	_events_path = events_path


func on_output_error(data: Array) -> void:
	var parsed := HookRuntimeErrorParserGd.parse_output_error(data)
	if parsed.is_empty():
		return

	var now := float(Time.get_unix_time_from_system())
	if not rate_limit_allows(now):
		return

	var scene := str(parsed.get("scene", ""))
	var line := int(parsed.get("line", -1))
	JsonlEventAppenderGd.append(
		_events_path,
		SessionEventJsonGd.create_runtime_error(
			now,
			str(parsed.get("message", "")),
			scene,
			line,
		)
	)


func rate_limit_allows(now: float) -> bool:
	var cutoff := now - Mc.RUNTIME_ERROR_RATE_WINDOW_SECONDS
	var kept: Array[float] = []
	for t in _emit_times:
		if t >= cutoff:
			kept.append(t)
	_emit_times = kept

	if _emit_times.size() >= Mc.RUNTIME_ERROR_RATE_LIMIT_PER_MINUTE:
		return false

	_emit_times.append(now)
	return true
