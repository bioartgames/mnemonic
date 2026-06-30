class_name HookDockShortcut
extends RefCounted

## Default focus/toggle shortcut for the Mnemonic dock tab (Godot shows on tab tooltip).
static func make_default() -> Shortcut:
	var shortcut := Shortcut.new()
	var event := InputEventKey.new()
	event.keycode = KEY_M
	event.alt_pressed = true
	shortcut.events.append(event)
	return shortcut
