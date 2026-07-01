class_name HookFlagWriter
extends RefCounted

const FLAG_JSON_BODY := "{}"
const HookFileMutexGd = preload("res://addons/mnemonic/ipc/hook_file_mutex.gd")


static func write_flag_current(paths: MnemonicDataRootPaths) -> bool:
	if paths == null or not paths.is_valid():
		push_warning("HookFlagWriter: DataRoot paths unavailable.")
		return false

	var path := paths.get_flag_current_file()
	var commands_dir := path.get_base_dir()

	var mutex := HookFileMutexGd.for_key(&"hook_flag")
	mutex.lock()
	var ok := _write_atomic(path, commands_dir)
	mutex.unlock()
	return ok


static func _write_atomic(path: String, commands_dir: String) -> bool:
	if not commands_dir.is_empty():
		var mkdir_err := DirAccess.make_dir_recursive_absolute(commands_dir)
		if mkdir_err != OK and mkdir_err != ERR_ALREADY_EXISTS:
			push_warning("HookFlagWriter: could not create %s (err %d)" % [commands_dir, mkdir_err])
			return false

	var temp_path := path + ".tmp"
	var file := FileAccess.open(temp_path, FileAccess.WRITE)
	if file == null:
		push_warning("HookFlagWriter: could not open %s for write" % temp_path)
		return false
	file.store_string(FLAG_JSON_BODY)
	file.close()

	if FileAccess.file_exists(path):
		var remove_err := DirAccess.remove_absolute(path)
		if remove_err != OK:
			push_warning("HookFlagWriter: could not remove existing %s (err %d)" % [path, remove_err])
			_cleanup_temp(temp_path)
			return false

	var rename_err := DirAccess.rename_absolute(temp_path, path)
	if rename_err != OK:
		push_warning("HookFlagWriter: could not rename %s to %s (err %d)" % [temp_path, path, rename_err])
		_cleanup_temp(temp_path)
		return false

	return true


static func _cleanup_temp(temp_path: String) -> void:
	if FileAccess.file_exists(temp_path):
		DirAccess.remove_absolute(temp_path)
