class_name HookClipGroupLabel
extends RefCounted

const HookClipDisplayGd = preload("res://addons/mnemonic_hook/clips/hook_clip_display.gd")
const HookCalendarLabelsGd = preload("res://addons/mnemonic_hook/clips/hook_calendar_labels.gd")


static func format_header(group: Dictionary, compact: bool) -> String:
	var rows: Array = group.get("rows", [])
	var count: int = rows.size()
	if count == 0:
		return "0 clips"

	var parts: PackedStringArray = PackedStringArray(["%d clips" % count])
	var reason := str(group.get("reason", ""))
	var reason_hint := _header_reason_hint(group)
	if not reason_hint.is_empty():
		parts.append(_truncate_hint(reason_hint, 40))
	if reason != "same_commit":
		var scene_hint := _dominant_scene_basename(rows)
		if not scene_hint.is_empty():
			parts.append(_truncate_hint(scene_hint, 20))
	parts.append(_date_span_from_rows(rows))
	return " · ".join(parts)


static func format_header_tooltip(group: Dictionary) -> String:
	var rows: Array = group.get("rows", [])
	var label := str(group.get("label", "")).strip_edges()
	var reason := str(group.get("reason", "")).strip_edges()
	var lines: PackedStringArray = PackedStringArray()
	if not label.is_empty():
		lines.append(label)
	lines.append("Clips: %d" % rows.size())
	if not reason.is_empty():
		lines.append("Grouping: %s" % _reason_tooltip(reason))
	var when := _date_span_from_rows(rows)
	if not when.is_empty():
		lines.append("When: %s" % when)
	var scenes_line := _scenes_tooltip_from_rows(rows)
	if not scenes_line.is_empty():
		lines.append("Scenes: %s" % scenes_line)
	var score_span := _score_span_from_rows(rows)
	if not score_span.is_empty():
		lines.append(score_span)
	_append_group_git_lines(lines, rows, reason)
	return "\n".join(lines)


static func _header_reason_hint(group: Dictionary) -> String:
	var reason := str(group.get("reason", ""))
	var label := str(group.get("label", ""))
	var rows: Array = group.get("rows", [])
	match reason:
		"same_commit":
			return _commit_subject_for_header(label, rows)
		"branch_session":
			var branch := _branch_from_label(label)
			return branch if not branch.is_empty() else "session"
		"playtest_block":
			return "playtest"
		"iteration_block":
			return "iteration"
		"error_debugging":
			return "error debug"
		"post_commit":
			return "post-commit"
		"singleton":
			return ""
		_:
			return ""


static func _commit_subject_for_header(group_label: String, rows: Array) -> String:
	var first := _first_row_dict(rows)
	var subject := str(first.get("commit_subject", "")).strip_edges()
	if not subject.is_empty():
		return subject
	var label := group_label.strip_edges()
	if not label.is_empty():
		if label.begins_with("Commit "):
			return label
		return label
	return _short_commit_hash_from_row(first)


static func _first_row_dict(rows: Array) -> Dictionary:
	if rows.is_empty() or typeof(rows[0]) != TYPE_DICTIONARY:
		return {}
	return rows[0]


static func _short_commit_hash_from_row(row: Dictionary) -> String:
	var commit := str(row.get("git_commit", "")).strip_edges()
	if commit.is_empty():
		return ""
	if commit.length() <= 8:
		return commit
	return commit.substr(0, 8)


static func _reason_tooltip(reason: String) -> String:
	match reason:
		"same_commit":
			return "Same git commit"
		"branch_session":
			return "Same branch within session gap"
		"playtest_block":
			return "Playtest clips close in time"
		"iteration_block":
			return "Save + playtest clips within 90 minutes"
		"error_debugging":
			return "Error-tagged clips same day"
		"post_commit":
			return "Commit after playtest window"
		"singleton":
			return "Single clip"
		_:
			return reason


static func _branch_from_label(label: String) -> String:
	var parts := label.split("·")
	if parts.is_empty():
		return ""
	return parts[0].strip_edges()


static func _date_span_from_rows(rows: Array) -> String:
	if rows.is_empty():
		return ""
	var min_ts := 0x7FFFFFFF
	var max_ts := 0
	for row in rows:
		if typeof(row) != TYPE_DICTIONARY:
			continue
		var ts := int(row.get("created_at", 0))
		if ts <= 0:
			continue
		min_ts = mini(min_ts, ts)
		max_ts = maxi(max_ts, ts)
	if max_ts <= 0:
		return ""
	var min_d := _utc_date_dict(min_ts)
	var max_d := _utc_date_dict(max_ts)
	if int(min_d.get("year", 0)) == int(max_d.get("year", 0)) and int(
		min_d.get("month", 0)
	) == int(max_d.get("month", 0)) and int(min_d.get("day", 0)) == int(max_d.get("day", 0)):
		return _format_short_date(min_d)
	if int(min_d.get("year", 0)) == int(max_d.get("year", 0)) and int(
		min_d.get("month", 0)
	) == int(max_d.get("month", 0)):
		return "%s–%d" % [_format_short_date(min_d, false), int(max_d.get("day", 0))]
	return "%s–%s" % [_format_short_date(min_d, false), _format_short_date(max_d, false)]


static func _dominant_scene_basename(rows: Array) -> String:
	var counts: Dictionary = {}
	for row in rows:
		if typeof(row) != TYPE_DICTIONARY:
			continue
		var scenes: Variant = row.get("scenes_active", [])
		if typeof(scenes) != TYPE_ARRAY:
			continue
		for path in scenes:
			var base := _basename_from_path(str(path))
			if base.is_empty():
				continue
			var key := base.to_lower()
			counts[key] = int(counts.get(key, 0)) + 1
	if counts.is_empty():
		return ""
	var best_key := ""
	var best_count := -1
	for key in counts.keys():
		var c := int(counts[key])
		if c > best_count or (c == best_count and (best_key.is_empty() or key < best_key)):
			best_count = c
			best_key = str(key)
	for row in rows:
		if typeof(row) != TYPE_DICTIONARY:
			continue
		var scenes: Variant = row.get("scenes_active", [])
		if typeof(scenes) != TYPE_ARRAY:
			continue
		for path in scenes:
			if _basename_from_path(str(path)).to_lower() == best_key:
				return _basename_from_path(str(path))
	return best_key


static func _basename_from_path(path: String) -> String:
	var trimmed := path.strip_edges()
	if trimmed.is_empty():
		return ""
	var slash := trimmed.rfind("/")
	var file := trimmed.substr(slash + 1) if slash >= 0 else trimmed
	var dot := file.rfind(".")
	if dot > 0:
		return file.substr(0, dot)
	return file


static func _utc_date_dict(unix_time: int) -> Dictionary:
	return Time.get_datetime_dict_from_unix_time(unix_time)


static func _format_short_date(d: Dictionary, include_day: bool = true) -> String:
	var month := int(d.get("month", 1))
	var name := HookCalendarLabelsGd.MONTH_ABBR[clampi(month - 1, 0, 11)]
	if include_day:
		return "%s %d" % [name, int(d.get("day", 0))]
	return name


static func _truncate_hint(text: String, max_len: int) -> String:
	if text.length() <= max_len:
		return text
	if max_len <= 1:
		return text.substr(0, max_len)
	return text.substr(0, max_len - 1) + "…"


static func _score_span_from_rows(rows: Array) -> String:
	var has_score := false
	var min_score := 0
	var max_score := 0
	for row in rows:
		if typeof(row) != TYPE_DICTIONARY or not row.has("score"):
			continue
		var score := int(row.get("score", 0))
		if not has_score:
			has_score = true
			min_score = score
			max_score = score
		else:
			min_score = mini(min_score, score)
			max_score = maxi(max_score, score)
	if not has_score:
		return ""
	if min_score == max_score:
		return "Score: %d" % min_score
	return "Score: %d–%d" % [min_score, max_score]


static func _scenes_tooltip_from_rows(rows: Array) -> String:
	var paths: Array = []
	var seen: Dictionary = {}
	for row in rows:
		if typeof(row) != TYPE_DICTIONARY:
			continue
		var scenes: Variant = row.get("scenes_active", [])
		if typeof(scenes) != TYPE_ARRAY:
			continue
		for path in scenes:
			var key := str(path)
			if key.is_empty() or seen.has(key):
				continue
			seen[key] = true
			paths.append(key)
	return HookClipDisplayGd.format_scene_basenames(paths, 64)


static func _append_group_git_lines(lines: PackedStringArray, rows: Array, reason: String) -> void:
	if reason != "same_commit" or rows.is_empty():
		return
	var first := _first_row_dict(rows)
	if first.is_empty():
		return
	var branch := str(first.get("git_branch", "")).strip_edges()
	var subject := str(first.get("commit_subject", "")).strip_edges()
	var commit := str(first.get("git_commit", "")).strip_edges()
	if not branch.is_empty():
		lines.append("Branch: %s" % branch)
	if not subject.is_empty():
		lines.append("Subject: %s" % subject)
	if not commit.is_empty():
		lines.append("Commit: %s" % commit)
