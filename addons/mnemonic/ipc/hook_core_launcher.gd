class_name HookCoreLauncher
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookCoreProcessProbeGd = preload("res://addons/mnemonic/ipc/hook_core_process_probe.gd")

const BUNDLED_CORE_RES_PATH := "res://addons/mnemonic/core/Mnemonic.Windows.exe"


static func ensure_editor_setting_registered(editor_interface: EditorInterface) -> void:
	if editor_interface == null:
		return
	var settings := editor_interface.get_editor_settings()
	if settings.has_setting(Mc.EDITOR_SETTING_CORE_WINDOWS_EXE):
		return
	settings.set_setting(Mc.EDITOR_SETTING_CORE_WINDOWS_EXE, "")


static func read_configured_path(editor_interface: EditorInterface) -> String:
	if editor_interface == null:
		return ""
	var settings := editor_interface.get_editor_settings()
	if not settings.has_setting(Mc.EDITOR_SETTING_CORE_WINDOWS_EXE):
		return ""
	return str(settings.get_setting(Mc.EDITOR_SETTING_CORE_WINDOWS_EXE)).strip_edges()


static func default_repo_root() -> String:
	return ProjectSettings.globalize_path("res://").path_join("..").simplify_path()


static func default_candidates_for_repo(repo_root: String) -> PackedStringArray:
	var root := repo_root.replace("\\", "/")
	return PackedStringArray([
		root.path_join(
			"mnemonic-core/src/Mnemonic.Windows/bin/Release/net8.0-windows/Mnemonic.Windows.exe"
		),
		root.path_join(
			"mnemonic-core/src/Mnemonic.Windows/bin/Debug/net8.0-windows/Mnemonic.Windows.exe"
		),
		root.path_join("mnemonic-core/bin/Release/net8.0-windows/Mnemonic.Windows.exe"),
		root.path_join("mnemonic-core/dist/smoke/Mnemonic.Windows.exe"),
	])


static func bundled_candidate() -> String:
	return ProjectSettings.globalize_path(BUNDLED_CORE_RES_PATH).replace("\\", "/")


static func repo_candidates(repo_root: String) -> PackedStringArray:
	return default_candidates_for_repo(repo_root)


static func build_kind_label(exe_path: String) -> String:
	var norm := exe_path.replace("\\", "/").to_lower()
	if "/bin/release/" in norm:
		return "Release"
	if "/bin/debug/" in norm:
		return "Debug"
	if "/dist/smoke/" in norm:
		return "smoke"
	if "/addons/mnemonic/core/" in norm or norm.ends_with("/core/mnemonic.windows.exe"):
		return "bundled"
	return "custom"


static func launch_status_message(exe_path: String, already_running: bool) -> String:
	var kind := build_kind_label(exe_path)
	if already_running:
		return "Core already running (%s)" % kind
	return "Core started (%s)" % kind


static func resolve_executable_from(configured: String, repo_root: String) -> String:
	var custom := configured.strip_edges().replace("\\", "/")
	if not custom.is_empty() and FileAccess.file_exists(custom):
		if custom.get_file().to_lower() != Mc.CORE_PROCESS_IMAGE_NAME.to_lower():
			push_warning(
				"HookCoreLauncher: ignoring custom path (expected %s): %s"
				% [Mc.CORE_PROCESS_IMAGE_NAME, custom]
			)
			custom = ""
		else:
			return custom

	var bundled := bundled_candidate()
	if FileAccess.file_exists(bundled):
		return bundled

	var best_path := ""
	var best_mtime := 0
	for candidate in repo_candidates(repo_root):
		var path := str(candidate).replace("\\", "/")
		if not FileAccess.file_exists(path):
			continue
		var mtime := _core_dll_modified_unix(path)
		if mtime >= best_mtime:
			best_mtime = mtime
			best_path = path
	return best_path


static func _core_dll_modified_unix(exe_path: String) -> int:
	var dll_path := exe_path.get_base_dir().path_join("Mnemonic.Core.dll")
	if not FileAccess.file_exists(dll_path):
		return 0
	return int(FileAccess.get_modified_time(dll_path))


static func resolve_executable(editor_interface: EditorInterface) -> String:
	return resolve_executable_from(read_configured_path(editor_interface), default_repo_root())


static func try_launch(editor_interface: EditorInterface) -> Dictionary:
	var fail_message := (
		"Launch failed — install the release zip (core/ bundled) or set %s in Editor Settings"
		% Mc.EDITOR_SETTING_CORE_WINDOWS_EXE
	)
	if OS.get_name() != "Windows":
		return { "ok": false, "message": fail_message }

	var exe_path := resolve_executable(editor_interface)
	if HookCoreProcessProbeGd.is_running():
		if exe_path.is_empty():
			return { "ok": true, "message": "Core already running" }
		return {
			"ok": true,
			"message": launch_status_message(exe_path, true),
		}

	if exe_path.is_empty():
		push_warning(
			"HookCoreLauncher: Mnemonic.Windows.exe not found. "
			+ "Install the release zip (core/ bundled) or set %s in Editor Settings."
			% Mc.EDITOR_SETTING_CORE_WINDOWS_EXE
		)
		return { "ok": false, "message": fail_message }

	var pid: int = OS.create_process(exe_path, PackedStringArray(), false)
	if pid == -1:
		return { "ok": false, "message": fail_message }

	return {
		"ok": true,
		"message": launch_status_message(exe_path, false),
	}
