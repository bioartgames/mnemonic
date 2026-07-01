class_name HookHeuristicRow
extends RefCounted

signal settings_changed

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookSignalsTableLayoutGd = preload(
	"res://addons/mnemonic/ui/hook_signals_table_layout.gd"
)

var _type_id: String = ""
var _default_weight: int = 0
var _default_cap: int = 0
var _signal_description: String = ""
var _enabled_check: CheckBox
var _label: Label
var _weight_spin: SpinBox
var _cap_spin: SpinBox
var _theme = null


func setup(
	grid: GridContainer,
	entry: Dictionary,
	type_settings: Dictionary,
	dock_theme,
) -> void:
	_type_id = str(entry.get("type", ""))
	_default_weight = int(entry.get("default_weight", 0))
	_default_cap = int(entry.get("default_cap", 0))
	_signal_description = str(entry.get("description", ""))
	_theme = dock_theme

	_enabled_check = CheckBox.new()
	_enabled_check.tooltip_text = _signal_description
	_enabled_check.toggled.connect(_on_row_changed)
	HookSignalsTableLayoutGd.add_checkbox_cell(grid, _theme, _enabled_check)

	_label = HookSignalsTableLayoutGd.add_signal_label_cell(
		grid,
		_theme,
		str(entry.get("label", _type_id)),
	)
	_label.tooltip_text = _signal_description

	_weight_spin = SpinBox.new()
	_weight_spin.min_value = 0
	_weight_spin.max_value = 20
	_weight_spin.tooltip_text = Mc.TOOLTIP_SIGNAL_WEIGHT
	_weight_spin.value_changed.connect(_on_row_changed)
	HookSignalsTableLayoutGd.add_weight_spin_cell(grid, _theme, _weight_spin)

	if _default_cap > 0:
		_cap_spin = SpinBox.new()
		_cap_spin.min_value = 1
		_cap_spin.max_value = 20
		_cap_spin.tooltip_text = Mc.TOOLTIP_SIGNAL_CAP
		_cap_spin.value_changed.connect(_on_row_changed)
		HookSignalsTableLayoutGd.add_cap_spin_cell(grid, _theme, _cap_spin)
	else:
		_cap_spin = null
		HookSignalsTableLayoutGd.add_cap_na_cell(grid, _theme)

	_apply_type_settings(type_settings)


func apply_theme(dock_theme) -> void:
	if dock_theme == null:
		return
	_theme = dock_theme
	if is_instance_valid(_enabled_check):
		_theme.apply_editor_checkbox(_enabled_check)
		_enabled_check.custom_minimum_size.x = _theme.heuristic_checkbox_column_width_px()
	if is_instance_valid(_label):
		_theme.apply_settings_signal_label(_label)
	if is_instance_valid(_weight_spin):
		_theme.apply_editor_spin_box(_weight_spin)
	if is_instance_valid(_cap_spin):
		_theme.apply_editor_spin_box(_cap_spin)


func get_type_id() -> String:
	return _type_id


func to_settings_dict() -> Dictionary:
	var enabled := _enabled_check.button_pressed
	var weight := int(_weight_spin.value)
	if not enabled:
		weight = 0
	var result := {
		"enabled": enabled,
		"weight": weight,
	}
	if _default_cap > 0 and is_instance_valid(_cap_spin):
		result["cap"] = _cap_for_storage(int(_cap_spin.value))
	return result


func _cap_for_display(stored_cap: int) -> int:
	if stored_cap <= 0:
		return _default_cap
	return stored_cap


func _cap_for_storage(display_cap: int) -> int:
	if display_cap == _default_cap:
		return 0
	return display_cap


func _apply_type_settings(type_settings: Dictionary) -> void:
	var enabled := bool(type_settings.get("enabled", true))
	var weight := int(type_settings.get("weight", _default_weight))
	if not type_settings.has("weight"):
		weight = _default_weight
	if not enabled:
		weight = 0
	_enabled_check.set_block_signals(true)
	_weight_spin.set_block_signals(true)
	_enabled_check.button_pressed = enabled and weight > 0
	_weight_spin.value = weight
	_weight_spin.editable = _enabled_check.button_pressed
	if is_instance_valid(_cap_spin):
		_cap_spin.set_block_signals(true)
		_cap_spin.value = _cap_for_display(int(type_settings.get("cap", 0)))
		_cap_spin.set_block_signals(false)
	_enabled_check.set_block_signals(false)
	_weight_spin.set_block_signals(false)


func _on_row_changed(_unused = null) -> void:
	_weight_spin.editable = _enabled_check.button_pressed
	if not _enabled_check.button_pressed:
		_weight_spin.value = 0
	settings_changed.emit()
