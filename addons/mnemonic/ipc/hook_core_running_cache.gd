class_name HookCoreRunningCache
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookCoreProcessProbeGd = preload("res://addons/mnemonic/ipc/hook_core_process_probe.gd")

static var _cached := false
static var _expires_ms := 0
static var _test_probe_enabled := false
static var _test_probe_result := false
static var _test_probe_calls := 0


static func invalidate() -> void:
	_expires_ms = 0


static func read(force_refresh: bool = false) -> bool:
	var now_ms := Time.get_ticks_msec()
	if not force_refresh and _expires_ms > 0 and now_ms < _expires_ms:
		return _cached
	_cached = _probe_is_running()
	_expires_ms = now_ms + Mc.CORE_PROCESS_PROBE_CACHE_MS
	return _cached


static func set_probe_for_tests(result: bool) -> void:
	_test_probe_enabled = true
	_test_probe_result = result
	_test_probe_calls = 0
	invalidate()


static func clear_probe_for_tests() -> void:
	_test_probe_enabled = false
	_test_probe_calls = 0
	invalidate()


static func test_probe_call_count() -> int:
	return _test_probe_calls


static func _probe_is_running() -> bool:
	if _test_probe_enabled:
		_test_probe_calls += 1
		return _test_probe_result
	return HookCoreProcessProbeGd.is_running()
