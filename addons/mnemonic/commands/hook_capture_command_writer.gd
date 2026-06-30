class_name HookCaptureCommandWriter
extends RefCounted

const COMMAND_JSON_BODY := "{}"
const HookFileMutexGd = preload("res://addons/mnemonic_hook/ipc/hook_file_mutex.gd")


static func write_pause(paths: MnemonicDataRootPaths) -> bool:
	return _write_command(paths, paths.get_pause_capture_file() if paths != null else "")


static func write_resume(paths: MnemonicDataRootPaths) -> bool:
	return _write_command(paths, paths.get_resume_capture_file() if paths != null else "")


static func write_exit_core(paths: MnemonicDataRootPaths) -> bool:
	return _write_command(paths, paths.get_exit_core_file() if paths != null else "")


static func write_rebuild_clips_index(paths: MnemonicDataRootPaths) -> bool:
	return _write_command(paths, paths.get_rebuild_clips_index_file() if paths != null else "")


static func _write_command(paths: MnemonicDataRootPaths, path: String) -> bool:
	if paths == null or not paths.is_valid() or path.is_empty():
		push_warning("HookCaptureCommandWriter: DataRoot paths unavailable.")
		return false

	var commands_dir := path.get_base_dir()
	var mutex := HookFileMutexGd.for_key(&"capture_command")
	mutex.lock()
	var ok := _write_atomic(path, commands_dir)
	mutex.unlock()
	return ok


static func _write_atomic(path: String, commands_dir: String) -> bool:
	if not commands_dir.is_empty():
		var mkdir_err := DirAccess.make_dir_recursive_absolute(commands_dir)
		if mkdir_err != OK and mkdir_err != ERR_ALREADY_EXISTS:
			push_warning("HookCaptureCommandWriter: could not create %s (err %d)" % [commands_dir, mkdir_err])
			return false

	var temp_path := path + ".tmp"
	var file := FileAccess.open(temp_path, FileAccess.WRITE)
	if file == null:
		push_warning("HookCaptureCommandWriter: could not open %s for write" % temp_path)
		return false
	file.store_string(COMMAND_JSON_BODY)
	file.close()

	if FileAccess.file_exists(path):
		var remove_err := DirAccess.remove_absolute(path)
		if remove_err != OK:
			push_warning("HookCaptureCommandWriter: could not remove existing %s (err %d)" % [path, remove_err])
			_cleanup_temp(temp_path)
			return false

	var rename_err := DirAccess.rename_absolute(temp_path, path)
	if rename_err != OK:
		push_warning("HookCaptureCommandWriter: could not rename %s to %s (err %d)" % [temp_path, path, rename_err])
		_cleanup_temp(temp_path)
		return false

	return true


static func _cleanup_temp(temp_path: String) -> void:
	if FileAccess.file_exists(temp_path):
		DirAccess.remove_absolute(temp_path)
