class_name HookSettingsHeuristicsSection
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookSettingsIoGd = preload("res://addons/mnemonic/ipc/hook_settings_io.gd")
const HookHeuristicCatalogGd = preload("res://addons/mnemonic/heuristic/hook_heuristic_catalog.gd")
const HookHeuristicRowGd = preload("res://addons/mnemonic/ui/hook_heuristic_row.gd")
const HookSignalsTableLayoutGd = preload(
	"res://addons/mnemonic/ui/hook_signals_table_layout.gd"
)

const _SIGNAL_CATEGORY_LABELS := {
	"playtest": Mc.UI_SIGNAL_CATEGORY_PLAYTEST,
	"editor": Mc.UI_SIGNAL_CATEGORY_EDITOR,
	"git": Mc.UI_SIGNAL_CATEGORY_GIT,
}


static func build(theme: HookDockTheme, panel: VBoxContainer) -> Dictionary:
	var signals_heading := Label.new()
	signals_heading.text = Mc.UI_SETTINGS_SECTION_SIGNALS
	signals_heading.tooltip_text = Mc.TOOLTIP_SIGNALS_SECTION
	if theme != null:
		theme.apply_popup_section_label(signals_heading)
	panel.add_child(signals_heading)

	var signals_section := VBoxContainer.new()
	signals_section.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	if theme != null:
		signals_section.add_theme_constant_override("separation", theme.row_separation_px())
	panel.add_child(signals_section)

	var signals_scroll := ScrollContainer.new()
	signals_scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	signals_scroll.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	signals_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	signals_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	signals_section.add_child(signals_scroll)

	var signals_grid := HookSignalsTableLayoutGd.create_grid(theme)
	signals_grid.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	signals_scroll.add_child(signals_grid)

	return {
		"signals_section": signals_section,
		"signals_scroll": signals_scroll,
		"signals_grid": signals_grid,
	}


static func rebuild_rows(
	paths: MnemonicDataRootPaths,
	theme: HookDockTheme,
	grid: GridContainer,
	heuristic_rows: Array,
	on_row_changed: Callable,
	on_finalize_layout: Callable = Callable(),
) -> void:
	if not is_instance_valid(grid):
		return
	for child in grid.get_children():
		child.queue_free()
	heuristic_rows.clear()
	var stored := HookSettingsIoGd.read_heuristics(paths)
	for category in ["playtest", "editor", "git"]:
		var entries := HookHeuristicCatalogGd.entries_for_category(category)
		if entries.is_empty():
			continue
		HookSignalsTableLayoutGd.add_category_row(
			grid,
			theme,
			_SIGNAL_CATEGORY_LABELS.get(category, category.capitalize()),
		)
		for entry in entries:
			var type_id := str(entry.get("type", ""))
			if not HookHeuristicCatalogGd.is_settings_ui_visible(type_id):
				continue
			var type_settings: Dictionary = {}
			if typeof(stored.get(type_id, null)) == TYPE_DICTIONARY:
				type_settings = stored[type_id]
			elif not stored.has(type_id):
				type_settings = {
					"enabled": true,
					"weight": int(entry.get("default_weight", 0)),
					"cap": 0,
				}
			var row: HookHeuristicRow = HookHeuristicRowGd.new()
			row.setup(grid, entry, type_settings, theme)
			if on_row_changed.is_valid():
				row.settings_changed.connect(on_row_changed)
			heuristic_rows.append(row)
	if on_finalize_layout.is_valid():
		on_finalize_layout.call_deferred()
