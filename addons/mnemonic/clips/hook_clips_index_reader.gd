class_name HookClipsIndexReader
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookControlJsonReaderGd = preload("res://addons/mnemonic/ipc/hook_control_json_reader.gd")


static func read(paths: MnemonicDataRootPaths) -> Dictionary:
	if paths == null or not paths.is_valid():
		return {"ok": false, "entries": [], "error": "paths"}

	var index_path := paths.get_clips_index_file()
	var file_read: Dictionary = HookControlJsonReaderGd.read_dict_file(index_path)
	if not bool(file_read.get("ok", false)):
		return {
			"ok": false,
			"entries": [],
			"error": str(file_read.get("error", "read")),
		}

	var root: Dictionary = file_read.get("parsed", {})
	var version_raw = root.get("index_version", null)
	if version_raw == null or int(version_raw) != Mc.CLIPS_INDEX_VERSION:
		return {"ok": false, "entries": [], "error": "version"}

	var clips_raw: Variant = root.get("clips", [])
	if typeof(clips_raw) != TYPE_ARRAY:
		return {"ok": false, "entries": [], "error": "clips"}

	var entries: Array[Dictionary] = []
	for item in clips_raw:
		if typeof(item) != TYPE_DICTIONARY:
			continue
		var normalized := _normalize_entry(item)
		if not normalized.is_empty():
			entries.append(normalized)

	return {"ok": true, "entries": entries, "error": ""}


static func read_built_at_unix(paths: MnemonicDataRootPaths) -> int:
	if paths == null or not paths.is_valid():
		return -1
	var file_read: Dictionary = HookControlJsonReaderGd.read_dict_file(paths.get_clips_index_file())
	if not bool(file_read.get("ok", false)):
		return -1
	var root: Dictionary = file_read.get("parsed", {})
	return int(root.get("built_at_unix", -1))


static func _normalize_entry(item: Dictionary) -> Dictionary:
	var id := str(item.get("id", "")).strip_edges()
	if id.is_empty():
		return {}

	var tags: Array = []
	var tags_raw: Variant = item.get("tags", [])
	if typeof(tags_raw) == TYPE_ARRAY:
		for t in tags_raw:
			tags.append(str(t))

	var scenes_active: Array = []
	var scenes_raw: Variant = item.get("scenes_active", [])
	if typeof(scenes_raw) == TYPE_ARRAY:
		for s in scenes_raw:
			scenes_active.append(str(s))

	var ai_topics: Array = []
	var topics_raw: Variant = item.get("ai_topics", [])
	if typeof(topics_raw) == TYPE_ARRAY:
		for topic in topics_raw:
			ai_topics.append(str(topic))

	return {
		"id": id,
		"created_at": int(item.get("created_at", 0)),
		"duration_seconds": int(item.get("duration_seconds", 0)),
		"score": int(item.get("score", 0)),
		"commit_subject": str(item.get("commit_subject", "")),
		"git_branch": str(item.get("git_branch", "")),
		"git_commit": str(item.get("git_commit", "")),
		"scenes_active": scenes_active,
		"tags": tags,
		"ai_topics": ai_topics,
		"has_thumb": bool(item.get("has_thumb", false)),
		"has_video": bool(item.get("has_video", false)),
	}
