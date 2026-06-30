class_name HookClipsSource
extends RefCounted

enum SourceMode { AUTO, SCAN_ONLY }

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookClipsIndexGd = preload("res://addons/mnemonic_hook/clips/hook_clips_index.gd")
const HookClipsIndexReaderGd = preload("res://addons/mnemonic_hook/clips/hook_clips_index_reader.gd")
const HookClipsRowBuilderGd = preload("res://addons/mnemonic_hook/clips/hook_clips_row_builder.gd")
const HookClipFilterGd = preload("res://addons/mnemonic_hook/clips/hook_clip_filter.gd")
const HookSuggestedGroupsReaderGd = preload("res://addons/mnemonic_hook/clips/hook_suggested_groups_reader.gd")


static func list_for_dock(
	paths: MnemonicDataRootPaths,
	criteria,
	mode: int,
	preserve_threshold: int = -1,
	highlight_score_min: int = -1,
	notable_score_min: int = -1,
) -> Dictionary:
	return _load_rows(
		paths, criteria, mode, preserve_threshold, highlight_score_min, notable_score_min
	)


static func list_grouped_for_dock(
	paths: MnemonicDataRootPaths,
	criteria,
	mode: int,
	preserve_threshold: int = -1,
	highlight_score_min: int = -1,
	notable_score_min: int = -1,
) -> Dictionary:
	var row_result: Dictionary = _load_rows(
		paths, criteria, mode, preserve_threshold, highlight_score_min, notable_score_min
	)
	var empty_reason := str(row_result.get("empty_reason", ""))
	if not empty_reason.is_empty():
		return {
			"use_groups": false,
			"groups": [],
			"rows": [],
			"empty_reason": empty_reason,
		}

	var rows: Array = row_result.get("rows", [])
	if mode == SourceMode.AUTO:
		var groups_read: Dictionary = HookSuggestedGroupsReaderGd.read(paths)
		if bool(groups_read.get("ok", false)):
			var grouped := _build_grouped_output(rows, groups_read.get("groups", []))
			if bool(grouped.get("use_groups", false)):
				return grouped

	return {
		"use_groups": false,
		"groups": [],
		"rows": rows,
		"empty_reason": "",
	}


static func _load_rows(
	paths: MnemonicDataRootPaths,
	criteria,
	mode: int,
	preserve_threshold: int = -1,
	highlight_score_min: int = -1,
	notable_score_min: int = -1,
) -> Dictionary:
	var raw_rows: Array[Dictionary] = []
	if paths == null or not paths.is_valid():
		return {"rows": [], "empty_reason": "no_clips"}

	if mode == SourceMode.SCAN_ONLY:
		raw_rows = HookClipsIndexGd.list_clips(
			paths, preserve_threshold, highlight_score_min, notable_score_min
		)
	else:
		var read_result: Dictionary = HookClipsIndexReaderGd.read(paths)
		if bool(read_result.get("ok", false)):
			for entry in read_result.get("entries", []):
				var row := HookClipsRowBuilderGd.from_index_entry(
					paths,
					entry,
					preserve_threshold,
					highlight_score_min,
					notable_score_min,
				)
				if not row.is_empty():
					raw_rows.append(row)
		else:
			match str(read_result.get("error", "")):
				"missing":
					raw_rows = HookClipsIndexGd.list_clips(
						paths, preserve_threshold, highlight_score_min, notable_score_min
					)
				_:
					return {"rows": [], "empty_reason": "index_unavailable"}

	if raw_rows.is_empty():
		return {"rows": [], "empty_reason": "no_clips"}

	var filtered: Array[Dictionary] = []
	for row in raw_rows:
		if HookClipFilterGd.matches(row, criteria):
			filtered.append(row)

	if filtered.is_empty() and criteria != null and not criteria.is_empty():
		return {"rows": [], "empty_reason": "no_match"}

	if filtered.size() > Mc.CLIP_LIST_MAX:
		filtered.resize(Mc.CLIP_LIST_MAX)

	return {"rows": filtered, "empty_reason": ""}


static func _build_grouped_output(rows: Array, file_groups: Array) -> Dictionary:
	var rows_by_id: Dictionary = {}
	for row in rows:
		if typeof(row) != TYPE_DICTIONARY:
			continue
		var id := str(row.get("id", "")).strip_edges()
		if not id.is_empty():
			rows_by_id[id] = row

	var output_groups: Array[Dictionary] = []
	var total_rows := 0
	for file_group in file_groups:
		if typeof(file_group) != TYPE_DICTIONARY:
			continue
		var group_rows: Array[Dictionary] = []
		for clip_id in file_group.get("clip_ids", PackedStringArray()):
			if total_rows >= Mc.CLIP_LIST_MAX:
				break
			var cid := str(clip_id)
			if rows_by_id.has(cid):
				group_rows.append(rows_by_id[cid])
				total_rows += 1
		if group_rows.is_empty():
			continue
		output_groups.append({
			"id": str(file_group.get("id", "")),
			"label": str(file_group.get("label", "")),
			"reason": str(file_group.get("reason", "")),
			"rows": group_rows,
		})
		if total_rows >= Mc.CLIP_LIST_MAX:
			break

	if output_groups.is_empty():
		return {"use_groups": false, "groups": [], "rows": rows, "empty_reason": ""}

	return {
		"use_groups": true,
		"groups": output_groups,
		"rows": [],
		"empty_reason": "",
	}
