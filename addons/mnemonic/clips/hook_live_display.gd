class_name HookLiveDisplay
extends RefCounted

const HookClipDisplayGd = preload("res://addons/mnemonic/clips/hook_clip_display.gd")


static func format_mmss(seconds: int) -> String:
	var clamped := maxi(0, seconds)
	return "%d:%02d" % [int(clamped / 60), clamped % 60]


static func segment_total_seconds(
	t_close_unix: float,
	t_open_unix: float,
	duration_seconds: int = 0,
) -> int:
	if t_close_unix > 0.0 and t_open_unix > 0.0 and t_close_unix > t_open_unix:
		return maxi(1, int(t_close_unix) - int(t_open_unix))
	if duration_seconds > 0:
		return duration_seconds
	return 0


static func format_countdown_timer(
	t_close_unix: float,
	t_open_unix: float = 0.0,
	duration_seconds: int = 0,
	now_unix: float = -1.0,
	close_phase: String = "",
) -> String:
	if t_close_unix <= 0.0:
		return "Recording"
	if now_unix < 0.0:
		now_unix = float(Time.get_unix_time_from_system())
	var remaining := maxi(0, int(t_close_unix) - int(now_unix))
	if remaining <= 0:
		match close_phase.strip_edges():
			"saving":
				return "Saving…"
			"discarding":
				return "Discarding…"
			_:
				return "Closing…"
	var total := segment_total_seconds(t_close_unix, t_open_unix, duration_seconds)
	if total <= 0:
		return format_mmss(remaining)
	return "%s / %s" % [format_mmss(remaining), format_mmss(total)]


## Close-phase label when the segment timer hits zero (not the previous segment in status.json).
static func close_phase_at_segment_end(
	score_preview: int,
	preserve_threshold: int,
	pending_manual_preserve_segment: int = -1,
	live_segment_index: int = -1,
) -> String:
	if live_segment_index >= 0 and pending_manual_preserve_segment == live_segment_index:
		return "saving"
	if preserve_threshold < 0:
		return ""
	if score_preview >= preserve_threshold:
		return "saving"
	return "discarding"


static func format_countdown_title(
	t_close_unix: float,
	t_open_unix: float = 0.0,
	duration_seconds: int = 0,
	now_unix: float = -1.0,
	close_phase: String = "",
) -> String:
	return format_countdown_timer(
		t_close_unix, t_open_unix, duration_seconds, now_unix, close_phase
	)


static func format_live_subline(row: Dictionary, preserve_threshold: int = -1) -> String:
	var parts: PackedStringArray = PackedStringArray()
	var score := format_live_score(row, preserve_threshold)
	if not score.is_empty():
		parts.append(score)
	var branch := format_live_branch(row)
	if not branch.is_empty():
		parts.append(branch)
	if parts.is_empty():
		return ""
	return " · ".join(parts)


static func format_live_score(row: Dictionary, preserve_threshold: int = -1) -> String:
	var score := int(row.get("score", 0))
	if preserve_threshold >= 0:
		return "%d / %d" % [score, preserve_threshold]
	return str(score)


static func format_live_branch(row: Dictionary) -> String:
	return str(row.get("git_branch", "")).strip_edges()


static func format_live_tooltip(segment_index: int, score_preview: int) -> String:
	if segment_index >= 0:
		return "Recording · segment %d · score %d" % [segment_index, score_preview]
	return "Recording · score %d" % score_preview
