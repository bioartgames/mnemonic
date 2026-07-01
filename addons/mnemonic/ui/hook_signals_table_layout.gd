class_name HookSignalsTableLayout
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const COLUMN_COUNT := 4


static func create_grid(theme) -> GridContainer:
	var grid := GridContainer.new()
	grid.columns = COLUMN_COUNT
	grid.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	if theme != null:
		grid.add_theme_constant_override("h_separation", theme.row_separation_px())
		grid.add_theme_constant_override("v_separation", theme.row_separation_px())
	return grid


static func add_category_row(grid: GridContainer, theme, text: String) -> void:
	_add_checkbox_spacer_cell(grid, theme)
	var label := Label.new()
	label.text = text
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	if theme != null:
		theme.apply_popup_subsection_label(label)
	grid.add_child(label)
	_add_metric_spacer_cell(grid, theme)
	_add_metric_spacer_cell(grid, theme)


static func add_checkbox_cell(grid: GridContainer, theme, checkbox: CheckBox) -> void:
	checkbox.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	if theme != null:
		theme.apply_editor_checkbox(checkbox)
		checkbox.custom_minimum_size.x = theme.heuristic_checkbox_column_width_px()
	grid.add_child(checkbox)


static func add_signal_label_cell(grid: GridContainer, theme, text: String) -> Label:
	var label := Label.new()
	label.text = text
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	if theme != null:
		theme.apply_settings_signal_label(label)
	grid.add_child(label)
	return label


static func add_weight_spin_cell(grid: GridContainer, theme, spin: SpinBox) -> void:
	_configure_metric_spin(spin, theme)
	grid.add_child(spin)


static func add_cap_spin_cell(grid: GridContainer, theme, spin: SpinBox) -> void:
	_configure_metric_spin(spin, theme)
	grid.add_child(spin)


static func add_cap_na_cell(grid: GridContainer, theme) -> void:
	grid.add_child(_make_na_placeholder(theme))


static func _add_checkbox_spacer_cell(grid: GridContainer, theme) -> void:
	var spacer := Control.new()
	spacer.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	if theme != null:
		spacer.custom_minimum_size.x = theme.heuristic_checkbox_column_width_px()
	grid.add_child(spacer)


static func _add_metric_spacer_cell(grid: GridContainer, theme) -> void:
	var spacer := Control.new()
	spacer.size_flags_horizontal = Control.SIZE_SHRINK_END
	if theme != null:
		spacer.custom_minimum_size.x = theme.spin_box_min_width_px()
	grid.add_child(spacer)


static func _configure_metric_spin(spin: SpinBox, theme) -> void:
	spin.size_flags_horizontal = Control.SIZE_SHRINK_END
	if theme != null:
		theme.apply_editor_spin_box(spin)


static func _make_na_placeholder(theme) -> Label:
	var placeholder := Label.new()
	placeholder.text = "—"
	placeholder.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	placeholder.size_flags_horizontal = Control.SIZE_SHRINK_END
	placeholder.tooltip_text = Mc.TOOLTIP_SIGNAL_CAP_NA
	if theme != null:
		theme.apply_popup_column_header(placeholder)
		placeholder.custom_minimum_size.x = theme.spin_box_min_width_px()
	return placeholder
