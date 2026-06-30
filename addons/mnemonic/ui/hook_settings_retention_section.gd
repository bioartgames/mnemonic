class_name HookSettingsRetentionSection
extends RefCounted

const Mc = preload("res://addons/mnemonic_hook/ipc/mnemonic_constants.gd")


static func build(
	theme: HookDockTheme,
	panel: VBoxContainer,
	add_row_fn: Callable,
	callbacks: Dictionary,
) -> Dictionary:
	var capture_retention_heading := Label.new()
	capture_retention_heading.text = Mc.UI_SETTINGS_SECTION_RETENTION
	capture_retention_heading.tooltip_text = Mc.TOOLTIP_RETENTION_SECTION
	if theme != null:
		theme.apply_popup_section_label(capture_retention_heading)
	panel.add_child(capture_retention_heading)

	var retention_panel := PanelContainer.new()
	retention_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	if theme != null:
		retention_panel.add_theme_stylebox_override("panel", theme.make_subtle_panel())
	var retention_inner := VBoxContainer.new()
	retention_inner.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	if theme != null:
		retention_inner.add_theme_constant_override("separation", theme.row_separation_px())
	retention_panel.add_child(retention_inner)
	panel.add_child(retention_panel)

	var segment_duration_spin := SpinBox.new()
	segment_duration_spin.min_value = Mc.SEGMENT_DURATION_MIN_SEC
	segment_duration_spin.max_value = Mc.SEGMENT_DURATION_MAX_SEC
	segment_duration_spin.step = Mc.SEGMENT_DURATION_STEP_SEC
	segment_duration_spin.value = Mc.SETTINGS_DEFAULT_SEGMENT_DURATION_SECONDS
	if theme != null:
		theme.apply_editor_spin_box(segment_duration_spin)
	if callbacks.has("segment_duration"):
		segment_duration_spin.value_changed.connect(callbacks["segment_duration"])
	add_row_fn.call(
		retention_inner,
		Mc.UI_SETTINGS_SEGMENT_LENGTH,
		segment_duration_spin,
		Mc.TOOLTIP_SEGMENT_DURATION,
	)

	var preserve_threshold_spin := SpinBox.new()
	preserve_threshold_spin.min_value = Mc.PRESERVE_THRESHOLD_MIN
	preserve_threshold_spin.max_value = Mc.PRESERVE_THRESHOLD_MAX
	preserve_threshold_spin.value = Mc.SETTINGS_DEFAULT_PRESERVE_THRESHOLD
	if theme != null:
		theme.apply_editor_spin_box(preserve_threshold_spin)
	if callbacks.has("preserve_threshold"):
		preserve_threshold_spin.value_changed.connect(callbacks["preserve_threshold"])
	add_row_fn.call(
		retention_inner,
		Mc.UI_SETTINGS_PRESERVE_THRESHOLD,
		preserve_threshold_spin,
		Mc.TOOLTIP_PRESERVE_THRESHOLD,
	)

	var notable_score_min_spin := SpinBox.new()
	notable_score_min_spin.min_value = Mc.PRESERVE_THRESHOLD_MIN
	notable_score_min_spin.max_value = Mc.PRESERVE_THRESHOLD_MAX
	notable_score_min_spin.value = Mc.SETTINGS_DEFAULT_NOTABLE_SCORE_MIN
	if theme != null:
		theme.apply_editor_spin_box(notable_score_min_spin)
	if callbacks.has("notable_score_min"):
		notable_score_min_spin.value_changed.connect(callbacks["notable_score_min"])
	add_row_fn.call(
		retention_inner,
		Mc.UI_SETTINGS_NOTABLE_SCORE,
		notable_score_min_spin,
		Mc.TOOLTIP_NOTABLE_SCORE_MIN,
	)

	var highlight_score_min_spin := SpinBox.new()
	highlight_score_min_spin.min_value = Mc.PRESERVE_THRESHOLD_MIN
	highlight_score_min_spin.max_value = Mc.PRESERVE_THRESHOLD_MAX
	highlight_score_min_spin.value = Mc.SETTINGS_DEFAULT_HIGHLIGHT_SCORE_MIN
	if theme != null:
		theme.apply_editor_spin_box(highlight_score_min_spin)
	if callbacks.has("highlight_score_min"):
		highlight_score_min_spin.value_changed.connect(callbacks["highlight_score_min"])
	add_row_fn.call(
		retention_inner,
		Mc.UI_SETTINGS_HIGHLIGHT_SCORE,
		highlight_score_min_spin,
		Mc.TOOLTIP_HIGHLIGHT_SCORE_MIN,
	)

	var segment_history_max_spin := SpinBox.new()
	segment_history_max_spin.min_value = Mc.SEGMENT_HISTORY_MAX_ENTRIES_MIN
	segment_history_max_spin.max_value = Mc.SEGMENT_HISTORY_MAX_ENTRIES_MAX
	segment_history_max_spin.value = Mc.SETTINGS_DEFAULT_SEGMENT_HISTORY_MAX_ENTRIES
	if theme != null:
		theme.apply_editor_spin_box(segment_history_max_spin)
	if callbacks.has("segment_history_max"):
		segment_history_max_spin.value_changed.connect(callbacks["segment_history_max"])
	add_row_fn.call(
		retention_inner,
		Mc.UI_SETTINGS_SEGMENT_LOG_RETENTION,
		segment_history_max_spin,
		Mc.TOOLTIP_SEGMENT_HISTORY_MAX_ENTRIES,
	)

	var draw_mouse_check := CheckBox.new()
	if theme != null:
		theme.apply_editor_checkbox(draw_mouse_check)
	if callbacks.has("draw_mouse"):
		draw_mouse_check.toggled.connect(callbacks["draw_mouse"])
	add_row_fn.call(
		retention_inner,
		Mc.UI_SETTINGS_CAPTURE_CURSOR,
		draw_mouse_check,
		Mc.TOOLTIP_DRAW_MOUSE,
	)

	return {
		"retention_panel": retention_panel,
		"segment_duration_spin": segment_duration_spin,
		"preserve_threshold_spin": preserve_threshold_spin,
		"notable_score_min_spin": notable_score_min_spin,
		"highlight_score_min_spin": highlight_score_min_spin,
		"segment_history_max_spin": segment_history_max_spin,
		"draw_mouse_check": draw_mouse_check,
	}
