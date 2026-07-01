class_name HookRuntimeErrorDebuggerPlugin
extends EditorDebuggerPlugin

const HookRuntimeErrorTrackerGd = preload("res://addons/mnemonic/errors/hook_runtime_error_tracker.gd")
const HookDebugSessionTrackerGd = preload("res://addons/mnemonic/errors/hook_debug_session_tracker.gd")

var _tracker: HookRuntimeErrorTracker
var _debug_session
var _is_playtest_active_callable: Callable
var _debuggers: Dictionary = {}


func _init(events_path: String, is_playtest_active_callable: Callable) -> void:
	_tracker = HookRuntimeErrorTrackerGd.new(events_path)
	_debug_session = HookDebugSessionTrackerGd.new(events_path)
	_is_playtest_active_callable = is_playtest_active_callable


func _setup_session(_session_id: int) -> void:
	var session := get_session(_session_id)
	if session == null:
		return
	if not session.started.is_connected(_on_debug_session_started):
		session.started.connect(_on_debug_session_started)
	call_deferred("_attach_debug_data_listeners")


func detach() -> void:
	_debug_session.detach()
	for dbg in _debuggers.values():
		if is_instance_valid(dbg) and dbg.debug_data.is_connected(_on_debug_data):
			dbg.debug_data.disconnect(_on_debug_data)
	_debuggers.clear()


func _on_debug_session_started() -> void:
	var playtest_active: bool = (
		not _is_playtest_active_callable.is_null() and _is_playtest_active_callable.call()
	)
	_debug_session.on_debug_session_started(playtest_active)
	call_deferred("_attach_debug_data_listeners")


func on_playtest_stopped() -> void:
	_debug_session.on_playtest_stopped()


func _attach_debug_data_listeners() -> void:
	var tree := Engine.get_main_loop()
	if tree == null or not tree is SceneTree:
		return
	var root: Node = (tree as SceneTree).root
	if root == null:
		return
	var debuggers := root.find_children("*", "ScriptEditorDebugger", true, false)
	for dbg in debuggers:
		if not is_instance_valid(dbg):
			continue
		var id := dbg.get_instance_id()
		if _debuggers.has(id):
			continue
		dbg.debug_data.connect(_on_debug_data)
		_debuggers[id] = dbg


func _on_debug_data(msg: String, data: Array) -> void:
	if not is_runtime_error_debug_message(msg):
		return
	if _is_playtest_active_callable.is_null() or not _is_playtest_active_callable.call():
		return
	_tracker.on_output_error(data)


static func is_runtime_error_debug_message(msg: String) -> bool:
	return msg == "error"
