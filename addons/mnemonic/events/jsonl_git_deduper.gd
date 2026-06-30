class_name JsonlGitDeduper
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")


static func has_recent_git_commit(path: String, commit: String) -> bool:
	for evt in _read_tail_events(path):
		if str(evt.get("type", "")) != "git_commit":
			continue
		if str(evt.get("commit", "")) == commit:
			return true
	return false


static func has_recent_git_branch_change(path: String, branch: String) -> bool:
	for evt in _read_tail_events(path):
		if str(evt.get("type", "")) != "git_branch_change":
			continue
		if str(evt.get("branch", "")) == branch:
			return true
	return false


static func has_recent_git_push(path: String, branch: String, window_sec: float) -> bool:
	var cutoff := float(Time.get_unix_time_from_system()) - window_sec
	for evt in _read_tail_events(path):
		if str(evt.get("type", "")) != "git_push":
			continue
		if float(evt.get("t", 0.0)) < cutoff:
			continue
		if str(evt.get("branch", "")) == branch:
			return true
	return false


static func _read_tail_events(path: String) -> Array:
	var out: Array = []
	if not FileAccess.file_exists(path):
		return out

	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		return out

	var size := file.get_length()
	if size <= 0:
		file.close()
		return out

	var read_length := mini(int(size), Mc.GIT_JSONL_DEDUPE_TAIL_BYTES)
	file.seek(size - read_length)
	var text := file.get_buffer(read_length).get_string_from_utf8()
	file.close()

	var lines := text.split("\n", false)
	for i in range(lines.size() - 1, -1, -1):
		var line := lines[i].strip_edges()
		if line.is_empty():
			continue
		var parsed = JSON.parse_string(line)
		if typeof(parsed) == TYPE_DICTIONARY:
			out.append(parsed)
	return out
