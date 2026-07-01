class_name HookClipDisplay
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookSettingsIoGd = preload("res://addons/mnemonic/ipc/hook_settings_io.gd")
const HookSignificanceTierGd = preload("res://addons/mnemonic/clips/hook_significance_tier.gd")
const HookTimeDisplayGd = preload("res://addons/mnemonic/clips/hook_time_display.gd")

const _CONTEXT_TAGS: Array[String] = ["playtest", "save", "commit", "transition"]


static func format_list_primary(row: Dictionary) -> String:
	var parts: PackedStringArray = PackedStringArray()
	var scenes := format_scene_basenames(row.get("scenes_active", []), 2)
	parts.append(scenes)
	var duration := int(row.get("duration_seconds", 0))
	if duration > 0:
		parts.append(format_duration(duration))
	var context := format_list_context_tag(row.get("tags", []))
	if not context.is_empty():
		parts.append(context)
	var joined := " · ".join(parts)
	return _truncate(joined, Mc.CLIP_DISPLAY_TITLE_MAX_LEN)


static func format_scene_basenames(scenes: Variant, max_count: int = 2) -> String:
	if typeof(scenes) != TYPE_ARRAY:
		return "No scene"
	var names: PackedStringArray = PackedStringArray()
	var seen: Dictionary = {}
	for path in scenes:
		var base := _basename_from_scene_path(str(path))
		if base.is_empty():
			continue
		var key := base.to_lower()
		if seen.has(key):
			continue
		seen[key] = true
		names.append(base)
		if names.size() >= max_count:
			break
	if names.is_empty():
		return "No scene"
	return ", ".join(names)


static func format_duration(seconds: int) -> String:
	if seconds < 60:
		return "%ds" % seconds
	if seconds < 3600:
		return "%dm" % int(seconds / 60)
	var hours := int(seconds / 3600)
	var mins := int((seconds % 3600) / 60)
	return "%dh%dm" % [hours, mins]


static func format_list_context_tag(tags: Variant) -> String:
	if typeof(tags) != TYPE_ARRAY:
		return ""
	for wanted in _CONTEXT_TAGS:
		for t in tags:
			if str(t) == wanted:
				return wanted
	return ""


static func format_primary_label(dic: Dictionary, folder_name: String) -> String:
	var subject := str(dic.get("commit_subject", "")).strip_edges()
	if not subject.is_empty():
		return _truncate(subject, Mc.CLIP_DISPLAY_TITLE_MAX_LEN)

	var branch := str(dic.get("git_branch", "")).strip_edges()
	if not branch.is_empty():
		var created_at := int(dic.get("created_at", 0))
		var when := (
			HookTimeDisplayGd.format_local_datetime(created_at)
			if created_at > 0
			else ""
		)
		if when.is_empty():
			return branch
		return "%s · %s" % [branch, when]

	var id := str(dic.get("id", folder_name)).strip_edges()
	if id.is_empty():
		return folder_name
	return id


static func format_short_tooltip(row: Dictionary) -> String:
	var score := int(row.get("score", 0))
	var primary := format_list_primary(row)
	if not primary.is_empty():
		return _truncate("%s · score %d" % [primary, score], Mc.CLIP_DISPLAY_TITLE_MAX_LEN)
	var branch := str(row.get("git_branch", "")).strip_edges()
	if not branch.is_empty():
		return _truncate("score %d · %s" % [score, branch], Mc.CLIP_DISPLAY_TITLE_MAX_LEN)
	var tags: Variant = row.get("tags", [])
	if typeof(tags) == TYPE_ARRAY:
		for t in tags:
			var tag := str(t).strip_edges()
			if not tag.is_empty():
				return _truncate("score %d · %s" % [score, tag], Mc.CLIP_DISPLAY_TITLE_MAX_LEN)
	return _truncate("score %d" % score, Mc.CLIP_DISPLAY_TITLE_MAX_LEN)


static func resolve_preserve_threshold(
	snap_threshold: int,
	paths: MnemonicDataRootPaths,
) -> int:
	if snap_threshold >= 0:
		return snap_threshold
	if paths != null and paths.is_valid():
		return HookSettingsIoGd.read_int(
			paths,
			Mc.SETTINGS_KEY_PRESERVE_THRESHOLD,
			Mc.SETTINGS_DEFAULT_PRESERVE_THRESHOLD,
		)
	return -1


static func resolve_highlight_score_min(
	snap_highlight: int,
	paths: MnemonicDataRootPaths,
) -> int:
	if snap_highlight >= 0:
		return snap_highlight
	if paths != null and paths.is_valid():
		return HookSettingsIoGd.read_int(
			paths,
			Mc.SETTINGS_KEY_HIGHLIGHT_SCORE_MIN,
			Mc.SETTINGS_DEFAULT_HIGHLIGHT_SCORE_MIN,
		)
	return -1


static func resolve_notable_score_min(
	snap_notable: int,
	paths: MnemonicDataRootPaths,
	preserve_threshold: int = -1,
) -> int:
	if snap_notable >= 0:
		return snap_notable
	if paths != null and paths.is_valid():
		var stored := HookSettingsIoGd.read_int(
			paths,
			Mc.SETTINGS_KEY_NOTABLE_SCORE_MIN,
			Mc.SETTINGS_DEFAULT_NOTABLE_SCORE_MIN,
		)
		if stored >= 0:
			return stored
	if preserve_threshold >= 0:
		return preserve_threshold
	return -1


## Devlog-focused hover text shared by saved clips and the active REC row.
static func format_devlog_tooltip(row: Dictionary, opts: Dictionary = {}) -> String:
	var lines: PackedStringArray = PackedStringArray()
	if bool(opts.get("manual_preserve_queued", false)):
		lines.append(Mc.TOOLTIP_LIVE_MANUAL_PRESERVE_QUEUED)
	elif bool(opts.get("save_in_progress", false)):
		lines.append(Mc.TOOLTIP_LIVE_MANUAL_PRESERVE_QUEUED)

	var segment_id := str(row.get("id", "")).strip_edges()
	if not segment_id.is_empty():
		lines.append("Segment: %s" % segment_id)

	var score := int(row.get("score", 0))
	var threshold := int(opts.get("preserve_threshold", -1))
	var highlight_min := int(opts.get("highlight_score_min", -1))
	var notable_min := int(opts.get("notable_score_min", -1))
	var is_live := bool(opts.get("is_live", false))
	lines.append(
		HookSignificanceTierGd.format_score_tooltip_line(
			score, threshold, highlight_min, notable_min, is_live
		)
	)

	append_git_tooltip_lines(lines, row)

	var scenes: Variant = row.get("scenes_active", [])
	if typeof(scenes) == TYPE_ARRAY and not scenes.is_empty():
		lines.append("Scenes: %s" % format_scene_basenames(scenes, 64))

	var signal_summary := str(opts.get("signal_summary", "")).strip_edges()
	if not signal_summary.is_empty():
		lines.append("Signals: %s" % signal_summary)
	else:
		var tags: Variant = row.get("tags", [])
		if typeof(tags) == TYPE_ARRAY and not tags.is_empty():
			var tag_parts: PackedStringArray = PackedStringArray()
			for t in tags:
				tag_parts.append(str(t))
			lines.append("Tags: %s" % ", ".join(tag_parts))

	if not bool(opts.get("is_live", false)):
		var topics: Variant = row.get("ai_topics", [])
		if typeof(topics) == TYPE_ARRAY and not topics.is_empty():
			var topic_parts: PackedStringArray = PackedStringArray()
			for topic in topics:
				topic_parts.append(str(topic))
			lines.append("Topics: %s" % ", ".join(topic_parts))
		var created_at := int(row.get("created_at", 0))
		if created_at > 0:
			lines.append(
				"Recorded: %s" % HookTimeDisplayGd.format_local_datetime(created_at)
			)

	if lines.is_empty():
		return ""
	return "\n".join(lines)


static func format_tooltip(row: Dictionary, preserve_threshold: int = -1) -> String:
	return format_devlog_tooltip(row, {"preserve_threshold": preserve_threshold})


static func append_git_tooltip_lines(lines: PackedStringArray, row: Dictionary) -> void:
	var branch := str(row.get("git_branch", "")).strip_edges()
	var subject := str(row.get("commit_subject", "")).strip_edges()
	var commit := _short_git_commit(str(row.get("git_commit", "")))
	if not branch.is_empty():
		lines.append("Branch: %s" % _truncate_tooltip(branch, Mc.TOOLTIP_GIT_BRANCH_MAX_LEN))
	if not subject.is_empty():
		lines.append("Subject: %s" % _truncate_tooltip(subject, Mc.TOOLTIP_GIT_SUBJECT_MAX_LEN))
	if not commit.is_empty():
		lines.append("Commit: %s" % commit)


static func _short_git_commit(commit: String) -> String:
	var trimmed := commit.strip_edges()
	if trimmed.is_empty():
		return ""
	if trimmed.length() <= 7:
		return trimmed
	return trimmed.substr(0, 7)


static func _truncate_tooltip(value: String, max_len: int) -> String:
	if value.length() <= max_len:
		return value
	if max_len <= 1:
		return value.substr(0, max_len)
	return value.substr(0, max_len - 1) + "…"


static func truncate_for_display(value: String, max_len: int = Mc.CLIP_DISPLAY_TITLE_MAX_LEN) -> String:
	return _truncate(value, max_len)


static func _basename_from_scene_path(path: String) -> String:
	var trimmed := path.strip_edges()
	if trimmed.is_empty():
		return ""
	var slash := trimmed.rfind("/")
	var file := trimmed.substr(slash + 1) if slash >= 0 else trimmed
	var dot := file.rfind(".")
	if dot > 0:
		return file.substr(0, dot)
	return file


static func _truncate(value: String, max_len: int) -> String:
	if value.length() <= max_len:
		return value
	if max_len <= 1:
		return value.substr(0, max_len)
	return value.substr(0, max_len - 1) + "…"
