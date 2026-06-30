class_name HookControlJsonReader
extends RefCounted

const MAX_READ_ATTEMPTS := 3


static func read_dict_file(path: String) -> Dictionary:
	if not FileAccess.file_exists(path):
		return {"ok": false, "parsed": {}, "error": "missing"}

	for attempt in MAX_READ_ATTEMPTS:
		var file := FileAccess.open(path, FileAccess.READ)
		if file == null:
			if attempt >= MAX_READ_ATTEMPTS - 1:
				return {"ok": false, "parsed": {}, "error": "read"}
			continue

		var text := file.get_as_text()
		file.close()

		var parsed: Variant = JSON.parse_string(text)
		if typeof(parsed) == TYPE_DICTIONARY:
			return {"ok": true, "parsed": parsed, "error": ""}

		if attempt >= MAX_READ_ATTEMPTS - 1:
			return {"ok": false, "parsed": {}, "error": "json"}

	return {"ok": false, "parsed": {}, "error": "json"}
