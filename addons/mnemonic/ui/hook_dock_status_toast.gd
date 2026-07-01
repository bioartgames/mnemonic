class_name HookDockStatusToast
extends RefCounted


const HookDockHostGd = preload("res://addons/mnemonic/ui/hook_dock_host.gd")

static func show(host, message: String, duration_sec: float = 3.0) -> void:
	if host == null or not is_instance_valid(host.toast_label) or not is_instance_valid(host.toast_timer):
		return
	host.toast_label.text = message
	host.toast_timer.wait_time = duration_sec
	host.toast_timer.start()


static func clear(host) -> void:
	if host == null or not is_instance_valid(host.toast_label):
		return
	host.toast_label.text = ""
