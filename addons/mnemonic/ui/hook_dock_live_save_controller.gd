class_name HookDockLiveSaveController
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookLiveSavePollGd = preload("res://addons/mnemonic/ui/hook_live_save_poll.gd")

var pending: bool = false
var segment_index: int = -1
var phase: int = HookLiveSavePollGd.PHASE_AWAIT_ACK
var deadline_ms: int = 0


static func live_score_meets_preserve_threshold(
	plugin,
	preserve_threshold: int,
) -> bool:
	if plugin == null:
		return false
	var snap: HookStatusSnapshot = plugin.get_status_snapshot()
	if snap == null:
		return false
	var threshold := preserve_threshold
	if threshold < 0:
		threshold = snap.preserve_threshold
	if threshold < 0:
		return false
	var live: Dictionary = snap.live_clip_preview
	if typeof(live) != TYPE_DICTIONARY:
		return false
	return int(live.get("score_preview", 0)) >= threshold


func can_save(plugin, preserve_threshold: int = -1) -> bool:
	if plugin == null or not plugin.is_core_running() or pending:
		return false
	var snap: HookStatusSnapshot = plugin.get_status_snapshot()
	if snap != null and snap.pending_manual_preserve_segment_index >= 0:
		return false
	if live_score_meets_preserve_threshold(plugin, preserve_threshold):
		return false
	return true


func begin_save(plugin, on_started: Callable) -> bool:
	if pending:
		return false
	if plugin == null or not plugin.is_core_running():
		return false
	var snap: HookStatusSnapshot = plugin.get_status_snapshot()
	var save_segment_index := -1
	if snap != null:
		var live: Dictionary = snap.live_clip_preview
		if typeof(live) == TYPE_DICTIONARY:
			save_segment_index = int(live.get("segment_index", snap.current_segment_index))
		else:
			save_segment_index = snap.current_segment_index
	var ok: bool = plugin.request_manual_preserve()
	if not ok:
		reset()
		return false
	pending = true
	segment_index = save_segment_index
	phase = HookLiveSavePollGd.PHASE_AWAIT_ACK
	deadline_ms = Time.get_ticks_msec() + int(Mc.LIVE_SAVE_FLAG_TIMEOUT_SEC * 1000.0)
	if on_started.is_valid():
		on_started.call()
	return true


func poll_tick(plugin) -> Dictionary:
	var result := {
		"outcome": HookLiveSavePollGd.Outcome.PENDING,
		"phase_advanced": false,
	}
	if not pending or plugin == null:
		result["outcome"] = HookLiveSavePollGd.Outcome.FAIL_PATHS
		return result
	var snap: HookStatusSnapshot = plugin.get_status_snapshot()
	var outcome := HookLiveSavePollGd.poll_session(
		snap, segment_index, deadline_ms, phase
	)
	result["outcome"] = outcome
	if outcome == HookLiveSavePollGd.Outcome.ACKNOWLEDGED:
		phase = HookLiveSavePollGd.PHASE_AWAIT_CLOSE
		result["phase_advanced"] = true
	return result


func reset() -> void:
	pending = false
	segment_index = -1
	phase = HookLiveSavePollGd.PHASE_AWAIT_ACK
	deadline_ms = 0
