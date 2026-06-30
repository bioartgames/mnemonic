class_name HookFileMutex
extends RefCounted

static var _mutexes: Dictionary = {}


static func for_key(key: StringName) -> Mutex:
	if not _mutexes.has(key):
		_mutexes[key] = Mutex.new()
	return _mutexes[key]
