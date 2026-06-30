class_name SessionEventJson
extends RefCounted


static func create_playtest_start(t: float, scene_path: String = "") -> Dictionary:
	var evt := {"t": t, "type": "playtest_start"}
	var path := scene_path.strip_edges()
	if not path.is_empty():
		evt["scene_path"] = path
	return evt


static func create_playtest_stop(t: float, duration_sec: float) -> Dictionary:
	return {"t": t, "type": "playtest_stop", "duration_sec": duration_sec}


static func create_git_commit(t: float, commit: String, subject: String) -> Dictionary:
	return {"t": t, "type": "git_commit", "commit": commit, "subject": subject}


static func create_git_branch_change(t: float, branch: String) -> Dictionary:
	return {"t": t, "type": "git_branch_change", "branch": branch}


static func create_git_push(t: float, branch: String, remote: String = "") -> Dictionary:
	return {"t": t, "type": "git_push", "branch": branch, "remote": remote}


static func create_debug_session_start(t: float) -> Dictionary:
	return {"t": t, "type": "debug_session_start"}


static func create_debug_session_stop(t: float, duration_sec: float) -> Dictionary:
	return {"t": t, "type": "debug_session_stop", "duration_sec": duration_sec}


static func create_script_save(t: float, path: String) -> Dictionary:
	return {"t": t, "type": "script_save", "path": path}


static func create_editor_focused_session(t: float, focus: String, duration_sec: float) -> Dictionary:
	return {"t": t, "type": "editor_focused_session", "focus": focus, "duration_sec": duration_sec}


static func create_scene_save(t: float, path: String) -> Dictionary:
	return {"t": t, "type": "scene_save", "path": path}


static func create_scene_transition(t: float, to_scene: String) -> Dictionary:
	return {"t": t, "type": "scene_transition", "to_scene": to_scene}


static func create_resource_saved(t: float, path: String) -> Dictionary:
	return {"t": t, "type": "resource_saved", "path": path}


static func create_editor_focus_changed(t: float, focus: String) -> Dictionary:
	return {"t": t, "type": "editor_focus_changed", "focus": focus}


static func create_runtime_error(t: float, message: String, scene: String = "", line: int = -1) -> Dictionary:
	var evt := {"t": t, "type": "runtime_error", "message": message}
	if not scene.is_empty():
		evt["scene"] = scene
	if line >= 0 and not scene.is_empty():
		evt["line"] = line
	return evt


static func to_json_line(event: Dictionary) -> String:
	if not event.has("t") or not event.has("type"):
		push_error("SessionEventJson: event must include t and type")
		return "{}"
	return JSON.stringify(event)
