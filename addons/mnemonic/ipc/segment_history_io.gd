class_name SegmentHistoryIo
extends RefCounted

const HookSignificanceTierGd = preload("res://addons/mnemonic_hook/clips/hook_significance_tier.gd")
const HookTimeDisplayGd = preload("res://addons/mnemonic_hook/clips/hook_time_display.gd")

## Keep trim logic in sync with mnemonic-core SegmentHistoryStore.TrimToMax.

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookFileMutexGd = preload("res://addons/mnemonic_hook/ipc/hook_file_mutex.gd")


static func _parse_record_entries(text: String, as_lines: bool) -> Array:
	var out: Array = []
	for line in text.split("\n", false):
		var trimmed := line.strip_edges()
		if trimmed.is_empty():
			continue
		var parsed: Variant = JSON.parse_string(trimmed)
		if typeof(parsed) != TYPE_DICTIONARY:
			continue
		var dic: Dictionary = parsed
		if int(dic.get("contract_version", 0)) != Mc.SEGMENT_HISTORY_CONTRACT_VERSION:
			continue
		out.append(trimmed if as_lines else dic)
	return out


static func history_file_exists(paths: MnemonicDataRootPaths) -> bool:
	if paths == null or not paths.is_valid():
		return false
	return FileAccess.file_exists(paths.get_segment_history_file())


static func read_records(paths: MnemonicDataRootPaths, max_display: int = -1) -> Array:
	if paths == null or not paths.is_valid():
		return []
	var path := paths.get_segment_history_file()
	if not FileAccess.file_exists(path):
		return []
	var text := FileAccess.get_file_as_string(path)
	if text.is_empty():
		return []
	var records := _parse_record_entries(text, false)
	if max_display > 0 and records.size() > max_display:
		records = records.slice(records.size() - max_display, records.size())
	records.reverse()
	return records


static func clear(paths: MnemonicDataRootPaths) -> void:
	if paths == null or not paths.is_valid():
		return
	var path := paths.get_segment_history_file()
	HookFileMutexGd.for_key(&"segment_history").lock()
	if FileAccess.file_exists(path):
		DirAccess.remove_absolute(path)
	HookFileMutexGd.for_key(&"segment_history").unlock()


static func trim_to_max(paths: MnemonicDataRootPaths, max_entries: int) -> void:
	if paths == null or not paths.is_valid():
		return
	var cap := clampi(
		max_entries,
		Mc.SEGMENT_HISTORY_MAX_ENTRIES_MIN,
		Mc.SEGMENT_HISTORY_MAX_ENTRIES_MAX,
	)
	var path := paths.get_segment_history_file()
	if not FileAccess.file_exists(path):
		return
	var text := FileAccess.get_file_as_string(path)
	HookFileMutexGd.for_key(&"segment_history").lock()
	var raw_lines := _parse_record_entries(text, true)
	if raw_lines.size() <= cap:
		HookFileMutexGd.for_key(&"segment_history").unlock()
		return
	var kept := raw_lines.slice(raw_lines.size() - cap, raw_lines.size())
	var out := "\n".join(kept)
	if not out.is_empty():
		out += "\n"
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file != null:
		file.store_string(out)
		file.close()
	HookFileMutexGd.for_key(&"segment_history").unlock()


static func build_search_blob(record: Dictionary) -> String:
	var parts: PackedStringArray = PackedStringArray()
	parts.append(format_summary(record))
	parts.append(str(record.get("capture_prefix", "")))
	parts.append(str(record.get("clip_id", "")))
	parts.append(str(record.get("git_subject", "")))
	parts.append(str(record.get("git_branch", "")))
	parts.append(str(record.get("git_commit", "")))
	parts.append("kept" if bool(record.get("preserved", false)) else "discarded")
	parts.append(str(int(record.get("segment_index", 0))))
	var breakdown: Variant = record.get("breakdown", [])
	if typeof(breakdown) == TYPE_ARRAY:
		for entry in breakdown:
			if typeof(entry) != TYPE_DICTIONARY:
				continue
			var row: Dictionary = entry
			parts.append(str(row.get("type", "")))
			parts.append(str(int(row.get("count", 0))))
			parts.append(str(int(row.get("points", 0))))
	return " ".join(parts).to_lower()


static func format_summary(
	record: Dictionary,
	preserve_threshold: int = -1,
	highlight_score_min: int = -1,
	notable_score_min: int = -1,
) -> String:
	var segment_index := int(record.get("segment_index", 0))
	var preserved := bool(record.get("preserved", false))
	var outcome := "kept" if preserved else "discarded"
	var score := int(record.get("score", 0))
	var threshold := int(record.get("threshold", 0))
	if preserve_threshold < 0 and threshold > 0:
		preserve_threshold = threshold
	var t_open := int(record.get("t_open_unix", 0))
	var t_close := int(record.get("t_close_unix", 0))
	var time_range := ""
	if t_open > 0 and t_close > 0:
		time_range = HookTimeDisplayGd.format_local_time_range(t_open, t_close)
	var branch := str(record.get("git_branch", "")).strip_edges()
	if branch.is_empty():
		branch = "—"
	var commit := str(record.get("git_commit", "")).strip_edges()
	if commit.length() > 7:
		commit = commit.substr(0, 7)
	var score_line := HookSignificanceTierGd.format_score_tooltip_line(
		score, preserve_threshold, highlight_score_min, notable_score_min
	)
	return "#%05d · %s · %s · threshold %d · %s · %s@%s" % [
		segment_index,
		outcome,
		score_line,
		threshold,
		time_range,
		branch,
		commit,
	]


static func format_tooltip(record: Dictionary) -> String:
	var lines: PackedStringArray = PackedStringArray()
	var t_open := int(record.get("t_open_unix", 0))
	var t_close := int(record.get("t_close_unix", 0))
	if t_open > 0 and t_close > 0:
		lines.append(HookTimeDisplayGd.format_local_time_range(t_open, t_close))
	lines.append("capture: %s" % str(record.get("capture_prefix", "")))
	lines.append(
		"manual preserve: %s" % ("yes" if bool(record.get("manual_preserve", false)) else "no")
	)
	var clip_id := str(record.get("clip_id", "")).strip_edges()
	if not clip_id.is_empty():
		lines.append("clip: %s" % clip_id)
	var subject := str(record.get("git_subject", "")).strip_edges()
	if not subject.is_empty():
		lines.append("subject: %s" % subject)
	var breakdown = record.get("breakdown", [])
	if typeof(breakdown) == TYPE_ARRAY:
		for entry in breakdown:
			if typeof(entry) != TYPE_DICTIONARY:
				continue
			var row: Dictionary = entry
			lines.append(
				"%s: %d (+%d)" % [
					str(row.get("type", "")),
					int(row.get("count", 0)),
					int(row.get("points", 0)),
				]
			)
	return "\n".join(lines)
