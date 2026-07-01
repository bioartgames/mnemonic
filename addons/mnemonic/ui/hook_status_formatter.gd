class_name HookStatusFormatter
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")


static func format_lifecycle(state: HookLifecycleState) -> String:
	if state == null:
		return "Status unavailable"

	if not state.core_running:
		return (
			"Mnemonic is not running. Click Start recording below, "
			+ "or start Mnemonic manually."
		)

	var result := state.status
	if result == null:
		return "Status unavailable"

	match result.code:
		HookStatusReadResult.Code.MISSING_FILE:
			return "Mnemonic Core is running. Waiting for status…"
		HookStatusReadResult.Code.PARSE_ERROR:
			return "Mnemonic Core is running. Status error: %s" % result.message
		HookStatusReadResult.Code.CONTRACT_MISMATCH:
			return "Mnemonic Core is running. Status contract mismatch (%s)." % result.message
		HookStatusReadResult.Code.OK:
			if result.snapshot == null:
				return "Status unavailable"
			return _format_snapshot(result.snapshot)
		_:
			return "Status unavailable"


static func should_show_lifecycle_label(state: HookLifecycleState) -> bool:
	if state == null:
		return true

	if not state.core_running:
		return true

	var result := state.status
	if result == null:
		return true
	if result.code != HookStatusReadResult.Code.OK:
		return true
	if result.snapshot == null:
		return true

	var snapshot := result.snapshot
	var snap_state := snapshot.state.strip_edges().to_lower()

	if snap_state == Mc.CAPTURE_STATE_RECORDING:
		return true
	if snap_state == Mc.CAPTURE_STATE_PAUSED:
		return true
	if snap_state == Mc.CAPTURE_STATE_ERROR:
		return true
	if snap_state == Mc.CAPTURE_STATE_IDLE:
		if not snapshot.ffmpeg_ok:
			return true
		if not snapshot.error.is_empty():
			return true
		return false
	if snap_state.is_empty():
		return false
	return true


static func _format_snapshot(snapshot: HookStatusSnapshot) -> String:
	var state := snapshot.state.strip_edges().to_lower()
	var text: String

	match state:
		Mc.CAPTURE_STATE_RECORDING:
			text = (
				"Recording · segment %d" % snapshot.current_segment_index
				if snapshot.recording
				else "Recording"
			)
		Mc.CAPTURE_STATE_PAUSED:
			text = Mc.UI_STATUS_RECORDING_STOPPED
		Mc.CAPTURE_STATE_IDLE:
			text = "Idle"
			if not snapshot.ffmpeg_ok and not snapshot.error.is_empty():
				text += " (FFmpeg unavailable)"
		Mc.CAPTURE_STATE_ERROR:
			text = (
				"Error: %s" % _truncate(snapshot.error, Mc.STATUS_ERROR_MAX_LEN)
				if not snapshot.error.is_empty()
				else "Error"
			)
		_:
			text = "State: unknown" if state.is_empty() else "State: %s" % snapshot.state

	return text


static func _truncate(value: String, max_length: int) -> String:
	if value.length() <= max_length:
		return value
	if max_length <= 3:
		return value.substr(0, max_length)
	return value.substr(0, max_length - 3) + "..."
