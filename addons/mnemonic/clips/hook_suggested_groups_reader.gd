class_name HookSuggestedGroupsReader
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookControlJsonReaderGd = preload("res://addons/mnemonic_hook/ipc/hook_control_json_reader.gd")


static func read(paths: MnemonicDataRootPaths) -> Dictionary:
	if paths == null or not paths.is_valid():
		return {"ok": false, "groups": [], "error": "paths"}

	var groups_path := paths.get_suggested_groups_file()
	var file_read: Dictionary = HookControlJsonReaderGd.read_dict_file(groups_path)
	if not bool(file_read.get("ok", false)):
		return {
			"ok": false,
			"groups": [],
			"error": str(file_read.get("error", "read")),
		}

	var root: Dictionary = file_read.get("parsed", {})
	var version_raw = root.get("groups_version", null)
	if version_raw == null or int(version_raw) != Mc.SUGGESTED_GROUPS_VERSION:
		return {"ok": false, "groups": [], "error": "version"}

	var groups_raw: Variant = root.get("groups", [])
	if typeof(groups_raw) != TYPE_ARRAY:
		return {"ok": false, "groups": [], "error": "groups"}

	var groups: Array[Dictionary] = []
	for item in groups_raw:
		if typeof(item) != TYPE_DICTIONARY:
			continue
		var normalized := _normalize_group(item)
		if not normalized.is_empty():
			groups.append(normalized)

	return {"ok": true, "groups": groups, "error": ""}


static func _normalize_group(item: Dictionary) -> Dictionary:
	var id := str(item.get("id", "")).strip_edges()
	if id.is_empty():
		return {}

	var clip_ids: PackedStringArray = PackedStringArray()
	var ids_raw: Variant = item.get("clip_ids", [])
	if typeof(ids_raw) == TYPE_ARRAY:
		for clip_id in ids_raw:
			var cid := str(clip_id).strip_edges()
			if not cid.is_empty():
				clip_ids.append(cid)

	if clip_ids.is_empty():
		return {}

	return {
		"id": id,
		"label": str(item.get("label", "")),
		"reason": str(item.get("reason", "")),
		"clip_ids": clip_ids,
	}
