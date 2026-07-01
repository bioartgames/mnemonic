class_name HookLifecyclePoller
extends RefCounted

const HookCoreRunningCacheGd = preload("res://addons/mnemonic/ipc/hook_core_running_cache.gd")
const HookLifecycleStateGd = preload("res://addons/mnemonic/ipc/hook_lifecycle_state.gd")
const HookStatusReaderGd = preload("res://addons/mnemonic/ipc/hook_status_reader.gd")
const HookStatusReadResultGd = preload("res://addons/mnemonic/ipc/hook_status_read_result.gd")

var _reader: HookStatusReader
var _last: HookLifecycleState = HookLifecycleStateGd.new()
var _not_running_warned: bool = false
var _missing_status_warned: bool = false
var _verbose_logging: bool = false


func _init(paths: MnemonicDataRootPaths, verbose_logging: bool = false) -> void:
	_reader = HookStatusReaderGd.new(paths)
	_verbose_logging = verbose_logging


func poll(force_refresh: bool = false) -> void:
	_last.core_running = HookCoreRunningCacheGd.read(force_refresh)
	_last.status = _reader.read()

	if not _last.core_running:
		if not _not_running_warned:
			if _verbose_logging:
				push_warning("Mnemonic: Mnemonic Core is not running.")
			_not_running_warned = true
		_missing_status_warned = false
	elif _last.status.code == HookStatusReadResult.Code.MISSING_FILE:
		_not_running_warned = false
		if not _missing_status_warned:
			if _verbose_logging:
				push_warning("Mnemonic: Core running but status.json missing.")
			_missing_status_warned = true
	else:
		_not_running_warned = false
		_missing_status_warned = false


func set_verbose_logging(value: bool) -> void:
	_verbose_logging = value
	_not_running_warned = false
	_missing_status_warned = false


func get_last() -> HookLifecycleState:
	return _last
