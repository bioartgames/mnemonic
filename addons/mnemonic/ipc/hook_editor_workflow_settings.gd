class_name HookEditorWorkflowSettings
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")

static var _editor_interface: EditorInterface = null


static func ensure_registered(editor_interface: EditorInterface) -> void:
	if editor_interface == null:
		return
	_editor_interface = editor_interface
	var settings := editor_interface.get_editor_settings()
	if not settings.has_setting(Mc.EDITOR_SETTING_AUTO_LAUNCH_CORE):
		settings.set_setting(Mc.EDITOR_SETTING_AUTO_LAUNCH_CORE, Mc.EDITOR_SETTING_DEFAULT_BOOL)
	if not settings.has_setting(Mc.EDITOR_SETTING_STOP_CORE_ON_EDITOR_EXIT):
		settings.set_setting(
			Mc.EDITOR_SETTING_STOP_CORE_ON_EDITOR_EXIT,
			Mc.EDITOR_SETTING_DEFAULT_BOOL,
		)
	if not settings.has_setting(Mc.EDITOR_SETTING_VERBOSE_LOGGING):
		settings.set_setting(Mc.EDITOR_SETTING_VERBOSE_LOGGING, Mc.EDITOR_SETTING_DEFAULT_BOOL)


static func read_auto_launch_core() -> bool:
	return _read_bool(Mc.EDITOR_SETTING_AUTO_LAUNCH_CORE, Mc.EDITOR_SETTING_DEFAULT_BOOL)


static func write_auto_launch_core(value: bool) -> void:
	_write_bool(Mc.EDITOR_SETTING_AUTO_LAUNCH_CORE, value)


static func read_stop_core_on_editor_exit() -> bool:
	return _read_bool(
		Mc.EDITOR_SETTING_STOP_CORE_ON_EDITOR_EXIT,
		Mc.EDITOR_SETTING_DEFAULT_BOOL,
	)


static func write_stop_core_on_editor_exit(value: bool) -> void:
	_write_bool(Mc.EDITOR_SETTING_STOP_CORE_ON_EDITOR_EXIT, value)


static func read_verbose_logging() -> bool:
	return _read_bool(Mc.EDITOR_SETTING_VERBOSE_LOGGING, Mc.EDITOR_SETTING_DEFAULT_BOOL)


static func write_verbose_logging(value: bool) -> void:
	_write_bool(Mc.EDITOR_SETTING_VERBOSE_LOGGING, value)


static func _read_bool(key: String, default_value: bool) -> bool:
	if _editor_interface == null:
		return default_value
	var settings := _editor_interface.get_editor_settings()
	if not settings.has_setting(key):
		return default_value
	return bool(settings.get_setting(key))


static func _write_bool(key: String, value: bool) -> void:
	if _editor_interface == null:
		return
	var settings := _editor_interface.get_editor_settings()
	settings.set_setting(key, value)
