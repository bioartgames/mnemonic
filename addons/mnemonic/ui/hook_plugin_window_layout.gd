class_name HookPluginWindowLayout
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")


static func read_dock_slot(configuration: ConfigFile) -> int:
	var raw := int(
		configuration.get_value(
			Mc.LAYOUT_SECTION_HOOK,
			Mc.LAYOUT_KEY_DOCK_SLOT,
			Mc.DEFAULT_DOCK_SLOT,
		)
	)
	return clamp_dock_slot(raw)


static func write_dock_slot(configuration: ConfigFile, slot: int) -> void:
	configuration.set_value(
		Mc.LAYOUT_SECTION_HOOK,
		Mc.LAYOUT_KEY_DOCK_SLOT,
		clamp_dock_slot(slot),
	)


static func clamp_dock_slot(slot: int) -> int:
	return clampi(slot, Mc.DOCK_SLOT_MIN, Mc.DOCK_SLOT_MAX_EXCLUSIVE - 1)
