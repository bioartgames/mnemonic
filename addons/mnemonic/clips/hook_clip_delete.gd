class_name HookClipDelete
extends RefCounted

const HookPathGuardGd = preload("res://addons/mnemonic/ipc/hook_path_guard.gd")


static func delete_clip_folder(folder_abs: String, clips_dir: String) -> bool:
	if folder_abs.is_empty() or clips_dir.is_empty():
		push_warning("HookClipDelete: empty path")
		return false

	if not HookPathGuardGd.is_absolute_path_under_root(folder_abs, clips_dir):
		push_warning("HookClipDelete: path outside clips dir: %s" % folder_abs)
		return false

	var normalized_folder := HookPathGuardGd.normalize_abs(folder_abs)
	if normalized_folder.is_empty():
		push_warning("HookClipDelete: invalid path: %s" % folder_abs)
		return false

	if not DirAccess.dir_exists_absolute(normalized_folder):
		push_warning("HookClipDelete: folder does not exist: %s" % normalized_folder)
		return false

	var clip_json := normalized_folder.path_join("clip.json")
	if not FileAccess.file_exists(clip_json):
		push_warning("HookClipDelete: refusing folder without clip.json: %s" % normalized_folder)
		return false

	var err: Error = _remove_dir_recursive(normalized_folder)
	if err != OK:
		push_warning("HookClipDelete: delete failed (%d) for %s" % [err, normalized_folder])
		return false

	return true


static func _remove_dir_recursive(dir_path: String) -> Error:
	for subdir in DirAccess.get_directories_at(dir_path):
		var err := _remove_dir_recursive(dir_path.path_join(subdir))
		if err != OK:
			return err
	for file_name in DirAccess.get_files_at(dir_path):
		var err := DirAccess.remove_absolute(dir_path.path_join(file_name))
		if err != OK:
			return err
	return DirAccess.remove_absolute(dir_path)
