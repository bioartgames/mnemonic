class_name HookDockFilterPanel
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookDatePickerGd = preload("res://addons/mnemonic/ui/hook_date_picker.gd")
const HookDockVerticalLayoutGd = preload("res://addons/mnemonic/ui/hook_dock_vertical_layout.gd")

var filter_row: HBoxContainer
var btn_toggle: Button
var btn_refresh: Button
var outer: PanelContainer
var panel: VBoxContainer
var search: LineEdit
var date_from: EditorHookDatePicker
var date_to: EditorHookDatePicker
var tier_option: OptionButton
var apply_timer: Timer
var btn_clear: Button


static func build(theme: HookDockTheme, timer_parent: Node) -> HookDockFilterPanel:
	var fp := HookDockFilterPanel.new()

	fp.filter_row = HBoxContainer.new()
	fp.filter_row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	HookDockVerticalLayoutGd.apply_hbox_center(fp.filter_row)
	if theme != null:
		fp.filter_row.add_theme_constant_override("separation", theme.row_separation_px())

	fp.btn_toggle = Button.new()
	fp.btn_toggle.text = Mc.UI_FILTER_CLIPS
	fp.btn_toggle.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	fp.btn_toggle.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	fp.filter_row.add_child(fp.btn_toggle)

	fp.btn_refresh = Button.new()
	fp.btn_refresh.tooltip_text = Mc.UI_REFRESH_CLIPS_TOOLTIP
	fp.btn_refresh.size_flags_horizontal = Control.SIZE_SHRINK_END
	fp.btn_refresh.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	fp.filter_row.add_child(fp.btn_refresh)

	fp.outer = PanelContainer.new()
	fp.outer.visible = false
	fp.outer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	fp.outer.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	if theme != null:
		fp.outer.add_theme_stylebox_override("panel", theme.make_filter_panel())

	fp.panel = VBoxContainer.new()
	fp.panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	fp.panel.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	if theme != null:
		fp.panel.add_theme_constant_override("separation", theme.row_separation_px())
	fp.outer.add_child(fp.panel)

	fp.search = LineEdit.new()
	fp.search.placeholder_text = Mc.UI_SEARCH_CLIPS_PLACEHOLDER
	fp.search.custom_minimum_size.x = 0
	fp.search.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	fp.panel.add_child(fp.search)

	var date_col := VBoxContainer.new()
	date_col.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	if theme != null:
		date_col.add_theme_constant_override("separation", theme.row_separation_px())
	fp.date_from = HookDatePickerGd.new()
	fp.date_from.configure("", Mc.UI_FILTER_DATE_FROM)
	fp.date_from.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	date_col.add_child(fp.date_from)
	fp.date_to = HookDatePickerGd.new()
	fp.date_to.configure("", Mc.UI_FILTER_DATE_TO)
	fp.date_to.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	date_col.add_child(fp.date_to)
	fp.panel.add_child(date_col)

	fp.tier_option = OptionButton.new()
	fp.tier_option.add_item(Mc.UI_FILTER_TIER_ALL)
	fp.tier_option.add_item(Mc.SIGNIFICANCE_TIER_LABEL_HIGHLIGHT)
	fp.tier_option.add_item(Mc.SIGNIFICANCE_TIER_LABEL_NOTABLE)
	fp.tier_option.add_item(Mc.SIGNIFICANCE_TIER_LABEL_MANUAL)
	if theme != null:
		theme.apply_filter_option_button(fp.tier_option)
	var tier_filter_row := CenterContainer.new()
	tier_filter_row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	if theme != null:
		tier_filter_row.custom_minimum_size.y = theme.filter_control_height_px()
	tier_filter_row.add_child(fp.tier_option)
	fp.panel.add_child(tier_filter_row)

	fp.apply_timer = Timer.new()
	fp.apply_timer.one_shot = true
	fp.apply_timer.wait_time = Mc.FILTER_APPLY_DEBOUNCE_SEC
	timer_parent.add_child(fp.apply_timer)

	fp.btn_clear = Button.new()
	fp.btn_clear.text = Mc.UI_CLEAR_FILTERS
	fp.panel.add_child(fp.btn_clear)

	return fp
