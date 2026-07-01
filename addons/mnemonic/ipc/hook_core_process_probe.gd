class_name HookCoreProcessProbe
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")

## Must stay false: true flashes a visible console every STATUS_POLL_INTERVAL_SEC.
const EXECUTE_OPEN_CONSOLE := false
const EXECUTE_READ_STDERR := true


static func get_tasklist_argv() -> PackedStringArray:
	return PackedStringArray([
		"/FI",
		"IMAGENAME eq %s" % Mc.CORE_PROCESS_IMAGE_NAME,
		"/NH",
	])


static func is_running() -> bool:
	if OS.get_name() != "Windows":
		return false

	var output: Array = []
	var exit_code: int = OS.execute(
		"tasklist",
		get_tasklist_argv(),
		output,
		EXECUTE_READ_STDERR,
		EXECUTE_OPEN_CONSOLE
	)
	if exit_code != 0:
		return false

	return parse_tasklist_output(output)


static func parse_tasklist_output(lines: PackedStringArray) -> bool:
	if lines.is_empty():
		return false

	var combined := ""
	for line in lines:
		combined += str(line)
	return Mc.CORE_PROCESS_IMAGE_NAME.to_lower() in combined.to_lower()
