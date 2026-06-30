class_name HookLiveScratchReveal
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")


static func resolve(scratch_dir: String, segment_index: int, capture_prefix: String) -> Dictionary:
	var empty := {
		"show_reveal": false,
		"reveal_abs": "",
		"reveal_is_file": false,
		"menu_label": "",
	}
	if segment_index < 0:
		return empty

	var prefix := capture_prefix.strip_edges()
	if not prefix.is_empty():
		var exact := scratch_dir.path_join("%s_segment_%05d.mp4" % [prefix, segment_index])
		if FileAccess.file_exists(exact):
			return {
				"show_reveal": true,
				"reveal_abs": exact,
				"reveal_is_file": true,
				"menu_label": Mc.UI_MENU_REVEAL,
			}

	if not scratch_dir.is_empty() and DirAccess.dir_exists_absolute(scratch_dir):
		var suffix := "_segment_%05d.mp4" % segment_index
		var newest_path := ""
		var newest_mtime := -1
		for name in DirAccess.get_files_at(scratch_dir):
			var file_name := str(name)
			if not file_name.ends_with(suffix):
				continue
			var path := scratch_dir.path_join(file_name)
			var mt := int(FileAccess.get_modified_time(path))
			if mt > newest_mtime:
				newest_mtime = mt
				newest_path = path
		if not newest_path.is_empty():
			return {
				"show_reveal": true,
				"reveal_abs": newest_path,
				"reveal_is_file": true,
				"menu_label": Mc.UI_MENU_REVEAL,
			}

		return {
			"show_reveal": true,
			"reveal_abs": scratch_dir,
			"reveal_is_file": false,
			"menu_label": Mc.UI_MENU_REVEAL,
		}

	return empty
