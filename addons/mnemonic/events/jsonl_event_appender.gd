class_name JsonlEventAppender
extends RefCounted

const SessionEventJsonGd = preload("res://addons/mnemonic_hook/events/session_event_json.gd")
const HookFileMutexGd = preload("res://addons/mnemonic_hook/ipc/hook_file_mutex.gd")


static func append(path: String, event: Dictionary) -> void:
	var directory := path.get_base_dir()
	if not directory.is_empty():
		var err := DirAccess.make_dir_recursive_absolute(directory)
		if err != OK and err != ERR_ALREADY_EXISTS:
			push_warning("JsonlEventAppender: could not create directory %s (err %d)" % [directory, err])
			return

	var mutex := HookFileMutexGd.for_key(&"jsonl_event")
	mutex.lock()
	var file: FileAccess = null
	if FileAccess.file_exists(path):
		file = FileAccess.open(path, FileAccess.READ_WRITE)
		if file != null:
			file.seek_end()
	else:
		file = FileAccess.open(path, FileAccess.WRITE)

	if file == null:
		mutex.unlock()
		push_warning("JsonlEventAppender: could not open %s for append" % path)
		return

	file.store_string(SessionEventJsonGd.to_json_line(event) + "\n")
	file.close()
	mutex.unlock()
