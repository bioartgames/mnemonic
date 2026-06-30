class_name HookStatusSnapshot
extends RefCounted

var contract_version: int = 1
var recording: bool = false
var state: String = "idle"
var ffmpeg_ok: bool = false
var current_segment_index: int = 0
var pending_manual_preserve_segment_index: int = -1
var capture_prefix: String = ""
var data_root: String = ""
var error: String = ""
var last_segment_score: int = -1
var last_segment_preserved: bool = false
var has_last_segment_preserved: bool = false
var preserve_threshold: int = -1
var notable_score_min: int = -1
var highlight_score_min: int = -1
var last_segment_breakdown: Array = []
var live_clip_preview: Dictionary = {}


static func from_dict(d: Dictionary) -> HookStatusSnapshot:
	if d.is_empty():
		return null
	if not d.has("contract_version"):
		return null
	var version_raw = d.get("contract_version")
	if typeof(version_raw) not in [TYPE_INT, TYPE_FLOAT]:
		return null

	var snap := HookStatusSnapshot.new()
	snap.contract_version = int(version_raw)
	snap.recording = bool(d.get("recording", false))
	snap.state = str(d.get("state", "idle"))
	snap.ffmpeg_ok = bool(d.get("ffmpeg_ok", false))
	snap.current_segment_index = int(d.get("current_segment_index", 0))
	if d.has("pending_manual_preserve_segment_index"):
		snap.pending_manual_preserve_segment_index = int(
			d.get("pending_manual_preserve_segment_index", -1)
		)
	snap.capture_prefix = str(d.get("capture_prefix", ""))
	snap.data_root = str(d.get("data_root", ""))
	snap.error = str(d.get("error", ""))
	if d.has("last_segment_score"):
		snap.last_segment_score = int(d.get("last_segment_score", -1))
	if d.has("last_segment_preserved"):
		snap.has_last_segment_preserved = true
		snap.last_segment_preserved = bool(d.get("last_segment_preserved", false))
	if d.has("preserve_threshold"):
		snap.preserve_threshold = int(d.get("preserve_threshold", -1))
	if d.has("highlight_score_min"):
		snap.highlight_score_min = int(d.get("highlight_score_min", -1))
	if d.has("notable_score_min"):
		snap.notable_score_min = int(d.get("notable_score_min", -1))
	var breakdown_raw = d.get("last_segment_breakdown", [])
	if typeof(breakdown_raw) == TYPE_ARRAY:
		snap.last_segment_breakdown = breakdown_raw
	var live_preview_raw = d.get("live_clip_preview", {})
	if typeof(live_preview_raw) == TYPE_DICTIONARY:
		snap.live_clip_preview = live_preview_raw
	return snap


func is_recording() -> bool:
	return recording
