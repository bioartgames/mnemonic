class_name HookCoreShutdown
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")
const HookCoreProcessProbeGd = preload("res://addons/mnemonic_hook/ipc/hook_core_process_probe.gd")
const HookCaptureCommandWriterGd = preload(
	"res://addons/mnemonic_hook/commands/hook_capture_command_writer.gd"
)


static func request_graceful_shutdown(paths: MnemonicDataRootPaths) -> bool:
	if paths == null or not paths.is_valid():
		return false
	if not HookCoreProcessProbeGd.is_running():
		return true
	if not HookCaptureCommandWriterGd.write_exit_core(paths):
		push_warning("Mnemonic Hook: could not write exit_core command.")
		return false

	var deadline_ms := Time.get_ticks_msec() + int(Mc.CORE_SHUTDOWN_WAIT_SEC * 1000.0)
	while Time.get_ticks_msec() < deadline_ms:
		if not HookCoreProcessProbeGd.is_running():
			return true
		OS.delay_msec(int(Mc.CORE_SHUTDOWN_POLL_SEC * 1000.0))

	push_warning(
		"Mnemonic Hook: Core did not exit within %ss after shutdown command."
		% Mc.CORE_SHUTDOWN_WAIT_SEC
	)
	return false
