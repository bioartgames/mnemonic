extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")


static func edited_scene_path(editor_interface: EditorInterface) -> String:
	if editor_interface == null:
		return ""
	var root := editor_interface.get_edited_scene_root()
	if root == null:
		return ""
	return root.scene_file_path.strip_edges()


static func playing_scene_path(editor_interface: EditorInterface) -> String:
	if editor_interface == null:
		return ""
	if not editor_interface.is_playing_scene():
		return ""
	return editor_interface.get_playing_scene().strip_edges()


static func collect_paths(editor_interface: EditorInterface) -> Dictionary:
	return {
		"edited": edited_scene_path(editor_interface),
		"playing": playing_scene_path(editor_interface),
	}


static func write_snapshot(paths: MnemonicDataRootPaths, editor_interface: EditorInterface) -> void:
	if paths == null or not paths.is_valid():
		return
	var collected := collect_paths(editor_interface)
	var control_dir := paths.get_control_dir()
	DirAccess.make_dir_recursive_absolute(control_dir)
	var payload := {
		"contract_version": Mc.EDITOR_SCENE_CONTRACT_VERSION,
		"updated_unix": float(Time.get_unix_time_from_system()),
		"edited_scene_path": str(collected.get("edited", "")),
		"playing_scene_path": str(collected.get("playing", "")),
	}
	var file := FileAccess.open(paths.get_editor_scene_file(), FileAccess.WRITE)
	if file == null:
		return
	file.store_string(JSON.stringify(payload))
	file.close()
