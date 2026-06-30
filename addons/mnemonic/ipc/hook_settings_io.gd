class_name HookSettingsIo
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookFileMutexGd = preload("res://addons/mnemonic_hook/ipc/hook_file_mutex.gd")


static func read_settings(paths: MnemonicDataRootPaths) -> Dictionary:
	if paths == null or not paths.is_valid():
		return {}

	var path := paths.get_settings_file()
	if not FileAccess.file_exists(path):
		return {}

	var text := FileAccess.get_file_as_string(path).strip_edges()
	if text.is_empty():
		return {}

	var parsed: Variant = JSON.parse_string(text)
	if typeof(parsed) != TYPE_DICTIONARY:
		push_warning("HookSettingsIo: invalid settings JSON at %s" % path)
		return {}

	return parsed


static func read_int(paths: MnemonicDataRootPaths, key: String, default_value: int) -> int:
	var dic := read_settings(paths)
	if not dic.has(key):
		return default_value
	var raw = dic.get(key)
	if typeof(raw) not in [TYPE_INT, TYPE_FLOAT]:
		return default_value
	return int(raw)


static func read_draw_mouse(paths: MnemonicDataRootPaths) -> bool:
	return read_bool(paths, Mc.SETTINGS_KEY_DRAW_MOUSE, Mc.SETTINGS_DEFAULT_DRAW_MOUSE)


static func read_bool(
	paths: MnemonicDataRootPaths,
	key: String,
	default_value: bool,
) -> bool:
	return bool(read_settings(paths).get(key, default_value))


static func read_start_recording_on_launch(paths: MnemonicDataRootPaths) -> bool:
	return read_bool(
		paths,
		Mc.SETTINGS_KEY_START_RECORDING_ON_LAUNCH,
		Mc.SETTINGS_DEFAULT_START_RECORDING_ON_LAUNCH,
	)


static func read_heuristics(paths: MnemonicDataRootPaths) -> Dictionary:
	var dic := read_settings(paths)
	var raw = dic.get(Mc.SETTINGS_KEY_HEURISTICS, {})
	if typeof(raw) != TYPE_DICTIONARY:
		return {}
	return raw


static func write_int(paths: MnemonicDataRootPaths, key: String, value: int) -> bool:
	return write_settings_merge(paths, {key: value})


static func write_draw_mouse(paths: MnemonicDataRootPaths, value: bool) -> bool:
	return write_bool(paths, Mc.SETTINGS_KEY_DRAW_MOUSE, value)


static func write_bool(paths: MnemonicDataRootPaths, key: String, value: bool) -> bool:
	return write_settings_merge(paths, {key: value})


static func write_start_recording_on_launch(paths: MnemonicDataRootPaths, value: bool) -> bool:
	return write_bool(paths, Mc.SETTINGS_KEY_START_RECORDING_ON_LAUNCH, value)


static func write_heuristics(paths: MnemonicDataRootPaths, heuristics: Dictionary) -> bool:
	return write_settings_merge(paths, {Mc.SETTINGS_KEY_HEURISTICS: heuristics})


static func write_settings_merge(paths: MnemonicDataRootPaths, patch: Dictionary) -> bool:
	if paths == null or not paths.is_valid():
		push_warning("HookSettingsIo: DataRoot paths unavailable.")
		return false

	var dic := read_settings(paths)
	for patch_key in patch.keys():
		dic[patch_key] = patch[patch_key]
	_normalize_settings_for_json(dic)

	var path := paths.get_settings_file()
	var settings_dir := path.get_base_dir()
	var mutex := HookFileMutexGd.for_key(&"hook_settings")
	mutex.lock()
	var ok := _write_atomic(path, settings_dir, dic)
	mutex.unlock()
	return ok


static func _write_atomic(path: String, settings_dir: String, dic: Dictionary) -> bool:
	if not settings_dir.is_empty():
		var mkdir_err := DirAccess.make_dir_recursive_absolute(settings_dir)
		if mkdir_err != OK and mkdir_err != ERR_ALREADY_EXISTS:
			push_warning("HookSettingsIo: could not create %s (err %d)" % [settings_dir, mkdir_err])
			return false

	var body := JSON.stringify(dic, "\t")
	var temp_path := path + ".tmp"
	var file := FileAccess.open(temp_path, FileAccess.WRITE)
	if file == null:
		push_warning("HookSettingsIo: could not open %s for write" % temp_path)
		return false
	file.store_string(body)
	file.close()

	if FileAccess.file_exists(path):
		var remove_err := DirAccess.remove_absolute(path)
		if remove_err != OK:
			push_warning("HookSettingsIo: could not remove existing %s (err %d)" % [path, remove_err])
			_cleanup_temp(temp_path)
			return false

	var rename_err := DirAccess.rename_absolute(temp_path, path)
	if rename_err != OK:
		push_warning("HookSettingsIo: could not rename %s to %s (err %d)" % [temp_path, path, rename_err])
		_cleanup_temp(temp_path)
		return false

	return true


static func _normalize_settings_for_json(dic: Dictionary) -> void:
	for key in dic.keys():
		var raw: Variant = dic.get(key)
		if typeof(raw) == TYPE_FLOAT:
			var as_int := int(raw)
			if float(as_int) == raw:
				dic[key] = as_int
	if dic.has(Mc.SETTINGS_KEY_HEURISTICS):
		var heuristics: Variant = dic.get(Mc.SETTINGS_KEY_HEURISTICS)
		if typeof(heuristics) == TYPE_DICTIONARY:
			for type_id in heuristics.keys():
				var type_settings: Variant = heuristics[type_id]
				if typeof(type_settings) != TYPE_DICTIONARY:
					continue
				var row: Dictionary = type_settings
				if row.has("weight") and typeof(row.get("weight")) in [TYPE_INT, TYPE_FLOAT]:
					row["weight"] = int(row.get("weight"))
				if row.has("cap") and typeof(row.get("cap")) in [TYPE_INT, TYPE_FLOAT]:
					row["cap"] = int(row.get("cap"))


static func _cleanup_temp(temp_path: String) -> void:
	if FileAccess.file_exists(temp_path):
		DirAccess.remove_absolute(temp_path)
