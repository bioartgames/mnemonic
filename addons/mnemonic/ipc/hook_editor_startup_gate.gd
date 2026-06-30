class_name HookEditorStartupGate
extends RefCounted

## Post-splash activation checks. Prefer EditorPlugin._set_window_layout as the gate (see plugin.gd);
## _set_window_layout also restores dock_slot via HookPluginWindowLayout before activation.
## get_base_control().is_inside_tree() is already true during the ~80% plugin-load phase.


static func is_resource_filesystem_idle(editor_interface: EditorInterface) -> bool:
	if editor_interface == null:
		return false
	var fs: EditorFileSystem = editor_interface.get_resource_filesystem()
	if fs == null:
		return true
	return not fs.is_scanning()


static func can_activate_heavy_work(editor_interface: EditorInterface) -> bool:
	return is_resource_filesystem_idle(editor_interface)
