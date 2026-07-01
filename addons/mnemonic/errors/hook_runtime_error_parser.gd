class_name HookRuntimeErrorParser
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")


static func parse_output_error(data: Array) -> Dictionary:
	if data.size() < 11:
		return {}

	if bool(data[9]):
		return {}

	var message := truncate_message(
		resolve_message(str(data[7]), str(data[8])),
		Mc.STATUS_ERROR_MAX_LEN,
	)
	if message.is_empty():
		return {}

	var stack_size := int(data[10])
	var scene_info := resolve_scene(str(data[4]), stack_size, data)
	var result := {"message": message}
	if not scene_info.get("scene", "").is_empty():
		result["scene"] = scene_info["scene"]
		var line := int(scene_info.get("line", -1))
		if line >= 0:
			result["line"] = line
	return result


static func resolve_message(error: String, error_descr: String) -> String:
	var descr := error_descr.strip_edges()
	if not descr.is_empty():
		return descr
	return error.strip_edges()


static func resolve_scene(source_file: String, stack_size: int, data: Array) -> Dictionary:
	var file := source_file.strip_edges()
	var line := -1
	if data.size() > 6:
		line = int(data[6])

	if file.begins_with("res://"):
		return {"scene": file, "line": line}

	if stack_size > 0:
		var idx := 11
		var frames := stack_size / 3
		for _i in range(frames):
			if idx + 2 >= data.size():
				break
			var frame_file := str(data[idx]).strip_edges()
			var frame_line := int(data[idx + 2])
			idx += 3
			if frame_file.begins_with("res://"):
				return {"scene": frame_file, "line": frame_line}

	return {"scene": "", "line": -1}


static func truncate_message(message: String, max_len: int = Mc.STATUS_ERROR_MAX_LEN) -> String:
	if max_len <= 0:
		return ""
	if message.length() <= max_len:
		return message
	return message.substr(0, max_len)
