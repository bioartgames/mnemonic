class_name HookClipsIndex
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookClipsRowBuilderGd = preload("res://addons/mnemonic_hook/clips/hook_clips_row_builder.gd")
const HookClipThumbnailGd = preload("res://addons/mnemonic_hook/clips/hook_clip_thumbnail.gd")


static func list_clips(
	paths: MnemonicDataRootPaths,
	preserve_threshold: int = -1,
	highlight_score_min: int = -1,
	notable_score_min: int = -1,
) -> Array[Dictionary]:
	var out: Array[Dictionary] = []
	if paths == null or not paths.is_valid():
		return out

	var clips_dir := paths.get_clips_dir()
	if not DirAccess.dir_exists_absolute(clips_dir):
		return out

	var sort_meta: Array[Dictionary] = []
	for name in DirAccess.get_directories_at(clips_dir):
		var meta := _read_clip_sort_meta(clips_dir, name)
		if not meta.is_empty():
			sort_meta.append(meta)

	sort_meta.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		return int(a.get("created_at", 0)) > int(b.get("created_at", 0))
	)

	if sort_meta.size() > Mc.CLIP_LIST_MAX:
		sort_meta.resize(Mc.CLIP_LIST_MAX)

	for meta in sort_meta:
		var subdir := str(meta.get("subdir", ""))
		if subdir.is_empty():
			continue
		var info := _read_clip_if_present(
			paths, clips_dir, subdir, preserve_threshold, highlight_score_min, notable_score_min
		)
		if not info.is_empty():
			out.append(info)

	return out


static func _read_clip_sort_meta(clips_dir: String, subdir: String) -> Dictionary:
	var json_abs := clips_dir.path_join(subdir).path_join("clip.json")
	if not FileAccess.file_exists(json_abs):
		return {}

	var file := FileAccess.open(json_abs, FileAccess.READ)
	if file == null:
		return {}

	var txt := file.get_as_text()
	file.close()
	var parsed: Variant = JSON.parse_string(txt)
	if typeof(parsed) != TYPE_DICTIONARY:
		return {}

	var dic: Dictionary = parsed
	return {
		"subdir": subdir,
		"created_at": int(dic.get("created_at", 0)),
	}


static func _read_clip_if_present(
	paths: MnemonicDataRootPaths,
	clips_dir: String,
	subdir: String,
	preserve_threshold: int = -1,
	highlight_score_min: int = -1,
	notable_score_min: int = -1,
) -> Dictionary:
	var folder_abs := clips_dir.path_join(subdir)
	var json_abs := folder_abs.path_join("clip.json")
	if not FileAccess.file_exists(json_abs):
		return {}

	var file := FileAccess.open(json_abs, FileAccess.READ)
	if file == null:
		return {}

	var txt := file.get_as_text()
	file.close()
	var parsed: Variant = JSON.parse_string(txt)
	if typeof(parsed) != TYPE_DICTIONARY:
		return {}

	var dic: Dictionary = parsed
	dic["id"] = str(dic.get("id", subdir))
	return HookClipsRowBuilderGd.from_clip_dict(
		paths, dic, folder_abs, preserve_threshold, highlight_score_min, notable_score_min
	)
