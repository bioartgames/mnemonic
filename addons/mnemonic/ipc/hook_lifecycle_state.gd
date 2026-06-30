class_name HookLifecycleState
extends RefCounted

const HookStatusReadResultGd = preload("res://addons/mnemonic_hook/ipc/hook_status_read_result.gd")

var core_running: bool = false
var status: HookStatusReadResult = HookStatusReadResultGd.missing_file()
